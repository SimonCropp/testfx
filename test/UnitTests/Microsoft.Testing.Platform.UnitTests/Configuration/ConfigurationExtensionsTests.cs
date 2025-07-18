﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Platform.Configurations;

using Moq;

namespace Microsoft.Testing.Platform.UnitTests;

[TestClass]
public sealed class ConfigurationExtensionsTests
{
    private static string GetActualValueFromConfiguration(IConfiguration configuration, string key) => key switch
    {
        PlatformConfigurationConstants.PlatformResultDirectory => configuration.GetTestResultDirectory(),
        PlatformConfigurationConstants.PlatformCurrentWorkingDirectory => configuration.GetCurrentWorkingDirectory(),
        _ => throw new ArgumentException("Unsupported key."),
    };

    [TestMethod]
    [DataRow(PlatformConfigurationConstants.PlatformResultDirectory)]
    [DataRow(PlatformConfigurationConstants.PlatformCurrentWorkingDirectory)]
    public void ConfigurationExtensions_TestedMethod_ReturnsExpectedPath(string key)
    {
        string expectedPath = Path.Combine("a", "b", "c");

        Mock<IConfiguration> configuration = new();
        configuration
            .Setup(configuration => configuration[key])
            .Returns(expectedPath);

        Assert.AreEqual(expectedPath, GetActualValueFromConfiguration(configuration.Object, key));
    }

    [TestMethod]
    [DataRow(PlatformConfigurationConstants.PlatformResultDirectory)]
    [DataRow(PlatformConfigurationConstants.PlatformCurrentWorkingDirectory)]
    public void ConfigurationExtensions_TestedMethod_ThrowsArgumentNullException(string key)
    {
        Mock<IConfiguration> configuration = new();
        configuration
            .Setup(configuration => configuration[key])
            .Returns(value: null);

        Assert.ThrowsExactly<ArgumentNullException>(() => GetActualValueFromConfiguration(configuration.Object, key));
    }
}
