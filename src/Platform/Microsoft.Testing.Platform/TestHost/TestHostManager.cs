// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Helpers;
using Microsoft.Testing.Platform.Requests;
using Microsoft.Testing.Platform.Resources;
using Microsoft.Testing.Platform.Services;

namespace Microsoft.Testing.Platform.TestHost;

internal sealed class TestHostManager : ITestHostManager
{
    // Registration ordering
    private readonly List<object> _factoryOrdering = [];

    // Exposed extension points
    private readonly List<Func<IServiceProvider, ITestApplicationLifecycleCallbacks>> _testApplicationLifecycleCallbacksFactories = [];
    private readonly List<Func<IServiceProvider, IDataConsumer>> _dataConsumerFactories = [];
    private readonly List<Func<IServiceProvider, ITestSessionLifetimeHandler>> _testSessionLifetimeHandlerFactories = [];
    private readonly List<ICompositeExtensionFactory> _dataConsumersCompositeServiceFactories = [];
    private readonly List<ICompositeExtensionFactory> _testSessionLifetimeHandlerCompositeFactories = [];

    // Non-exposed extension points
    private Func<IServiceProvider, ITestExecutionFilterFactory>? _testExecutionFilterFactory;
    private Func<IServiceProvider, ITestFrameworkInvoker>? _testFrameworkInvokerFactory;

    public void AddTestFrameworkInvoker(Func<IServiceProvider, ITestFrameworkInvoker> testFrameworkInvokerFactory)
    {
        Guard.NotNull(testFrameworkInvokerFactory);
        if (_testFrameworkInvokerFactory is not null)
        {
            throw new InvalidOperationException(PlatformResources.TestAdapterInvokerFactoryAlreadySetErrorMessage);
        }

        _testFrameworkInvokerFactory = testFrameworkInvokerFactory;
    }

    internal async Task<ActionResult<ITestFrameworkInvoker>> TryBuildTestAdapterInvokerAsync(ServiceProvider serviceProvider)
    {
        if (_testFrameworkInvokerFactory is null)
        {
            return ActionResult.Fail<ITestFrameworkInvoker>();
        }

        ITestFrameworkInvoker testAdapterInvoke = _testFrameworkInvokerFactory(serviceProvider);

        // We initialize only if enabled
        if (await InitializeIfEnabled(testAdapterInvoke))
        {
            return ActionResult.Ok(testAdapterInvoke);
        }

        return ActionResult.Fail<ITestFrameworkInvoker>();
    }

    public void AddTestExecutionFilterFactory(Func<IServiceProvider, ITestExecutionFilterFactory> testExecutionFilterFactory)
    {
        Guard.NotNull(testExecutionFilterFactory);
        if (_testExecutionFilterFactory is not null)
        {
            throw new InvalidOperationException(PlatformResources.TEstExecutionFilterFactoryFactoryAlreadySetErrorMessage);
        }

        _testExecutionFilterFactory = testExecutionFilterFactory;
    }

    internal async Task<ActionResult<ITestExecutionFilterFactory>> TryBuildTestExecutionFilterFactoryAsync(ServiceProvider serviceProvider)
    {
        if (_testExecutionFilterFactory is null)
        {
            return ActionResult.Fail<ITestExecutionFilterFactory>();
        }

        ITestExecutionFilterFactory testExecutionFilterFactory = _testExecutionFilterFactory(serviceProvider);

        if (await InitializeIfEnabled(testExecutionFilterFactory))
        {
            return ActionResult.Ok(testExecutionFilterFactory);
        }

        return ActionResult.Fail<ITestExecutionFilterFactory>();
    }

    public void AddTestApplicationLifecycleCallbacks(Func<IServiceProvider, ITestApplicationLifecycleCallbacks> testApplicationLifecycleCallbacks)
    {
        Guard.NotNull(testApplicationLifecycleCallbacks);
        _testApplicationLifecycleCallbacksFactories.Add(testApplicationLifecycleCallbacks);
    }

