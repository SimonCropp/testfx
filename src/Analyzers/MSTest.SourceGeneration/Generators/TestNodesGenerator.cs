﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under dual-license. See LICENSE.PLATFORMTOOLS.txt file in the project root for full license information.

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Testing.Framework.SourceGeneration.Helpers;
using Microsoft.Testing.Framework.SourceGeneration.ObjectModels;

namespace Microsoft.Testing.Framework.SourceGeneration;

[Generator]
public sealed class TestNodesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<TestTypeInfo> testClassesProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute",
            static (node, _) =>
                node is TypeDeclarationSyntax typeDeclarationSyntax
                && (typeDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword) || typeDeclarationSyntax.Modifiers.Any(SyntaxKind.InternalKeyword))
                // No static classes.
                && !typeDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword),
            static (context, _) =>
            {
                WellKnownTypes wellKnownTypes = new(context.SemanticModel.Compilation);
                var testClassInfo = TestTypeInfo.TryBuild(context, wellKnownTypes);
                return testClassInfo;
            })
            .WhereNotNull();

        // Generate a file with one static class and one static TestNode field for all public classes we find
        context.RegisterImplementationSourceOutput(testClassesProvider, AddTestClassNode);

        IncrementalValueProvider<(string? Left, ImmutableArray<TestTypeInfo> Right)> assemblyNamespacesProvider
            = context.CompilationProvider.Select((compilation, _) => compilation.AssemblyName)
            .Combine(testClassesProvider.Collect());

        context.RegisterImplementationSourceOutput(assemblyNamespacesProvider, AddAssemblyTestNode);
    }

    private static void AddAssemblyTestNode(SourceProductionContext context, (string? AssemblyName, ImmutableArray<TestTypeInfo> TestClasses) provider)
    {
        string assemblyName = provider.AssemblyName ?? "<UnknownAssembly>";
        ImmutableArray<TestTypeInfo> testClasses = provider.TestClasses;

        var sourceStringBuilder = new IndentedStringBuilder();
        sourceStringBuilder.AppendAutoGeneratedHeader();
        sourceStringBuilder.AppendLine();

        TestNamespaceInfo[] uniqueUsedNamespaces = testClasses
            .Select(x => x.ContainingNamespace)
            .Distinct()
            .ToArray();

        string? safeAssemblyName = null;
        IDisposable? namespaceBlock = null;
        try
        {
            if (!uniqueUsedNamespaces.Any(x => x.IsGlobalNamespace))
            {
                safeAssemblyName = ToSafeNamespace(assemblyName);

                // TODO: We should look for the default namespace, if made visible to the compiler, or default to assembly name.
                namespaceBlock = sourceStringBuilder.AppendBlock($"namespace {safeAssemblyName}");
            }

            foreach (TestNamespaceInfo usedNamespace in uniqueUsedNamespaces)
            {
                if (!usedNamespace.IsGlobalNamespace)
                {
                    sourceStringBuilder.AppendLine($"using {usedNamespace.FullyQualifiedName};");
                }
            }

            sourceStringBuilder.AppendLine("using ColGen = global::System.Collections.Generic;");
            sourceStringBuilder.AppendLine("using CA = global::System.Diagnostics.CodeAnalysis;");
            sourceStringBuilder.AppendLine("using Sys = global::System;");
            sourceStringBuilder.AppendLine("using Tasks = global::System.Threading.Tasks;");
            sourceStringBuilder.AppendLine("using Msg = global::Microsoft.Testing.Platform.Extensions.Messages;");
            sourceStringBuilder.AppendLine("using MSTF = global::Microsoft.Testing.Framework;");
            sourceStringBuilder.AppendLine("using Cap = global::Microsoft.Testing.Platform.Capabilities.TestFramework;");
            sourceStringBuilder.AppendLine("using TrxReport = global::Microsoft.Testing.Extensions.TrxReport.Abstractions;");
            sourceStringBuilder.AppendLine();

            sourceStringBuilder.AppendLine("[CA::ExcludeFromCodeCoverage]");
            using (sourceStringBuilder.AppendBlock("public sealed class SourceGeneratedTestNodesBuilder : MSTF::ITestNodesBuilder"))
            {
                using (sourceStringBuilder.AppendBlock("private sealed class ClassCapabilities : TrxReport::ITrxReportCapability"))
                {
                    string isTrxReportSupported = testClasses.IsEmpty ? "false" : "true";
                    sourceStringBuilder.AppendLine($"bool TrxReport::ITrxReportCapability.IsSupported {{ get; }} = {isTrxReportSupported};");
                    sourceStringBuilder.AppendLine("void TrxReport::ITrxReportCapability.Enable() {}");
                }

                sourceStringBuilder.AppendLine();
                sourceStringBuilder.AppendLine("public ColGen::IReadOnlyCollection<Cap::ITestFrameworkCapability> Capabilities { get; } = new Cap::ITestFrameworkCapability[1] { new ClassCapabilities() };");
                sourceStringBuilder.AppendLine();

                using (sourceStringBuilder.AppendBlock($"public Tasks::Task<MSTF::TestNode[]> BuildAsync(MSTF::ITestSessionContext testSessionContext)"))
                {
                    if (testClasses.IsEmpty)
                    {
                        sourceStringBuilder.AppendLine("return Tasks::Task.FromResult(Sys::Array.Empty<MSTF::TestNode>());");
                    }
                    else
                    {
                        AppendAssemblyTestNodeBuilderContent(sourceStringBuilder, assemblyName, testClasses);
                    }
                }
            }
        }
        finally
        {
            namespaceBlock?.Dispose();
        }

        string code = sourceStringBuilder.ToString();
        // DEBUG: Debug.WriteLine is useful to observe the code when changing the source code generator or applying it to a new test suite.
        // VS is caching the generator, so start DebugView++ and just rebuild the TestContainer to make changes,
        // and observe the compiler process only (csc.exe).
        // Debug.WriteLine(code);
        context.AddSource("SourceGeneratedTestNodesBuilder.g.cs", code);

        IndentedStringBuilder hookCode = new();
        hookCode.AppendAutoGeneratedHeader();
        using (hookCode.AppendBlock("namespace Microsoft.Testing.Framework.SourceGeneration"))
        {
            hookCode.AppendLine("[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
            using (hookCode.AppendBlock("public static class SourceGeneratedTestingPlatformBuilderHook"))
            {
                using (hookCode.AppendBlock("public static void AddExtensions(Microsoft.Testing.Platform.Builder.ITestApplicationBuilder testApplicationBuilder, string[] _)"))
                {
                    hookCode.AppendLine("testApplicationBuilder.AddTestFramework(new Microsoft.Testing.Framework.Configurations.TestFrameworkConfiguration(System.Environment.ProcessorCount),");
                    hookCode.IndentationLevel++;
                    hookCode.AppendLine($"new {(safeAssemblyName is not null ? safeAssemblyName + "." : string.Empty)}SourceGeneratedTestNodesBuilder());");
                    hookCode.IndentationLevel--;
                }
            }
        }

        // Add a hook to the test platform builder to register the test framework to MSBuild.
        context.AddSource("SourceGeneratedTestingPlatformBuilderHook.g.cs", hookCode.ToString());
    }

    private static void AppendAssemblyTestNodeBuilderContent(IndentedStringBuilder sourceStringBuilder, string assemblyName,
        ImmutableArray<TestTypeInfo> testClasses)
    {
        Dictionary<TestNamespaceInfo, string> rootVariablesPerNamespace = new();
        int variableIndex = 1;
        IEnumerable<IGrouping<TestNamespaceInfo, TestTypeInfo>> classesPerNamespaces = testClasses.GroupBy(x => x.ContainingNamespace);
        foreach (IGrouping<TestNamespaceInfo, TestTypeInfo> namespaceClasses in classesPerNamespaces)
        {
            string namespaceTestsVariableName = $"namespace{variableIndex}Tests";
            rootVariablesPerNamespace.Add(namespaceClasses.Key, namespaceTestsVariableName);
            sourceStringBuilder.AppendLine($"ColGen::List<MSTF::TestNode> {namespaceTestsVariableName} = new();");

            foreach (TestTypeInfo testClassInfo in namespaceClasses)
            {
                string escapedClassFullName = TestNodeHelpers.GenerateEscapedName(testClassInfo.FullyQualifiedName);
                sourceStringBuilder.AppendLine($"{namespaceTestsVariableName}.Add({testClassInfo.GeneratedTypeName}.TestNode);");
            }

            variableIndex++;
            sourceStringBuilder.AppendLine();
        }

        sourceStringBuilder.Append("MSTF::TestNode root = ");

        using (sourceStringBuilder.AppendTestNode(assemblyName, assemblyName, Array.Empty<string>(), ';'))
        {
            foreach (IGrouping<TestNamespaceInfo, TestTypeInfo> group in classesPerNamespaces)
            {
                group.Key.AppendNamespaceTestNode(sourceStringBuilder, rootVariablesPerNamespace[group.Key]);
            }
        }

        sourceStringBuilder.AppendLine();
        sourceStringBuilder.AppendLine("return Tasks::Task.FromResult(new MSTF::TestNode[1] { root });");
    }

    private static void AddTestClassNode(SourceProductionContext context, TestTypeInfo testClassInfo)
    {
        var sourceStringBuilder = new IndentedStringBuilder();
        sourceStringBuilder.AppendAutoGeneratedHeader();
        sourceStringBuilder.AppendLine();

        testClassInfo.AppendTestNode(sourceStringBuilder);

        string code = sourceStringBuilder.ToString();
        // DEBUG: Debug.WriteLine is useful to observe the code when changing the source code generator or applying it to a new test suite.
        // VS is caching the generator, so start DebugView++ and just rebuild the TestContainer to make changes,
        // and observe the compiler process only (csc.exe).
        // Debug.WriteLine(code);
        context.AddSource($"{testClassInfo.FullyQualifiedName}.g.cs", code);
    }

    // Borrowed from https://github.com/dotnet/templating/blob/dad34814012bf29aa35eaf8e8013af4b10b997da/src/Microsoft.TemplateEngine.Orchestrator.RunnableProjects/ValueForms/DefaultSafeNamespaceValueFormFactory.cs#L10
    public static string ToSafeNamespace(string value)
    {
        const char invalidCharacterReplacement = '_';

        value = value ?? throw new ArgumentNullException(nameof(value));
        value = value.Trim();

        StringBuilder safeValueStr = new(value.Length);

        for (int i = 0; i < value.Length; i++)
        {
            if (i < value.Length - 1 && char.IsSurrogatePair(value[i], value[i + 1]))
            {
                safeValueStr.Append(invalidCharacterReplacement);
                // Skip both chars that make up this symbol.
                i++;
                continue;
            }

            bool isFirstCharacterOfIdentifier = safeValueStr.Length == 0 || safeValueStr[safeValueStr.Length - 1] == '.';
            bool isValidFirstCharacter = UnicodeCharacterUtilities.IsIdentifierStartCharacter(value[i]);
            bool isValidPartCharacter = UnicodeCharacterUtilities.IsIdentifierPartCharacter(value[i]);

            if (isFirstCharacterOfIdentifier && !isValidFirstCharacter && isValidPartCharacter)
            {
                // This character cannot be at the beginning, but is good otherwise. Prefix it with something valid.
                safeValueStr.Append(invalidCharacterReplacement);
                safeValueStr.Append(value[i]);
            }
            else if ((isFirstCharacterOfIdentifier && isValidFirstCharacter)
                || (!isFirstCharacterOfIdentifier && isValidPartCharacter)
                || (safeValueStr.Length > 0 && i < value.Length - 1 && value[i] == '.'))
            {
                // This character is allowed to be where it is.
                safeValueStr.Append(value[i]);
            }
            else
            {
                safeValueStr.Append(invalidCharacterReplacement);
            }
        }

        return safeValueStr.ToString();
    }
}
