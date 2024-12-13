﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Testing.Platform.Helpers;
using Microsoft.Testing.Platform.OutputDevice.Terminal;

namespace Microsoft.Testing.Platform.UnitTests;

[TestGroup]
public sealed class TerminalTestReporterTests : TestBase
{
    public TerminalTestReporterTests(ITestExecutionContext testExecutionContext)
        : base(testExecutionContext)
    {
    }

    public void AppendStackFrameFormatsStackTraceLineCorrectly()
    {
        var terminal = new StringBuilderTerminal();
        Exception err;
        try
        {
            throw new Exception();
        }
        catch (Exception ex)
        {
            err = ex;
        }

        string firstStackTraceLine = err.StackTrace!.Replace("\r", string.Empty).Split('\n')[0];
        TerminalTestReporter.AppendStackFrame(terminal, firstStackTraceLine);

#if NETCOREAPP
        Assert.Contains("    at Microsoft.Testing.Platform.UnitTests.TerminalTestReporterTests.AppendStackFrameFormatsStackTraceLineCorrectly() in ", terminal.Output);
#else
        Assert.Contains("    at Microsoft.Testing.Platform.UnitTests.TerminalTestReporterTests.AppendStackFrameFormatsStackTraceLineCorrectly()", terminal.Output);
#endif
        // Line number without the respective file
        Assert.That(!terminal.Output.ToString().Contains(" :0"));
    }

    // Code with line when we have symbols
    [Arguments(
        "   at TestingPlatformEntryPoint.Main(String[]) in /_/TUnit.TestProject/obj/Release/net8.0/osx-x64/TestPlatformEntryPoint.cs:line 16",
        $"    at TestingPlatformEntryPoint.Main(String[]) in /_/TUnit.TestProject/obj/Release/net8.0/osx-x64/TestPlatformEntryPoint.cs:16")]
    // code without line when we don't have symbols
    [Arguments(
        "   at TestingPlatformEntryPoint.<Main>(String[])",
        "    at TestingPlatformEntryPoint.<Main>(String[])")]
    // stack trace when published as NativeAOT
    [Arguments(
        "   at BenchmarkTest.ExceptionThrower.<Nested1>d__2.MoveNext() + 0x9d",
        "    at BenchmarkTest.ExceptionThrower.<Nested1>d__2.MoveNext() + 0x9d")]
    // spanners that we want to keep, to not lose information
    [Arguments(
        "--- End of stack trace from previous location ---",
        "    --- End of stack trace from previous location ---")]
    public void StackTraceRegexCapturesLines(string stackTraceLine, string expected)
    {
        var terminal = new StringBuilderTerminal();
        TerminalTestReporter.AppendStackFrame(terminal, stackTraceLine);

        // We add newline after every, but it is hard to put it in the attribute.
        expected += Environment.NewLine;

        Assert.AreEqual(expected, terminal.Output);
    }

