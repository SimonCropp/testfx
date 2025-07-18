﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Testing.Platform.OutputDevice.Terminal;

/// <summary>
/// An artifact / attachment that was reported during run.
/// </summary>
internal sealed record TestRunArtifact(bool OutOfProcess, string? TestName, string Path);
