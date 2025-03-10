﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Testing.Platform.Helpers;

internal sealed class SystemTask : ITask
{
    public Task Run(Action action)
        => Task.Run(action);

    public Task Run(Func<Task> function, CancellationToken cancellationToken)
        => Task.Run(function, cancellationToken);

    public Task<T> Run<T>(Func<Task<T>?> function, CancellationToken cancellationToken)
        => Task.Run(function, cancellationToken);

    public Task RunLongRunning(Func<Task> action, string name, CancellationToken cancellationToken)
    {
        // We create custom thread so we can assign the name that will help us to identify the thread in the dump
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        TaskCompletionSource<int> taskCompletionSource = new();
        Thread thread = new(() =>
        {
            try
            {
                action().GetAwaiter().GetResult();
                taskCompletionSource.SetResult(0);
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);

                // We're not a normal Task.Run task so we won't go in the unobserved exception handler, but we want to act like an unhandled so we
                // will end inside the AppDomain.CurrentDomain.UnhandledException handler as expected
                throw;
            }
        })
        {
            IsBackground = true,
            Name = name,
        };

#pragma warning disable CA1416 // Validate platform compatibility
        thread.Start();
#pragma warning restore CA1416

        return taskCompletionSource.Task;
    }

    public Task WhenAll(params Task[] tasks)
        => Task.WhenAll(tasks);

    public Task Delay(int millisecondDelay)
        => Task.Delay(millisecondDelay);

    public Task Delay(TimeSpan timeSpan, CancellationToken cancellation)
        => Task.Delay(timeSpan, cancellation);
}