    public void OutputFormattingIsCorrect()
    {
        var stringBuilderConsole = new StringBuilderConsole();
        var terminalReporter = new TerminalTestReporter(stringBuilderConsole, new TerminalTestReporterOptions
        {
            ShowPassedTests = () => true,
            UseAnsi = true,
            ForceAnsi = true,

            ShowAssembly = false,
            ShowAssemblyStartAndComplete = false,
            ShowProgress = () => false,
        });

        DateTimeOffset startTime = DateTimeOffset.MinValue;
        DateTimeOffset endTime = DateTimeOffset.MaxValue;
        terminalReporter.TestExecutionStarted(startTime, 1, isDiscovery: false);

        string targetFramework = "net8.0";
        string architecture = "x64";
        string assembly = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\work\assembly.dll" : "/mnt/work/assembly.dll";
        string folder = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\work\" : "/mnt/work/";
        string folderNoSlash = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\work" : "/mnt/work";
        string folderLink = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:/work/" : "mnt/work/";
        string folderLinkNoSlash = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:/work" : "mnt/work";

        terminalReporter.AssemblyRunStarted(assembly, targetFramework, architecture, executionId: null);
        string standardOutput = "Hello!";
        string errorOutput = "Oh no!";

        terminalReporter.TestCompleted(assembly, targetFramework, architecture, executionId: null, "PassedTest1", TestOutcome.Passed, TimeSpan.FromSeconds(10),
            errorMessage: null, exception: null, expected: null, actual: null, standardOutput, errorOutput);
        terminalReporter.TestCompleted(assembly, targetFramework, architecture, executionId: null, "SkippedTest1", TestOutcome.Skipped, TimeSpan.FromSeconds(10),
            errorMessage: null, exception: null, expected: null, actual: null, standardOutput, errorOutput);
        // timed out + canceled + failed should all report as failed in summary
        terminalReporter.TestCompleted(assembly, targetFramework, architecture, executionId: null, "TimedoutTest1", TestOutcome.Timeout, TimeSpan.FromSeconds(10),
            errorMessage: null, exception: null, expected: null, actual: null, standardOutput, errorOutput);
        terminalReporter.TestCompleted(assembly, targetFramework, architecture, executionId: null, "CanceledTest1", TestOutcome.Canceled, TimeSpan.FromSeconds(10),
            errorMessage: null, exception: null, expected: null, actual: null, standardOutput, errorOutput);
        terminalReporter.TestCompleted(assembly, targetFramework, architecture, executionId: null, "FailedTest1", TestOutcome.Fail, TimeSpan.FromSeconds(10),
            errorMessage: "Tests failed", exception: new StackTraceException(@$"   at FailingTest() in {folder}codefile.cs:line 10"), expected: "ABC", actual: "DEF", standardOutput, errorOutput);
        terminalReporter.ArtifactAdded(outOfProcess: true, assembly, targetFramework, architecture, executionId: null, testName: null, @$"{folder}artifact1.txt");
        terminalReporter.ArtifactAdded(outOfProcess: false, assembly, targetFramework, architecture, executionId: null, testName: null, @$"{folder}artifact2.txt");
        terminalReporter.AssemblyRunCompleted(assembly, targetFramework, architecture, executionId: null, exitCode: null, outputData: null, errorData: null);
        terminalReporter.TestExecutionCompleted(endTime);

        string output = stringBuilderConsole.Output;

        string expected = $"""
            ␛[92mpassed␛[m PassedTest1␛[90m ␛[90m(10s 000ms)␛[m
            ␛[90m  Standard output
                Hello!
              Error output
                Oh no!
            ␛[m␛[93mskipped␛[m SkippedTest1␛[90m ␛[90m(10s 000ms)␛[m
            ␛[90m  Standard output
                Hello!
              Error output
                Oh no!
            ␛[m␛[91mfailed (canceled)␛[m TimedoutTest1␛[90m ␛[90m(10s 000ms)␛[m
            ␛[90m  Standard output
                Hello!
              Error output
                Oh no!
            ␛[m␛[91mfailed (canceled)␛[m CanceledTest1␛[90m ␛[90m(10s 000ms)␛[m
            ␛[90m  Standard output
                Hello!
              Error output
                Oh no!
            ␛[m␛[91mfailed␛[m FailedTest1␛[90m ␛[90m(10s 000ms)␛[m
            ␛[91m  Tests failed
            ␛[m␛[91m  Expected
                ABC
              Actual
                DEF
            ␛[m␛[90m    at FailingTest() in ␛[90m␛]8;;file:///{folderLink}codefile.cs␛\{folder}codefile.cs:10␛]8;;␛\␛[m␛[90m
            ␛[m␛[90m  Standard output
                Hello!
              Error output
                Oh no!
            ␛[m

              Out of process file artifacts produced:
                - ␛[90m␛]8;;file:///{folderLink}artifact1.txt␛\{folder}artifact1.txt␛]8;;␛\␛[m
              In process file artifacts produced:
                - ␛[90m␛]8;;file:///{folderLink}artifact2.txt␛\{folder}artifact2.txt␛]8;;␛\␛[m
            ␛[91mTest run summary: Failed!␛[90m - ␛[m␛[90m␛]8;;file:///{folderLinkNoSlash}␛\{folder}assembly.dll␛]8;;␛\␛[m (net8.0|x64)
            ␛[m  total: 5
            ␛[91m  failed: 3
            ␛[m  succeeded: 1
              skipped: 1
              duration: 3652058d 23h 59m 59s 999ms

            """;

        Assert.AreEqual(expected, ShowEscape(output));
    }

