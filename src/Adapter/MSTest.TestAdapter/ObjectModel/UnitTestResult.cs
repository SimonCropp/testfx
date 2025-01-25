﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.Extensions;

namespace Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.ObjectModel;

[Serializable]
[DebuggerDisplay("{DisplayName} ({Outcome})")]
#if RELEASE
#if NET6_0_OR_GREATER
[Obsolete(Constants.PublicTypeObsoleteMessage, DiagnosticId = "MSTESTOBS")]
#else
[Obsolete(Constants.PublicTypeObsoleteMessage)]
#endif
#endif
public class UnitTestResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitTestResult"/> class.
    /// </summary>
    internal UnitTestResult() => DatarowIndex = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitTestResult"/> class.
    /// </summary>
    /// <param name="testFailedException"> The test failed exception. </param>
    internal UnitTestResult(TestFailedException testFailedException)
        : this()
    {
        Outcome = testFailedException.Outcome.ToUnitTestOutcome();
        ErrorMessage = testFailedException.Message;

        if (testFailedException.StackTraceInformation != null)
        {
            ErrorStackTrace = testFailedException.StackTraceInformation.ErrorStackTrace;
            ErrorLineNumber = testFailedException.StackTraceInformation.ErrorLineNumber;
            ErrorFilePath = testFailedException.StackTraceInformation.ErrorFilePath;
            ErrorColumnNumber = testFailedException.StackTraceInformation.ErrorColumnNumber;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitTestResult"/> class.
    /// </summary>
    /// <param name="outcome"> The outcome. </param>
    /// <param name="errorMessage"> The error message. </param>
    internal UnitTestResult(UnitTestOutcome outcome, string? errorMessage)
        : this()
    {
        Outcome = outcome;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets the display name for the result.
    /// </summary>
    public string? DisplayName { get; internal set; }

    /// <summary>
    /// Gets the outcome of the result.
    /// </summary>
    public UnitTestOutcome Outcome { get; internal set; }

    /// <summary>
    /// Gets the errorMessage of the result.
    /// </summary>
    public string? ErrorMessage { get; internal set; }

    /// <summary>
    /// Gets the stackTrace of the result.
    /// </summary>
    public string? ErrorStackTrace { get; internal set; }

    /// <summary>
    /// Gets the execution id of the result.
    /// </summary>
    public Guid ExecutionId { get; internal set; }

    /// <summary>
    /// Gets the parent execution id of the result.
    /// </summary>
    public Guid ParentExecId { get; internal set; }

    /// <summary>
    /// Gets the inner results count of the result.
    /// </summary>
    public int InnerResultsCount { get; internal set; }

    /// <summary>
    /// Gets the duration of the result.
    /// </summary>
    public TimeSpan Duration { get; internal set; }

    /// <summary>
    /// Gets the standard output of the result.
    /// </summary>
    public string? StandardOut { get; internal set; }

    /// <summary>
    /// Gets the Standard Error of the result.
    /// </summary>
    public string? StandardError { get; internal set; }

    /// <summary>
    /// Gets the debug trace of the result.
    /// </summary>
    public string? DebugTrace { get; internal set; }

    /// <summary>
    /// Gets additional information messages generated by TestContext.WriteLine.
    /// </summary>
    public string? TestContextMessages { get; internal set; }

    /// <summary>
    /// Gets the source code FilePath where the error was thrown.
    /// </summary>
    public string? ErrorFilePath { get; internal set; }

    /// <summary>
    /// Gets the line number in the source code file where the error was thrown.
    /// </summary>
    public int ErrorLineNumber { get; private set; }

    /// <summary>
    /// Gets the column number in the source code file where the error was thrown.
    /// </summary>
    public int ErrorColumnNumber { get; private set; }

    /// <summary>
    /// Gets data row index in data source. Set only for results of individual
    /// run of data row of a data driven test.
    /// </summary>
    public int DatarowIndex { get; internal set; }

    /// <summary>
    /// Gets the result files attached by the test.
    /// </summary>
    public IList<string>? ResultFiles { get; internal set; }
}
