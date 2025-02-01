// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CS8618 // Properties below are set by MSBuild.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Testing.Platform.MSBuild;

public sealed class TestingPlatformEntryPointTask : Build.Utilities.Task
{
    private const string CSharpLanguageSymbol = "C#";
    private const string FSharpLanguageSymbol = "F#";
    private const string VBLanguageSymbol = "VB";

    public TestingPlatformEntryPointTask()
        : this(new FileSystem())
    {
    }

    internal TestingPlatformEntryPointTask(IFileSystem fileSystem)
    {
        if (Environment.GetEnvironmentVariable("TESTINGPLATFORM_MSBUILD_LAUNCH_ATTACH_DEBUGGER") == "1")
        {
            Debugger.Launch();
        }

        _fileSystem = fileSystem;
    }

    [Required]
    public ITaskItem TestingPlatformEntryPointSourcePath { get; set; }

    [Required]
    public ITaskItem Language { get; set; }

    [Required]
    public string RootNamespace { get; set; }

    [Output]
    public ITaskItem TestingPlatformEntryPointGeneratedFilePath { get; set; }

    private readonly IFileSystem _fileSystem;

    public override bool Execute()
    {
        Log.LogMessage(MessageImportance.Normal, $"TestingPlatformEntryPointSourcePath: '{TestingPlatformEntryPointSourcePath.ItemSpec}'");
        Log.LogMessage(MessageImportance.Normal, $"Language: '{Language.ItemSpec}'");

        if (!Language.ItemSpec.Equals(CSharpLanguageSymbol, StringComparison.OrdinalIgnoreCase) &&
            !Language.ItemSpec.Equals(VBLanguageSymbol, StringComparison.OrdinalIgnoreCase) &&
            !Language.ItemSpec.Equals(FSharpLanguageSymbol, StringComparison.OrdinalIgnoreCase))
        {
            TestingPlatformEntryPointGeneratedFilePath = default!;
            Log.LogError($"Language '{Language.ItemSpec}' is not supported.");
        }
        else
        {
            GenerateEntryPoint(Language.ItemSpec, RootNamespace, TestingPlatformEntryPointSourcePath, _fileSystem, Log);
            TestingPlatformEntryPointGeneratedFilePath = TestingPlatformEntryPointSourcePath;
        }

        return !Log.HasLoggedErrors;
    }

    private static void GenerateEntryPoint(string language, string rootNamespace, ITaskItem testingPlatformEntryPointSourcePath, IFileSystem fileSystem, TaskLoggingHelper taskLoggingHelper)
    {
        string entryPointSource = GetEntryPointSourceCode(language, rootNamespace);
        taskLoggingHelper.LogMessage(MessageImportance.Normal, $"Entrypoint source:\n'{entryPointSource}'");
        fileSystem.WriteAllText(testingPlatformEntryPointSourcePath.ItemSpec, entryPointSource);
    }

    private static string GetEntryPointSourceCode(string language, string rootNamespace)
    {
        if (language == CSharpLanguageSymbol)
        {
            return string.IsNullOrEmpty(rootNamespace)
                ? $$"""
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Microsoft.Testing.Platform.MSBuild
// </auto-generated>
//------------------------------------------------------------------------------

[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal sealed class TestingPlatformEntryPoint
{
    public static async global::System.Threading.Tasks.Task<int> Main(string[] args)
    {
        global::Microsoft.Testing.Platform.Builder.ITestApplicationBuilder builder = await global::Microsoft.Testing.Platform.Builder.TestApplication.CreateBuilderAsync(args);
        SelfRegisteredExtensions.AddSelfRegisteredExtensions(builder, args);
        using (global::Microsoft.Testing.Platform.Builder.ITestApplication app = await builder.BuildAsync())
        {
            return await app.RunAsync();
        }
    }
}
"""
                : $$"""
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Microsoft.Testing.Platform.MSBuild
// </auto-generated>
//------------------------------------------------------------------------------

namespace {{rootNamespace}}
{
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal sealed class TestingPlatformEntryPoint
    {
        public static async global::System.Threading.Tasks.Task<int> Main(string[] args)
        {
            global::Microsoft.Testing.Platform.Builder.ITestApplicationBuilder builder = await global::Microsoft.Testing.Platform.Builder.TestApplication.CreateBuilderAsync(args);
            SelfRegisteredExtensions.AddSelfRegisteredExtensions(builder, args);
            using (global::Microsoft.Testing.Platform.Builder.ITestApplication app = await builder.BuildAsync())
            {
                return await app.RunAsync();
            }
        }
    }
}
""";
        }
        else if (language == VBLanguageSymbol)
        {
            return string.IsNullOrEmpty(rootNamespace)
                ? $$"""
'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by Microsoft.Testing.Platform.MSBuild
' </auto-generated>
'------------------------------------------------------------------------------

<System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage>
Module TestingPlatformEntryPoint

    Function Main(args As String()) As Integer
        Return MainAsync(args).GetAwaiter().GetResult()
    End Function

    Public Async Function MainAsync(args As String()) As Global.System.Threading.Tasks.Task(Of Integer)
        Dim builder = Await Global.Microsoft.Testing.Platform.Builder.TestApplication.CreateBuilderAsync(args)
        SelfRegisteredExtensions.AddSelfRegisteredExtensions(builder, args)
        Using testApplication = Await builder.BuildAsync()
            Return Await testApplication.RunAsync()
        End Using
    End Function

End Module
"""
                : $$"""
'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by Microsoft.Testing.Platform.MSBuild
' </auto-generated>
'------------------------------------------------------------------------------

Namespace {{rootNamespace}}
    <System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage>
    Module TestingPlatformEntryPoint

        Function Main(args As String()) As Integer
            Return MainAsync(args).GetAwaiter().GetResult()
        End Function

        Public Async Function MainAsync(args As String()) As Global.System.Threading.Tasks.Task(Of Integer)
            Dim builder = Await Global.Microsoft.Testing.Platform.Builder.TestApplication.CreateBuilderAsync(args)
            SelfRegisteredExtensions.AddSelfRegisteredExtensions(builder, args)
            Using testApplication = Await builder.BuildAsync()
                Return Await testApplication.RunAsync()
            End Using
        End Function

    End Module
End Namespace
""";
        }
        else if (language == FSharpLanguageSymbol)
        {
            return string.IsNullOrEmpty(rootNamespace)
                ? $$"""
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Microsoft.Testing.Platform.MSBuild
// </auto-generated>
//------------------------------------------------------------------------------

module TestingPlatformEntryPoint =

    [<System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage>]
    [<EntryPoint>]
    let main args =
        task {
            let! builder = Microsoft.Testing.Platform.Builder.TestApplication.CreateBuilderAsync args
            Microsoft.TestingPlatform.Extensions.SelfRegisteredExtensions.AddSelfRegisteredExtensions(builder, args)
            use! app = builder.BuildAsync()
            return! app.RunAsync()
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously
"""
                : $$"""
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by Microsoft.Testing.Platform.MSBuild
// </auto-generated>
//------------------------------------------------------------------------------

namespace {{rootNamespace}}

module TestingPlatformEntryPoint =

    [<System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage>]
    [<EntryPoint>]
    let main args =
        task {
            let! builder = Microsoft.Testing.Platform.Builder.TestApplication.CreateBuilderAsync args
            SelfRegisteredExtensions.AddSelfRegisteredExtensions(builder, args)
            use! app = builder.BuildAsync()
            return! app.RunAsync()
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously
""";
        }

        throw new InvalidOperationException($"Language not supported '{language}'");
    }
}
