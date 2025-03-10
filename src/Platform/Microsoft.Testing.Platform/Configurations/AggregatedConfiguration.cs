﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Helpers;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Services;

namespace Microsoft.Testing.Platform.Configurations;

internal sealed class AggregatedConfiguration(IConfigurationProvider[] configurationProviders, ITestApplicationModuleInfo testApplicationModuleInfo, IFileSystem fileSystem) : IConfiguration
{
    public const string DefaultTestResultFolderName = "TestResults";
    private readonly IConfigurationProvider[] _configurationProviders = configurationProviders;
    private readonly ITestApplicationModuleInfo _testApplicationModuleInfo = testApplicationModuleInfo;
    private readonly IFileSystem _fileSystem = fileSystem;
    private string? _resultDirectory;
    private string? _currentWorkingDirectory;
    private string? _testHostWorkingDirectory;

    public string? this[string key]
    {
        get
        {
            if (key == PlatformConfigurationConstants.PlatformResultDirectory && _resultDirectory is not null)
            {
                return _resultDirectory;
            }

            if (key == PlatformConfigurationConstants.PlatformCurrentWorkingDirectory && _currentWorkingDirectory is not null)
            {
                return _currentWorkingDirectory;
            }

            if (key == PlatformConfigurationConstants.PlatformTestHostWorkingDirectory && _testHostWorkingDirectory is not null)
            {
                return _testHostWorkingDirectory;
            }

            foreach (IConfigurationProvider source in _configurationProviders)
            {
                if (source.TryGet(key, out string? value))
                {
                    return value;
                }
            }

            return null;
        }
    }

    public /* for testing */ void SetResultDirectory(string resultDirectory) =>
        _resultDirectory = Guard.NotNull(resultDirectory);

    public /* for testing */ void SetCurrentWorkingDirectory(string workingDirectory) =>
        _currentWorkingDirectory = Guard.NotNull(workingDirectory);

    public void SetTestHostWorkingDirectory(string workingDirectory) =>
        _testHostWorkingDirectory = Guard.NotNull(workingDirectory);

    public async Task CheckTestResultsDirectoryOverrideAndCreateItAsync(ICommandLineOptions commandLineOptions, IFileLoggerProvider? fileLoggerProvider)
    {
        // Load Configuration
        _currentWorkingDirectory = _testApplicationModuleInfo.GetCurrentTestApplicationDirectory();

        string? resultDirectory = this[PlatformConfigurationConstants.PlatformResultDirectory];
        if (resultDirectory is null)
        {
            if (commandLineOptions.TryGetOptionArgumentList(PlatformCommandLineProvider.ResultDirectoryOptionKey, out string[]? resultDirectoryArg))
            {
                _resultDirectory = resultDirectoryArg[0];
            }
        }
        else
        {
            _resultDirectory = resultDirectory;
        }

        _resultDirectory ??= Path.Combine(_currentWorkingDirectory, DefaultTestResultFolderName);

        _resultDirectory = _fileSystem.CreateDirectory(_resultDirectory);

        // In case of the result directory is overridden by the config file we move logs to it.
        // This can happen in case of VSTest mode where the result directory is set to a different location.
        // This behavior is non documented and we reserve the right to change it in the future.
        if (fileLoggerProvider is not null)
        {
            await fileLoggerProvider.CheckLogFolderAndMoveToTheNewIfNeededAsync(_resultDirectory);
        }
    }
}