    internal async Task<List<ITestApplicationLifecycleCallbacks>> BuildTestApplicationLifecycleCallbackAsync(ServiceProvider serviceProvider)
    {
        List<ITestApplicationLifecycleCallbacks> callbacks = [];
        foreach (Func<IServiceProvider, ITestApplicationLifecycleCallbacks> testApplicationLifecycleCallbacksFactory in _testApplicationLifecycleCallbacksFactories)
        {
            ITestApplicationLifecycleCallbacks service = testApplicationLifecycleCallbacksFactory(serviceProvider);

            ThrowIfRegistered(callbacks, service);

            if (await InitializeIfEnabled(service))
            {
                // Register the extension for usage
                callbacks.Add(service);
            }
        }

        return callbacks;
    }

    public void AddDataConsumer(Func<IServiceProvider, IDataConsumer> dataConsumerFactory)
    {
        Guard.NotNull(dataConsumerFactory);
        _dataConsumerFactories.Add(dataConsumerFactory);
        _factoryOrdering.Add(dataConsumerFactory);
    }

    public void AddDataConsumer<T>(CompositeExtensionFactory<T> compositeServiceFactory)
        where T : class, IDataConsumer =>
        AddCompositeServiceFactory(compositeServiceFactory, _dataConsumersCompositeServiceFactories);

    private void AddCompositeServiceFactory<T>(CompositeExtensionFactory<T> factory, List<ICompositeExtensionFactory> factories)
        where T : class, IExtension
    {
        Guard.NotNull(factory);
        if (factories.Contains(factory))
        {
            throw new ArgumentException(PlatformResources.CompositeServiceFactoryInstanceAlreadyRegistered);
        }

        factories.Add(factory);
        _factoryOrdering.Add(factory);
    }

