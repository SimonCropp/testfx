﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

using Analyzer.Utilities.Extensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using MSTest.Analyzers.Helpers;

namespace MSTest.Analyzers;

/// <summary>
/// MSTEST0007: <inheritdoc cref="Resources.UseAttributeOnTestMethodAnalyzerTitle"/>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public sealed class UseAttributeOnTestMethodAnalyzer : DiagnosticAnalyzer
{
    private const string OwnerAttributeShortName = "Owner";
    internal static readonly DiagnosticDescriptor OwnerRule = DiagnosticDescriptorHelper.Create(
        DiagnosticIds.UseAttributeOnTestMethodRuleId,
        title: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerTitle), Resources.ResourceManager, typeof(Resources), OwnerAttributeShortName),
        messageFormat: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources), OwnerAttributeShortName),
        description: null,
        Category.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    private const string PriorityAttributeShortName = "Priority";
    internal static readonly DiagnosticDescriptor PriorityRule = DiagnosticDescriptorHelper.Create(
        DiagnosticIds.UseAttributeOnTestMethodRuleId,
        title: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerTitle), Resources.ResourceManager, typeof(Resources), PriorityAttributeShortName),
        messageFormat: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources), PriorityAttributeShortName),
        description: null,
        Category.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    private const string TestPropertyAttributeShortName = "TestProperty";
    internal static readonly DiagnosticDescriptor TestPropertyRule = DiagnosticDescriptorHelper.Create(
        DiagnosticIds.UseAttributeOnTestMethodRuleId,
        title: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerTitle), Resources.ResourceManager, typeof(Resources), TestPropertyAttributeShortName),
        messageFormat: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources), TestPropertyAttributeShortName),
        description: null,
        Category.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    private const string WorkItemAttributeShortName = "WorkItem";
    internal static readonly DiagnosticDescriptor WorkItemRule = DiagnosticDescriptorHelper.Create(
        DiagnosticIds.UseAttributeOnTestMethodRuleId,
        title: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerTitle), Resources.ResourceManager, typeof(Resources), WorkItemAttributeShortName),
        messageFormat: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources), WorkItemAttributeShortName),
        description: null,
        Category.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    private const string DescriptionAttributeShortName = "Description";
    internal static readonly DiagnosticDescriptor DescriptionRule = DiagnosticDescriptorHelper.Create(
        DiagnosticIds.UseAttributeOnTestMethodRuleId,
        title: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerTitle), Resources.ResourceManager, typeof(Resources), DescriptionAttributeShortName),
        messageFormat: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources), DescriptionAttributeShortName),
        description: null,
        Category.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    private const string ExpectedExceptionAttributeShortName = "ExpectedException";
    internal static readonly DiagnosticDescriptor ExpectedExceptionRule = DiagnosticDescriptorHelper.Create(
        DiagnosticIds.UseAttributeOnTestMethodRuleId,
        title: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerTitle), Resources.ResourceManager, typeof(Resources), ExpectedExceptionAttributeShortName),
        messageFormat: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources), ExpectedExceptionAttributeShortName),
        description: null,
        Category.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    private const string CssIterationAttributeShortName = "CssIteration";
    internal static readonly DiagnosticDescriptor CssIterationRule = DiagnosticDescriptorHelper.Create(
        DiagnosticIds.UseAttributeOnTestMethodRuleId,
        title: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerTitle), Resources.ResourceManager, typeof(Resources), CssIterationAttributeShortName),
        messageFormat: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources), CssIterationAttributeShortName),
        description: null,
        Category.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    private const string CssProjectStructureAttributeShortName = "CssProjectStructure";
    internal static readonly DiagnosticDescriptor CssProjectStructureRule = DiagnosticDescriptorHelper.Create(
        DiagnosticIds.UseAttributeOnTestMethodRuleId,
        title: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerTitle), Resources.ResourceManager, typeof(Resources), CssProjectStructureAttributeShortName),
        messageFormat: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources), CssProjectStructureAttributeShortName),
        description: null,
        Category.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    private const string ConditionBaseAttributeShortName = "ConditionBaseAttribute";
    internal static readonly DiagnosticDescriptor ConditionBaseRule = DiagnosticDescriptorHelper.Create(
        DiagnosticIds.UseAttributeOnTestMethodRuleId,
        title: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerTitle), Resources.ResourceManager, typeof(Resources), ConditionBaseAttributeShortName),
        messageFormat: new LocalizableResourceString(
            nameof(Resources.UseAttributeOnTestMethodAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources), ConditionBaseAttributeShortName),
        description: null,
        Category.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    // IMPORTANT: Remember to add any new rule to the rule tuple.
    // IMPORTANT: The order is important. For example, Owner, Priority, and Description are also TestProperty. We report the first violation and then bail-out.
    //            It may be a better idea to consolidate OwnerRule, PriorityRule, DescriptionRule, and TestPropertyRule into a single rule.
    private static readonly List<(string AttributeFullyQualifiedName, DiagnosticDescriptor Rule)> RuleTuples =
    [
        (WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingOwnerAttribute, OwnerRule),
        (WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingPriorityAttribute, PriorityRule),
        (WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingDescriptionAttribute, DescriptionRule),
        (WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingTestPropertyAttribute, TestPropertyRule),
        (WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingWorkItemAttribute, WorkItemRule),
        (WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingExpectedExceptionBaseAttribute, ExpectedExceptionRule),
        (WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingCssIterationAttribute, CssIterationRule),
        (WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingCssProjectStructureAttribute, CssProjectStructureRule),
        (WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingConditionBaseAttribute, ConditionBaseRule),
    ];

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            OwnerRule,
            PriorityRule,
            TestPropertyRule,
            WorkItemRule,
            DescriptionRule,
            ExpectedExceptionRule,
            CssIterationRule,
            CssProjectStructureRule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(context =>
        {
            // No need to register any action if we don't find the TestMethodAttribute symbol since
            // the current analyzer checks if certain test attributes are applied on test methods. No
            // test methods, nothing to check.
            if (!context.Compilation.TryGetOrCreateTypeByMetadataName(
                WellKnownTypeNames.MicrosoftVisualStudioTestToolsUnitTestingTestMethodAttribute,
                out INamedTypeSymbol? testMethodAttributeSymbol))
            {
                return;
            }

            // Get a list of attributes and associated rules that are found in the current compilation
            // context.
            List<(INamedTypeSymbol AttributeSymbol, DiagnosticDescriptor Rule)> attributeRuleTuples = [];
            foreach ((string attributeFullyQualifiedName, DiagnosticDescriptor rule) in RuleTuples)
            {
                if (context.Compilation.TryGetOrCreateTypeByMetadataName(attributeFullyQualifiedName, out INamedTypeSymbol? attributeSymbol))
                {
                    attributeRuleTuples.Add((attributeSymbol, rule));
                }
            }

            // Should there be any test attributes present in the current assembly, we call upon
            // further analysis to make sure they are used as intended, see registered action for
            // more info.
            if (attributeRuleTuples.Count > 0)
            {
                context.RegisterSymbolAction(context => AnalyzeSymbol(context, testMethodAttributeSymbol, attributeRuleTuples), SymbolKind.Method);
            }
        });
    }

    private static void AnalyzeSymbol(
        SymbolAnalysisContext context,
        INamedTypeSymbol testMethodAttributeSymbol,
        IEnumerable<(INamedTypeSymbol AttributeSymbol, DiagnosticDescriptor Rule)> attributeRuleTuples)
    {
        var methodSymbol = (IMethodSymbol)context.Symbol;

        List<(AttributeData AttributeData, DiagnosticDescriptor Rule)> attributes = [];
        foreach (AttributeData methodAttribute in methodSymbol.GetAttributes())
        {
            // Current method should be a test method or should inherit from the TestMethod attribute.
            // If it is, the current analyzer will trigger no diagnostic so it exits.
            if (methodAttribute.AttributeClass.Inherits(testMethodAttributeSymbol))
            {
                return;
            }

            // Get all test attributes decorating the current method.
            foreach ((INamedTypeSymbol attributeSymbol, DiagnosticDescriptor rule) in attributeRuleTuples)
            {
                if (methodAttribute.AttributeClass.Inherits(attributeSymbol))
                {
                    attributes.Add((methodAttribute, rule));
                    break;
                }
            }
        }

        // If there's any test attributes decorating a non-test method we report diagnostics on the
        // said test attribute.
        foreach ((AttributeData AttributeData, DiagnosticDescriptor Rule) attribute in attributes)
        {
            if (attribute.AttributeData.ApplicationSyntaxReference?.GetSyntax() is { } syntax)
            {
                context.ReportDiagnostic(syntax.CreateDiagnostic(attribute.Rule));
            }
        }
    }
}