    private static string? ShowEscape(string? text)
    {
        string visibleEsc = "\x241b";
        return text?.Replace(AnsiCodes.Esc, visibleEsc);
    }

    internal class StringBuilderConsole : IConsole
    {
        private readonly StringBuilder _output = new();

        public int BufferHeight => int.MaxValue;

        public int BufferWidth => int.MinValue;

        public bool IsOutputRedirected => throw new NotImplementedException();

        public string Output => _output.ToString();

        public event ConsoleCancelEventHandler? CancelKeyPress = (sender, e) => { };

        public void Clear() => throw new NotImplementedException();

        public ConsoleColor GetBackgroundColor() => throw new NotImplementedException();

        public ConsoleColor GetForegroundColor() => ConsoleColor.White;

        public void SetBackgroundColor(ConsoleColor color) => throw new NotImplementedException();

        public void SetForegroundColor(ConsoleColor color)
        {
            // do nothing
        }

        public void Write(string format, object?[]? args) => throw new NotImplementedException();

        public void Write(string? value) => _output.Append(value);

        public void Write(char value) => _output.Append(value);

        public void WriteLine() => _output.AppendLine();

        public void WriteLine(string? value) => _output.AppendLine(value);

        public void WriteLine(object? value) => throw new NotImplementedException();

        public void WriteLine(string format, object? arg0) => throw new NotImplementedException();

        public void WriteLine(string format, object? arg0, object? arg1) => throw new NotImplementedException();

        public void WriteLine(string format, object? arg0, object? arg1, object? arg2) => throw new NotImplementedException();

        public void WriteLine(string format, object?[]? args) => throw new NotImplementedException();
    }

    internal class StringBuilderTerminal : ITerminal
    {
        private readonly StringBuilder _stringBuilder;

        public StringBuilderTerminal()
            => _stringBuilder = new();

        public string Output => _stringBuilder.ToString();

        public int Width => throw new NotImplementedException();

        public int Height => throw new NotImplementedException();

        public void Append(char value) => _stringBuilder.Append(value);

        public void Append(string value) => _stringBuilder.Append(value);

        public void AppendLine() => _stringBuilder.AppendLine();

        public void AppendLine(string value) => _stringBuilder.AppendLine(value);

        public void AppendLink(string path, int? lineNumber)
        {
            _stringBuilder.Append(path);
            _stringBuilder.Append(':');
            _stringBuilder.Append(lineNumber);
        }

        public void EraseProgress() => throw new NotImplementedException();

        public void HideCursor() => throw new NotImplementedException();

        public void RenderProgress(TestProgressState?[] progress) => throw new NotImplementedException();

        public void ResetColor()
        {
            // do nothing
        }

        public void SetColor(TerminalColor color)
        {
            // do nothing
        }

        public void ShowCursor() => throw new NotImplementedException();

        public void StartBusyIndicator() => throw new NotImplementedException();

        public void StartUpdate() => throw new NotImplementedException();

        public void StopBusyIndicator() => throw new NotImplementedException();

        public void StopUpdate() => throw new NotImplementedException();
    }

    private class StackTraceException : Exception
    {
        public StackTraceException(string stackTrace) => StackTrace = stackTrace;

        public override string? StackTrace { get; }
    }
}
