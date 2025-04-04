﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// An enum that is used with <see cref="OSConditionAttribute"/> to control which operating systems a test method or test class supports or doesn't support.
/// </summary>
[Flags]
public enum OperatingSystems
{
    /// <summary>
    /// Represents the Linux operating system.
    /// </summary>
    Linux = 1 << 0,

    /// <summary>
    /// Representing the MacOS operating system.
    /// </summary>
    OSX = 1 << 1,

    /// <summary>
    /// Represents the Windows operating system.
    /// </summary>
    Windows = 1 << 2,

    /// <summary>
    /// Represents the FreeBSD operating system.
    /// </summary>
    FreeBSD = 1 << 3,
}
