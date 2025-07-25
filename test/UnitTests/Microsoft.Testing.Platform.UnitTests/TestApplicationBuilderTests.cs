﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Helpers;
using Microsoft.Testing.Platform.Services;
using Microsoft.Testing.Platform.TestHost;
using Microsoft.Testing.Platform.TestHostControllers;

namespace Microsoft.Testing.Platform.UnitTests;

[TestClass]
public sealed class TestApplicationBuilderTests
{
    private readonly ServiceProvider _serviceProvider = new();

    public TestApplicationBuilderTests()
    {
        CurrentTestApplicationModuleInfo testApplicationModuleInfo = new(new SystemEnvironment(), new SystemProcessHandler());
        AggregatedConfiguration configuration = new([], testApplicationModuleInfo, new SystemFileSystem(), new(null, [], []));
        configuration.SetCurrentWorkingDirectory(string.Empty);
        configuration.SetCurrentWorkingDirectory(string.Empty);
        _serviceProvider.AddService(configuration);
    }

    [TestMethod]
    public async Task TestApplicationLifecycleCallbacks_DuplicatedId_ShouldFail()
    {
        TestHostManager testHostManager = new();
        testHostManager.AddTestHostApplicationLifetime(_ => new ApplicationLifecycleCallbacks("duplicatedId"));
        testHostManager.AddTestHostApplicationLifetime(_ => new ApplicationLifecycleCallbacks("duplicatedId"));
        InvalidOperationException invalidOperationException = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => testHostManager.BuildTestApplicationLifecycleCallbackAsync(_serviceProvider));
        Assert.IsTrue(invalidOperationException.Message.Contains("duplicatedId") && invalidOperationException.Message.Contains(typeof(ApplicationLifecycleCallbacks).ToString()));
    }

    [TestMethod]
    public async Task DataConsumer_DuplicatedId_ShouldFail()
    {
        TestHostManager testHostManager = new();
        testHostManager.AddDataConsumer(_ => new Consumer("duplicatedId"));
        testHostManager.AddDataConsumer(_ => new Consumer("duplicatedId"));
        InvalidOperationException invalidOperationException = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => testHostManager.BuildDataConsumersAsync(_serviceProvider, []));
        Assert.IsTrue(invalidOperationException.Message.Contains("duplicatedId") && invalidOperationException.Message.Contains(typeof(Consumer).ToString()));
    }

    [TestMethod]
    public async Task DataConsumer_DuplicatedIdWithCompositeFactory_ShouldFail()
    {
        TestHostManager testHostManager = new();
        CompositeExtensionFactory<Consumer> compositeExtensionFactory = new(() => new Consumer("duplicatedId"));
        testHostManager.AddDataConsumer(_ => new Consumer("duplicatedId"));
        testHostManager.AddDataConsumer(compositeExtensionFactory);
        InvalidOperationException invalidOperationException = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => testHostManager.BuildDataConsumersAsync(_serviceProvider, []));
        Assert.IsTrue(invalidOperationException.Message.Contains("duplicatedId") && invalidOperationException.Message.Contains(typeof(Consumer).ToString()));
    }

    [TestMethod]
    public async Task TestSessionLifetimeHandle_DuplicatedId_ShouldFail()
    {
        TestHostManager testHostManager = new();
        testHostManager.AddTestSessionLifetimeHandle(_ => new TestSessionLifetimeHandler("duplicatedId"));
        testHostManager.AddTestSessionLifetimeHandle(_ => new TestSessionLifetimeHandler("duplicatedId"));
        InvalidOperationException invalidOperationException = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => testHostManager.BuildTestSessionLifetimeHandleAsync(_serviceProvider, []));
        Assert.IsTrue(invalidOperationException.Message.Contains("duplicatedId") && invalidOperationException.Message.Contains(typeof(TestSessionLifetimeHandler).ToString()));
    }

    [TestMethod]
    public async Task TestSessionLifetimeHandle_DuplicatedIdWithCompositeFactory_ShouldFail()
    {
        TestHostManager testHostManager = new();
        CompositeExtensionFactory<TestSessionLifetimeHandler> compositeExtensionFactory = new(() => new TestSessionLifetimeHandler("duplicatedId"));
        testHostManager.AddTestSessionLifetimeHandle(_ => new TestSessionLifetimeHandler("duplicatedId"));
        testHostManager.AddTestSessionLifetimeHandle(compositeExtensionFactory);
        InvalidOperationException invalidOperationException = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => testHostManager.BuildTestSessionLifetimeHandleAsync(_serviceProvider, []));
        Assert.IsTrue(invalidOperationException.Message.Contains("duplicatedId") && invalidOperationException.Message.Contains(typeof(TestSessionLifetimeHandler).ToString()));
    }

    [DataRow(true)]
    [DataRow(false)]
    [TestMethod]
    public async Task TestHost_ComposeFactory_ShouldSucceed(bool withParameter)
    {
        TestHostManager testHostManager = new();
        CompositeExtensionFactory<TestSessionLifetimeHandlerPlusConsumer> compositeExtensionFactory =
            withParameter
            ? new(sp => new TestSessionLifetimeHandlerPlusConsumer(sp))
            : new(() => new TestSessionLifetimeHandlerPlusConsumer());
        testHostManager.AddTestSessionLifetimeHandle(compositeExtensionFactory);
        testHostManager.AddDataConsumer(compositeExtensionFactory);
        List<ICompositeExtensionFactory> compositeExtensions = [];
        IDataConsumer[] consumers = [.. (await testHostManager.BuildDataConsumersAsync(_serviceProvider, compositeExtensions)).Select(x => (IDataConsumer)x.Consumer)];
        ITestSessionLifetimeHandler[] sessionLifetimeHandle = [.. (await testHostManager.BuildTestSessionLifetimeHandleAsync(_serviceProvider, compositeExtensions)).Select(x => (ITestSessionLifetimeHandler)x.TestSessionLifetimeHandler)];
        Assert.AreEqual(1, consumers.Length);
        Assert.AreEqual(1, sessionLifetimeHandle.Length);
        Assert.AreEqual(compositeExtensions[0].GetInstance(), consumers[0]);
        Assert.AreEqual(compositeExtensions[0].GetInstance(), sessionLifetimeHandle[0]);
    }

    [TestMethod]
    public async Task TestHostControllerEnvironmentVariableProvider_DuplicatedId_ShouldFail()
    {
        TestHostControllersManager testHostControllerManager = new();
        testHostControllerManager.AddEnvironmentVariableProvider(_ => new TestHostEnvironmentVariableProvider("duplicatedId"));
        testHostControllerManager.AddEnvironmentVariableProvider(_ => new TestHostEnvironmentVariableProvider("duplicatedId"));
        InvalidOperationException invalidOperationException = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => testHostControllerManager.BuildAsync(_serviceProvider));
        Assert.IsTrue(invalidOperationException.Message.Contains("duplicatedId") && invalidOperationException.Message.Contains(typeof(TestHostEnvironmentVariableProvider).ToString()));
    }

    [TestMethod]
    public async Task TestHostControllerEnvironmentVariableProvider_DuplicatedIdWithCompositeFactory_ShouldFail()
    {
        TestHostControllersManager testHostControllerManager = new();
        CompositeExtensionFactory<TestHostEnvironmentVariableProvider> compositeExtensionFactory = new(() => new TestHostEnvironmentVariableProvider("duplicatedId"));
        testHostControllerManager.AddEnvironmentVariableProvider(_ => new TestHostEnvironmentVariableProvider("duplicatedId"));
        testHostControllerManager.AddEnvironmentVariableProvider(compositeExtensionFactory);
        InvalidOperationException invalidOperationException = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => testHostControllerManager.BuildAsync(_serviceProvider));
        Assert.IsTrue(invalidOperationException.Message.Contains("duplicatedId") && invalidOperationException.Message.Contains(typeof(TestHostEnvironmentVariableProvider).ToString()));
    }

    [TestMethod]
    public async Task TestHostControllerProcessLifetimeHandler_DuplicatedId_ShouldFail()
    {
        TestHostControllersManager testHostControllerManager = new();
        testHostControllerManager.AddProcessLifetimeHandler(_ => new TestHostProcessLifetimeHandler("duplicatedId"));
        testHostControllerManager.AddProcessLifetimeHandler(_ => new TestHostProcessLifetimeHandler("duplicatedId"));
        InvalidOperationException invalidOperationException = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => testHostControllerManager.BuildAsync(_serviceProvider));
        Assert.IsTrue(invalidOperationException.Message.Contains("duplicatedId") && invalidOperationException.Message.Contains(typeof(TestHostProcessLifetimeHandler).ToString()));
    }

    [TestMethod]
    public async Task TestHostControllerProcessLifetimeHandler_DuplicatedIdWithCompositeFactory_ShouldFail()
    {
        TestHostControllersManager testHostControllerManager = new();
        CompositeExtensionFactory<TestHostProcessLifetimeHandler> compositeExtensionFactory = new(() => new TestHostProcessLifetimeHandler("duplicatedId"));
        testHostControllerManager.AddProcessLifetimeHandler(_ => new TestHostProcessLifetimeHandler("duplicatedId"));
        testHostControllerManager.AddProcessLifetimeHandler(compositeExtensionFactory);
        InvalidOperationException invalidOperationException = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => testHostControllerManager.BuildAsync(_serviceProvider));
        Assert.IsTrue(invalidOperationException.Message.Contains("duplicatedId") && invalidOperationException.Message.Contains(typeof(TestHostProcessLifetimeHandler).ToString()));
    }

    [DataRow(true)]
    [DataRow(false)]
    [TestMethod]
    public async Task TestHostController_ComposeFactory_ShouldSucceed(bool withParameter)
    {
        TestHostControllersManager testHostControllerManager = new();
        CompositeExtensionFactory<TestHostProcessLifetimeHandlerPlusTestHostEnvironmentVariableProvider> compositeExtensionFactory =
            withParameter
            ? new(sp => new TestHostProcessLifetimeHandlerPlusTestHostEnvironmentVariableProvider(sp))
            : new(() => new TestHostProcessLifetimeHandlerPlusTestHostEnvironmentVariableProvider());
        testHostControllerManager.AddEnvironmentVariableProvider(compositeExtensionFactory);
        testHostControllerManager.AddProcessLifetimeHandler(compositeExtensionFactory);
        TestHostControllerConfiguration configuration = await testHostControllerManager.BuildAsync(_serviceProvider);
        Assert.IsTrue(configuration.RequireProcessRestart);
        Assert.AreEqual(1, configuration.LifetimeHandlers.Length);
        Assert.AreEqual(1, configuration.EnvironmentVariableProviders.Length);
        Assert.AreEqual((object)configuration.LifetimeHandlers[0], configuration.EnvironmentVariableProviders[0]);
        Assert.AreEqual(((ICompositeExtensionFactory)compositeExtensionFactory).GetInstance(), configuration.LifetimeHandlers[0]);
        Assert.AreEqual(((ICompositeExtensionFactory)compositeExtensionFactory).GetInstance(), configuration.EnvironmentVariableProviders[0]);
    }

    [DataRow(true)]
    [DataRow(false)]
    [TestMethod]
    public void ComposeFactory_InvalidComposition_ShouldFail(bool withParameter)
    {
        CompositeExtensionFactory<InvalidComposition> compositeExtensionFactory =
            withParameter
            ? new CompositeExtensionFactory<InvalidComposition>(sp => new InvalidComposition(sp))
            : new CompositeExtensionFactory<InvalidComposition>(() => new InvalidComposition());
        InvalidOperationException invalidOperationException = Assert.ThrowsExactly<InvalidOperationException>(() => ((ICompositeExtensionFactory)compositeExtensionFactory).GetInstance());
        Assert.AreEqual(CompositeExtensionFactory<InvalidComposition>.ValidateCompositionErrorMessage, invalidOperationException.Message);
    }

    [SuppressMessage("Design", "TA0001:Extension should not implement cross-functional areas", Justification = "Done on purpose for testing error")]
    private sealed class InvalidComposition : ITestHostProcessLifetimeHandler, ITestSessionLifetimeHandler
    {
        public InvalidComposition(IServiceProvider serviceProvider)
        {
        }

        public InvalidComposition()
        {
        }

        public string Uid => nameof(InvalidComposition);

        public string Version => AppVersion.DefaultSemVer;

        public string DisplayName => nameof(InvalidComposition);

        public string Description => nameof(InvalidComposition);

        public Task BeforeTestHostProcessStartAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool> IsEnabledAsync() => throw new NotImplementedException();

        public Task OnTestHostProcessExitedAsync(ITestHostProcessInformation testHostProcessInformation, CancellationToken cancellation) => throw new NotImplementedException();

        public Task OnTestHostProcessStartedAsync(ITestHostProcessInformation testHostProcessInformation, CancellationToken cancellation) => throw new NotImplementedException();

        public Task OnTestSessionStartingAsync(SessionUid sessionUid, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task OnTestSessionFinishingAsync(SessionUid sessionUid, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class TestHostProcessLifetimeHandlerPlusTestHostEnvironmentVariableProvider : ITestHostProcessLifetimeHandler, ITestHostEnvironmentVariableProvider
    {
        public TestHostProcessLifetimeHandlerPlusTestHostEnvironmentVariableProvider()
        {
        }

        public TestHostProcessLifetimeHandlerPlusTestHostEnvironmentVariableProvider(IServiceProvider serviceProvider)
        {
        }

        public string Uid => nameof(TestHostProcessLifetimeHandlerPlusTestHostEnvironmentVariableProvider);

        public string Version => AppVersion.DefaultSemVer;

        public string DisplayName => nameof(TestHostProcessLifetimeHandlerPlusTestHostEnvironmentVariableProvider);

        public string Description => nameof(TestHostProcessLifetimeHandlerPlusTestHostEnvironmentVariableProvider);

        public Task<ValidationResult> ValidateTestHostEnvironmentVariablesAsync(IReadOnlyEnvironmentVariables environmentVariables) => throw new NotImplementedException();

        public Task BeforeTestHostProcessStartAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        public Task OnTestHostProcessExitedAsync(ITestHostProcessInformation testHostProcessInformation, CancellationToken cancellation) => throw new NotImplementedException();

        public Task OnTestHostProcessStartedAsync(ITestHostProcessInformation testHostProcessInformation, CancellationToken cancellation) => throw new NotImplementedException();

        public Task UpdateAsync(IEnvironmentVariables environmentVariables) => throw new NotImplementedException();
    }

    private sealed class TestHostProcessLifetimeHandler : ITestHostProcessLifetimeHandler
    {
        public TestHostProcessLifetimeHandler(string id) => Uid = id;

        public string Uid { get; }

        public string Version => nameof(Consumer);

        public string DisplayName => nameof(Consumer);

        public string Description => nameof(Consumer);

        public Task BeforeTestHostProcessStartAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        public Task OnTestHostProcessExitedAsync(ITestHostProcessInformation testHostProcessInformation, CancellationToken cancellation) => throw new NotImplementedException();

        public Task OnTestHostProcessStartedAsync(ITestHostProcessInformation testHostProcessInformation, CancellationToken cancellation) => throw new NotImplementedException();
    }

    private sealed class TestHostEnvironmentVariableProvider : ITestHostEnvironmentVariableProvider
    {
        public TestHostEnvironmentVariableProvider(string id) => Uid = id;

        public string Uid { get; }

        public string Version => nameof(Consumer);

        public string DisplayName => nameof(Consumer);

        public string Description => nameof(Consumer);

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        public Task UpdateAsync(IEnvironmentVariables environmentVariables) => throw new NotImplementedException();

        public Task<ValidationResult> ValidateTestHostEnvironmentVariablesAsync(IReadOnlyEnvironmentVariables environmentVariables) => throw new NotImplementedException();
    }

    public sealed class TestSessionLifetimeHandlerPlusConsumer : ITestSessionLifetimeHandler, IDataConsumer
    {
        public TestSessionLifetimeHandlerPlusConsumer()
        {
        }

        public TestSessionLifetimeHandlerPlusConsumer(IServiceProvider serviceProvider)
        {
        }

        public string Uid => nameof(TestSessionLifetimeHandlerPlusConsumer);

        public string Version => AppVersion.DefaultSemVer;

        public string DisplayName => nameof(TestSessionLifetimeHandlerPlusConsumer);

        public string Description => nameof(TestSessionLifetimeHandlerPlusConsumer);

        public Type[] DataTypesConsumed => [];

        public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        public Task OnTestSessionFinishingAsync(SessionUid sessionUid, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task OnTestSessionStartingAsync(SessionUid sessionUid, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class TestSessionLifetimeHandler : ITestSessionLifetimeHandler
    {
        public TestSessionLifetimeHandler(string id) => Uid = id;

        public string Uid { get; }

        public string Version => nameof(Consumer);

        public string DisplayName => nameof(Consumer);

        public string Description => nameof(Consumer);

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        public Task OnTestSessionStartingAsync(SessionUid sessionUid, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task OnTestSessionFinishingAsync(SessionUid sessionUid, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class Consumer : IDataConsumer
    {
        public Consumer(string id) => Uid = id;

        public string Uid { get; }

        public string Version => nameof(Consumer);

        public string DisplayName => nameof(Consumer);

        public string Description => nameof(Consumer);

        public Type[] DataTypesConsumed => throw new NotImplementedException();

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class ApplicationLifecycleCallbacks : ITestHostApplicationLifetime
    {
        public ApplicationLifecycleCallbacks(string id) => Uid = id;

        public string Uid { get; }

        public string Version => nameof(ApplicationLifecycleCallbacks);

        public string DisplayName => nameof(ApplicationLifecycleCallbacks);

        public string Description => nameof(ApplicationLifecycleCallbacks);

        public Task AfterRunAsync(int exitCode, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task BeforeRunAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);
    }
}
