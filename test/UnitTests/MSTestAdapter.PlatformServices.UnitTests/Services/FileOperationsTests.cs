﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET462
using Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices;

using TestFramework.ForTestingMSTest;

namespace MSTestAdapter.PlatformServices.Tests.Services;

public class FileOperationsTests : TestContainer
{
    private readonly FileOperations _fileOperations;

    public FileOperationsTests() => _fileOperations = new FileOperations();

    public void LoadAssemblyShouldThrowExceptionIfTheFileNameHasInvalidCharacters()
    {
        string filePath = "temp<>txt";
        void A() => _fileOperations.LoadAssembly(filePath, false);

#if NETCOREAPP
        VerifyThrows<FileNotFoundException>(A);
#else
        VerifyThrows<ArgumentException>(A);
#endif
    }

    public void LoadAssemblyShouldThrowExceptionIfFileIsNotFound() =>
        VerifyThrows<FileNotFoundException>(() => _fileOperations.LoadAssembly("temptxt", false));

    public void LoadAssemblyShouldLoadAssemblyInCurrentContext()
    {
        string filePath = typeof(FileOperationsTests).Assembly.Location;

        // This should not throw.
        _fileOperations.LoadAssembly(filePath, false);
    }

#if !WIN_UI
    public void DoesFileExistReturnsTrueForAllFiles()
    {
        Verify(_fileOperations.DoesFileExist(null!));
        Verify(_fileOperations.DoesFileExist("foobar"));
    }
#endif

    public void GetFullFilePathShouldReturnAssemblyFileName()
    {
        Verify(_fileOperations.GetFullFilePath(null!) is null);
        Verify(_fileOperations.GetFullFilePath("assemblyFileName") == "assemblyFileName");
    }
}
#pragma warning restore SA1649 // SA1649FileNameMustMatchTypeName

#endif