    internal async Task<List<(IExtension Consumer, int RegistrationOrder)>> BuildDataConsumersAsync(ServiceProvider serviceProvider, List<ICompositeExtensionFactory> alreadyBuiltServices)
    {
        List<(IExtension Consumer, int RegistrationOrder)> dataConsumers = [];
        foreach (Func<IServiceProvider, IDataConsumer> dataConsumerFactory in _dataConsumerFactories)
        {
            IDataConsumer service = dataConsumerFactory(serviceProvider);

            ThrowIfRegistered(dataConsumers.Select(_ => _.Consumer), service);

            if (await InitializeIfEnabled(service))
            {
                // Register the extension for usage
                dataConsumers.Add((service, _factoryOrdering.IndexOf(dataConsumerFactory)));
            }
        }

        foreach (ICompositeExtensionFactory compositeServiceFactory in _dataConsumersCompositeServiceFactories)
        {
            ICompositeExtensionFactory? compositeFactoryInstance;
            var factoryType = compositeServiceFactory.GetType();
            // We check if the same service is already built in some other build phase
            if ((compositeFactoryInstance = alreadyBuiltServices.SingleOrDefault(x => x.GetType() == factoryType)) is null)
            {
                // We clone the instance because we want to have fresh instance per BuildTestApplicationLifecycleCallbackAsync call
                compositeFactoryInstance = (ICompositeExtensionFactory)compositeServiceFactory.Clone();

                // Create the new fresh instance
                var instance = (IExtension)compositeFactoryInstance.GetInstance(serviceProvider);

                ThrowIfRegistered(dataConsumers.Select(_ => _.Consumer), instance);

                await InitializeIfEnabled(instance);

                // Add to the list of shared singletons
                alreadyBuiltServices.Add(compositeFactoryInstance);
            }

            // Get the singleton
            var extension = (IExtension)compositeFactoryInstance.GetInstance();

            // We register the extension only if enabled
            if (await extension.IsEnabledAsync())
            {
                if (extension is IDataConsumer consumer)
                {
                    // Register the extension for usage
                    dataConsumers.Add((consumer, _factoryOrdering.IndexOf(compositeServiceFactory)));
                    continue;
                }

                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, PlatformResources.ExtensionDoesNotImplementGivenInterfaceErrorMessage, extension.GetType(), typeof(IDataConsumer)));
            }
        }

        return dataConsumers;
    }

    public void AddTestSessionLifetimeHandle(Func<IServiceProvider, ITestSessionLifetimeHandler> testSessionLifetimeHandleFactory)
    {
        Guard.NotNull(testSessionLifetimeHandleFactory);
        _testSessionLifetimeHandlerFactories.Add(testSessionLifetimeHandleFactory);
        _factoryOrdering.Add(testSessionLifetimeHandleFactory);
    }

    public void AddTestSessionLifetimeHandle<T>(CompositeExtensionFactory<T> compositeServiceFactory)
        where T : class, ITestSessionLifetimeHandler =>
        AddCompositeServiceFactory(compositeServiceFactory, _testSessionLifetimeHandlerCompositeFactories);

    internal async Task<List<(IExtension TestSessionLifetimeHandler, int RegistrationOrder)>> BuildTestSessionLifetimeHandleAsync(ServiceProvider serviceProvider, List<ICompositeExtensionFactory> alreadyBuiltServices)
    {
        List<(IExtension TestSessionLifetimeHandler, int RegistrationOrder)> testSessionLifetimeHandlers = [];
        foreach (Func<IServiceProvider, ITestSessionLifetimeHandler> testSessionLifetimeHandlerFactory in _testSessionLifetimeHandlerFactories)
        {
            ITestSessionLifetimeHandler service = testSessionLifetimeHandlerFactory(serviceProvider);

            ThrowIfRegistered(testSessionLifetimeHandlers.Select(_ => _.TestSessionLifetimeHandler), service);

            if (await InitializeIfEnabled(service))
            {
                // Register the extension for usage
                testSessionLifetimeHandlers.Add((service, _factoryOrdering.IndexOf(testSessionLifetimeHandlerFactory)));
            }
        }

        foreach (ICompositeExtensionFactory compositeServiceFactory in _testSessionLifetimeHandlerCompositeFactories)
        {
            ICompositeExtensionFactory? compositeFactoryInstance;

            // We check if the same service is already built in some other build phase
            if ((compositeFactoryInstance = alreadyBuiltServices.SingleOrDefault(x => x.GetType() == compositeServiceFactory.GetType())) is null)
            {
                // We clone the instance because we want to have fresh instance per BuildTestApplicationLifecycleCallbackAsync call
                compositeFactoryInstance = (ICompositeExtensionFactory)compositeServiceFactory.Clone();

                // Create the new fresh instance
                var instance = (IExtension)compositeFactoryInstance.GetInstance(serviceProvider);

                ThrowIfRegistered(testSessionLifetimeHandlers.Select(_ => _.TestSessionLifetimeHandler), instance);

                await InitializeIfEnabled(instance);

                // Add to the list of shared singletons
                alreadyBuiltServices.Add(compositeFactoryInstance);
            }

            // Get the singleton
            var extension = (IExtension)compositeFactoryInstance.GetInstance();

            // We register the extension only if enabled
            if (await extension.IsEnabledAsync())
            {
                if (extension is ITestSessionLifetimeHandler testSessionLifetimeHandler)
                {
                    // Register the extension for usage
                    testSessionLifetimeHandlers.Add((testSessionLifetimeHandler, _factoryOrdering.IndexOf(compositeServiceFactory)));
                }
                else
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, PlatformResources.ExtensionDoesNotImplementGivenInterfaceErrorMessage, extension.GetType(), typeof(ITestSessionLifetimeHandler)));
                }
            }
        }

        return testSessionLifetimeHandlers;
    }

    private static void ThrowIfRegistered(IEnumerable<IExtension> extensions, IExtension extension)
    {
        // Check if we have already extensions of the same type with same id registered
        var existing = extensions.SingleOrDefault(_ => _.Uid == extension.Uid);
        if (existing != null)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, PlatformResources.ExtensionWithSameUidAlreadyRegisteredErrorMessage, extension.Uid, existing.GetType()));
        }
    }

    private static async Task<bool> InitializeIfEnabled(IExtension instance)
    {
        // We initialize only if enabled
        if (await instance.IsEnabledAsync())
        {
            await instance.TryInitializeAsync();
            return true;
        }

        return false;
    }
}
