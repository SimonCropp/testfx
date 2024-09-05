﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Testing.Platform.IPC;

// WARNING: Please note this file needs to be kept aligned with the one in the dotnet sdk.
// The protocol follows the concept of optional properties.
// The id is used to identify the property in the stream and it will be skipped if it's not recognized.
// We can add new properties with new ids, but we CANNOT change the existing ids (to support backwards compatibility).
internal static class VoidResponseFieldsId
{
    public const int MessagesSerializerId = 0;
}

internal static class TestHostProcessExitRequestFieldsId
{
    public const int MessagesSerializerId = 1;
}

internal static class TestHostProcessPIDRequestFieldsId
{
    public const int MessagesSerializerId = 2;
}

internal static class CommandLineOptionMessagesFieldsId
{
    public const int MessagesSerializerId = 3;

    public const ushort ModulePath = 1;
    public const ushort CommandLineOptionMessageList = 2;
}

internal static class CommandLineOptionMessageFieldsId
{
    public const ushort Name = 1;
    public const ushort Description = 2;
    public const ushort IsHidden = 3;
    public const ushort IsBuiltIn = 4;
}

internal static class DiscoveredTestMessagesFieldsId
{
    public const int MessagesSerializerId = 5;

    public const ushort ExecutionId = 1;
    public const ushort DiscoveredTestMessageList = 2;
}

internal static class DiscoveredTestMessageFieldsId
{
    public const ushort Uid = 1;
    public const ushort DisplayName = 2;
}

internal static class TestResultMessagesFieldsId
{
    public const int MessagesSerializerId = 6;

    public const ushort ExecutionId = 1;
    public const ushort SuccessfulTestMessageList = 2;
    public const ushort FailedTestMessageList = 3;
}

internal static class SuccessfulTestResultMessageFieldsId
{
    public const ushort Uid = 1;
    public const ushort DisplayName = 2;
    public const ushort State = 3;
    public const ushort Reason = 4;
    public const ushort SessionUid = 5;
}

internal static class FailedTestResultMessageFieldsId
{
    public const ushort Uid = 1;
    public const ushort DisplayName = 2;
    public const ushort State = 3;
    public const ushort Reason = 4;
    public const ushort ErrorMessage = 5;
    public const ushort ErrorStackTrace = 6;
    public const ushort SessionUid = 7;
}

internal static class FileArtifactMessagesFieldsId
{
    public const int MessagesSerializerId = 7;

    public const ushort ExecutionId = 1;
    public const ushort FileArtifactMessageList = 2;
}

internal static class FileArtifactMessageFieldsId
{
    public const ushort FullPath = 1;
    public const ushort DisplayName = 2;
    public const ushort Description = 3;
    public const ushort TestUid = 4;
    public const ushort TestDisplayName = 5;
    public const ushort SessionUid = 6;
}

internal static class TestSessionEventFieldsId
{
    public const int MessagesSerializerId = 8;

    public const ushort SessionType = 1;
    public const ushort SessionUid = 2;
    public const ushort ExecutionId = 3;
}

internal static class HandshakeMessageFieldsId
{
    public const int MessagesSerializerId = 9;
}