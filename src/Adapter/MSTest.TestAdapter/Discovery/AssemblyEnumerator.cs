﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using System.Text;

using Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.Execution;
using Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.Helpers;
using Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Internal;

namespace Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter.Discovery;

/// <summary>
/// Enumerates through all types in the assembly in search of valid test methods.
/// </summary>
[SuppressMessage("Performance", "CA1852: Seal internal types", Justification = "Overrides required for testability")]
internal class AssemblyEnumerator : MarshalByRefObject
{
    /// <summary>
    /// Helper for reflection API's.
    /// </summary>
    private static readonly ReflectHelper ReflectHelper = ReflectHelper.Instance;

    /// <summary>
    /// Type cache.
    /// </summary>
    private readonly TypeCache _typeCache = new(ReflectHelper);

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyEnumerator"/> class.
    /// </summary>
    public AssemblyEnumerator()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssemblyEnumerator"/> class.
    /// </summary>
    /// <param name="settings">The settings for the session.</param>
    /// <remarks>Use this constructor when creating this object in a new app domain so the settings for this app domain are set.</remarks>
    public AssemblyEnumerator(MSTestSettings settings) =>
        // Populate the settings into the domain(Desktop workflow) performing discovery.
        // This would just be resetting the settings to itself in non desktop workflows.
        MSTestSettings.PopulateSettings(settings);

    /// <summary>
    /// Returns object to be used for controlling lifetime, null means infinite lifetime.
    /// </summary>
    /// <returns>
    /// The <see cref="object"/>.
    /// </returns>
    [SecurityCritical]
#if NET5_0_OR_GREATER
    [Obsolete]
#endif
    public override object InitializeLifetimeService() => null!;

    /// <summary>
    /// Enumerates through all types in the assembly in search of valid test methods.
    /// </summary>
    /// <param name="assemblyFileName">The assembly file name.</param>
    /// <param name="runSettingsXml">The xml specifying runsettings.</param>
    /// <param name="warnings">Contains warnings if any, that need to be passed back to the caller.</param>
    /// <returns>A collection of Test Elements.</returns>
    internal ICollection<UnitTestElement> EnumerateAssembly(
        string assemblyFileName,
        [StringSyntax(StringSyntaxAttribute.Xml, nameof(runSettingsXml))] string? runSettingsXml,
        out ICollection<string> warnings)
    {
        DebugEx.Assert(!StringEx.IsNullOrWhiteSpace(assemblyFileName), "Invalid assembly file name.");
        var warningMessages = new List<string>();
        var tests = new List<UnitTestElement>();
        // Contains list of assembly/class names for which we have already added fixture tests.
        var fixturesTests = new HashSet<string>();

        Assembly assembly = PlatformServiceProvider.Instance.FileOperations.LoadAssembly(assemblyFileName, isReflectionOnly: false);

        Type[] types = GetTypes(assembly, assemblyFileName, warningMessages);
        bool discoverInternals = ReflectHelper.GetDiscoverInternalsAttribute(assembly) != null;
        TestIdGenerationStrategy testIdGenerationStrategy = ReflectHelper.GetTestIdGenerationStrategy(assembly);

        // Set the test ID generation strategy for DataRowAttribute and DynamicDataAttribute so we can improve display name without
        // causing a breaking change.
        DataRowAttribute.TestIdGenerationStrategy = testIdGenerationStrategy;
        DynamicDataAttribute.TestIdGenerationStrategy = testIdGenerationStrategy;

        TestDataSourceDiscoveryOption testDataSourceDiscovery = ReflectHelper.GetTestDataSourceDiscoveryOption(assembly)
#pragma warning disable CS0618 // Type or member is obsolete

            // When using legacy strategy, there is no point in trying to "read" data during discovery
            // as the ID generator will ignore it.
            ?? (testIdGenerationStrategy == TestIdGenerationStrategy.Legacy
                ? TestDataSourceDiscoveryOption.DuringExecution
                : TestDataSourceDiscoveryOption.DuringDiscovery);
#pragma warning restore CS0618 // Type or member is obsolete

        Dictionary<string, object>? testRunParametersFromRunSettings = RunSettingsUtilities.GetTestRunParameters(runSettingsXml);
        foreach (Type type in types)
        {
            if (type == null)
            {
                continue;
            }

            List<UnitTestElement> testsInType = DiscoverTestsInType(assemblyFileName, testRunParametersFromRunSettings, type, warningMessages, discoverInternals,
                testDataSourceDiscovery, testIdGenerationStrategy, fixturesTests);
            tests.AddRange(testsInType);
        }

        warnings = warningMessages;
        return tests;
    }

    /// <summary>
    /// Gets the types defined in an assembly.
    /// </summary>
    /// <param name="assembly">The reflected assembly.</param>
    /// <param name="assemblyFileName">The file name of the assembly.</param>
    /// <param name="warningMessages">Contains warnings if any, that need to be passed back to the caller.</param>
    /// <returns>Gets the types defined in the provided assembly.</returns>
    internal static Type[] GetTypes(Assembly assembly, string assemblyFileName, ICollection<string>? warningMessages)
    {
        try
        {
            return PlatformServiceProvider.Instance.ReflectionOperations.GetDefinedTypes(assembly);
        }
        catch (ReflectionTypeLoadException ex)
        {
            PlatformServiceProvider.Instance.AdapterTraceLogger.LogWarning($"MSTestExecutor.TryGetTests: {Resource.TestAssembly_AssemblyDiscoveryFailure}", assemblyFileName, ex);
            PlatformServiceProvider.Instance.AdapterTraceLogger.LogWarning(Resource.ExceptionsThrown);

            if (ex.LoaderExceptions != null)
            {
                // If not able to load all type, log a warning and continue with loaded types.
                string message = string.Format(CultureInfo.CurrentCulture, Resource.TypeLoadFailed, assemblyFileName, GetLoadExceptionDetails(ex));

                warningMessages?.Add(message);

                foreach (Exception? loaderEx in ex.LoaderExceptions)
                {
                    PlatformServiceProvider.Instance.AdapterTraceLogger.LogWarning("{0}", loaderEx);
                }
            }

            return ex.Types!;
        }
    }

    /// <summary>
    /// Formats load exception as multi-line string, each line contains load error message.
    /// </summary>
    /// <param name="ex">The exception.</param>
    /// <returns>Returns loader exceptions as a multi-line string.</returns>
    internal static string GetLoadExceptionDetails(ReflectionTypeLoadException ex)
    {
        DebugEx.Assert(ex != null, "exception should not be null.");

        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase); // Exception -> null.
        var errorDetails = new StringBuilder();

        if (ex.LoaderExceptions?.Length > 0)
        {
            // Loader exceptions can contain duplicates, leave only unique exceptions.
            foreach (Exception? loaderException in ex.LoaderExceptions)
            {
                DebugEx.Assert(loaderException != null, "loader exception should not be null.");
                string line = string.Format(CultureInfo.CurrentCulture, Resource.EnumeratorLoadTypeErrorFormat, loaderException.GetType(), loaderException.Message);
                if (!map.ContainsKey(line))
                {
                    map.Add(line, null);
                    errorDetails.AppendLine(line);
                }
            }
        }
        else
        {
            errorDetails.AppendLine(ex.Message);
        }

        return errorDetails.ToString();
    }

    /// <summary>
    /// Returns an instance of the <see cref="TypeEnumerator"/> class.
    /// </summary>
    /// <param name="type">The type to enumerate.</param>
    /// <param name="assemblyFileName">The reflected assembly name.</param>
    /// <param name="discoverInternals">True to discover test classes which are declared internal in
    /// addition to test classes which are declared public.</param>
    /// <param name="discoveryOption"><see cref="TestDataSourceDiscoveryOption"/> to use when generating tests.</param>
    /// <param name="testIdGenerationStrategy"><see cref="TestIdGenerationStrategy"/> to use when generating TestId.</param>
    /// <returns>a TypeEnumerator instance.</returns>
    internal virtual TypeEnumerator GetTypeEnumerator(Type type, string assemblyFileName, bool discoverInternals, TestDataSourceDiscoveryOption discoveryOption, TestIdGenerationStrategy testIdGenerationStrategy)
    {
        var typeValidator = new TypeValidator(ReflectHelper, discoverInternals);
        var testMethodValidator = new TestMethodValidator(ReflectHelper, discoverInternals);

        return new TypeEnumerator(type, assemblyFileName, ReflectHelper, typeValidator, testMethodValidator, discoveryOption, testIdGenerationStrategy);
    }

    private List<UnitTestElement> DiscoverTestsInType(
        string assemblyFileName,
        Dictionary<string, object>? testRunParametersFromRunSettings,
        Type type,
        List<string> warningMessages,
        bool discoverInternals,
        TestDataSourceDiscoveryOption discoveryOption,
        TestIdGenerationStrategy testIdGenerationStrategy,
        HashSet<string> fixturesTests)
    {
        IDictionary<string, object> tempSourceLevelParameters = PlatformServiceProvider.Instance.SettingsProvider.GetProperties(assemblyFileName);
        tempSourceLevelParameters = testRunParametersFromRunSettings?.ConcatWithOverwrites(tempSourceLevelParameters)
            ?? tempSourceLevelParameters
            ?? new Dictionary<string, object>();
        var sourceLevelParameters = tempSourceLevelParameters.ToDictionary(x => x.Key, x => (object?)x.Value);

        string? typeFullName = null;
        var tests = new List<UnitTestElement>();

        try
        {
            typeFullName = type.FullName;
            TypeEnumerator testTypeEnumerator = GetTypeEnumerator(type, assemblyFileName, discoverInternals, discoveryOption, testIdGenerationStrategy);
            ICollection<UnitTestElement>? unitTestCases = testTypeEnumerator.Enumerate(out ICollection<string> warningsFromTypeEnumerator);
            warningMessages.AddRange(warningsFromTypeEnumerator);

            if (unitTestCases != null)
            {
                foreach (UnitTestElement test in unitTestCases)
                {
                    if (discoveryOption == TestDataSourceDiscoveryOption.DuringDiscovery)
                    {
                        Lazy<TestMethodInfo?> testMethodInfo = GetTestMethodInfo(sourceLevelParameters, test);

                        // Add fixture tests like AssemblyInitialize, AssemblyCleanup, ClassInitialize, ClassCleanup.
                        if (MSTestSettings.CurrentSettings.ConsiderFixturesAsSpecialTests && testMethodInfo.Value is not null)
                        {
                            AddFixtureTests(testMethodInfo.Value, tests, fixturesTests);
                        }

                        if (DynamicDataAttached(test, testMethodInfo, tests))
                        {
                            continue;
                        }
                    }

                    tests.Add(test);
                }
            }
        }
        catch (Exception exception)
        {
            // If we fail to discover type from a class, then don't abort the discovery
            // Move to the next type.
            string message = string.Format(CultureInfo.CurrentCulture, Resource.CouldNotInspectTypeDuringDiscovery, typeFullName, assemblyFileName, exception.Message);
            PlatformServiceProvider.Instance.AdapterTraceLogger.LogInfo($"AssemblyEnumerator: {message}");
            warningMessages.Add(message);
        }

        return tests;
    }

    private Lazy<TestMethodInfo?> GetTestMethodInfo(IDictionary<string, object?> sourceLevelParameters, UnitTestElement test) =>
        new(() =>
        {
            // NOTE: From this place we don't have any path that would let the user write a message on the TestContext and we don't do
            // anything with what would be printed anyway so we can simply use a simple StringWriter.
            using var writer = new StringWriter();
            TestMethod testMethod = test.TestMethod;
            MSTestAdapter.PlatformServices.Interface.ITestContext testContext = PlatformServiceProvider.Instance.GetTestContext(testMethod, writer, sourceLevelParameters);
            return _typeCache.GetTestMethodInfo(testMethod, testContext, MSTestSettings.CurrentSettings.CaptureDebugTraces);
        });

    private static bool DynamicDataAttached(UnitTestElement test, Lazy<TestMethodInfo?> testMethodInfo, List<UnitTestElement> tests)
    {
        // It should always be `true`, but if any part of the chain is obsolete; it might not contain those.
        // Since we depend on those properties, if they don't exist, we bail out early.
        if (!test.TestMethod.HasManagedMethodAndTypeProperties)
        {
            return false;
        }

        DynamicDataType originalDataType = test.TestMethod.DataType;

        // PERF: For perf we started setting DataType in TypeEnumerator, so when it is None we will not reach this line.
        // But if we do run this code, we still reset it to None, because the code that determines if this is data drive test expects the value to be None
        // and only sets it when needed.
        //
        // If you remove this line and acceptance tests still pass you are okay.
        test.TestMethod.DataType = DynamicDataType.None;

        // The data source tests that we can process currently are those using attributes that
        // implement ITestDataSource (i.e, DataRow and DynamicData attributes).
        // However, for DataSourceAttribute, we currently don't have anyway to process it during discovery.
        // (Note: this method is only called under discoveryOption == TestDataSourceDiscoveryOption.DuringDiscovery)
        // So we want to return false from this method for non ITestDataSource (whether it's None or DataSourceAttribute). Otherwise, the test
        // will be completely skipped which is wrong behavior.
        return originalDataType == DynamicDataType.ITestDataSource &&
            testMethodInfo.Value != null &&
            TryProcessITestDataSourceTests(test, testMethodInfo.Value, tests);
    }

    private static void AddFixtureTests(TestMethodInfo testMethodInfo, List<UnitTestElement> tests, HashSet<string> fixtureTests)
    {
        string assemblyName = testMethodInfo.Parent.Parent.Assembly.GetName().Name!;
        string assemblyLocation = testMethodInfo.Parent.Parent.Assembly.Location;
        string classFullName = testMethodInfo.Parent.ClassType.FullName!;

        // Check if fixtures for this assembly has already been added.
        if (!fixtureTests.Contains(assemblyLocation))
        {
            _ = fixtureTests.Add(assemblyLocation);

            // Add AssemblyInitialize and AssemblyCleanup fixture tests if they exist.
            if (testMethodInfo.Parent.Parent.AssemblyInitializeMethod is not null)
            {
                tests.Add(GetAssemblyFixtureTest(testMethodInfo.Parent.Parent.AssemblyInitializeMethod, assemblyName,
                    classFullName, assemblyLocation, Constants.AssemblyInitializeFixtureTrait));
            }

            if (testMethodInfo.Parent.Parent.AssemblyCleanupMethod is not null)
            {
                tests.Add(GetAssemblyFixtureTest(testMethodInfo.Parent.Parent.AssemblyCleanupMethod, assemblyName,
                    classFullName, assemblyLocation, Constants.AssemblyCleanupFixtureTrait));
            }
        }

        // Check if fixtures for this class has already been added.
        if (!fixtureTests.Contains(assemblyLocation + classFullName))
        {
            _ = fixtureTests.Add(assemblyLocation + classFullName);

            // Add ClassInitialize and ClassCleanup fixture tests if they exist.
            if (testMethodInfo.Parent.ClassInitializeMethod is not null)
            {
                tests.Add(GetClassFixtureTest(testMethodInfo.Parent.ClassInitializeMethod, classFullName,
                    assemblyLocation, Constants.ClassInitializeFixtureTrait));
            }

            if (testMethodInfo.Parent.ClassCleanupMethod is not null)
            {
                tests.Add(GetClassFixtureTest(testMethodInfo.Parent.ClassCleanupMethod, classFullName,
                    assemblyLocation, Constants.ClassCleanupFixtureTrait));
            }
        }

        static UnitTestElement GetAssemblyFixtureTest(MethodInfo methodInfo, string assemblyName, string classFullName,
            string assemblyLocation, string fixtureType)
        {
            string methodName = GetMethodName(methodInfo);
            string[] hierarchy = [null!, assemblyName, Constants.AssemblyFixturesHierarchyClassName, methodName];
            return GetFixtureTest(classFullName, assemblyLocation, fixtureType, methodName, hierarchy);
        }

        static UnitTestElement GetClassFixtureTest(MethodInfo methodInfo, string classFullName,
            string assemblyLocation, string fixtureType)
        {
            string methodName = GetMethodName(methodInfo);
            string[] hierarchy = [null!, classFullName, methodName];
            return GetFixtureTest(classFullName, assemblyLocation, fixtureType, methodName, hierarchy);
        }

        static string GetMethodName(MethodInfo methodInfo)
        {
            ParameterInfo[] args = methodInfo.GetParameters();
            return args.Length > 0
                ? $"{methodInfo.Name}({string.Join(",", args.Select(a => a.ParameterType.FullName))})"
                : methodInfo.Name;
        }

        static UnitTestElement GetFixtureTest(string classFullName, string assemblyLocation, string fixtureType, string methodName, string[] hierarchy)
        {
            var method = new TestMethod(classFullName, methodName, hierarchy, methodName, classFullName, assemblyLocation, false, null, TestIdGenerationStrategy.FullyQualified);
            return new UnitTestElement(method)
            {
                DisplayName = $"[{fixtureType}] {methodName}",
                Ignored = true,
                Traits = [new Trait(Constants.FixturesTestTrait, fixtureType)],
            };
        }
    }

    private static bool TryProcessITestDataSourceTests(UnitTestElement test, TestMethodInfo testMethodInfo, List<UnitTestElement> tests)
    {
        // We don't have a special method to filter attributes that are not derived from Attribute, so we take all
        // attributes and filter them. We don't have to care if there is one, because this method is only entered when
        // there is at least one (we determine this in TypeEnumerator.GetTestFromMethod.
        IEnumerable<ITestDataSource> testDataSources = ReflectHelper.Instance.GetDerivedAttributes<Attribute>(testMethodInfo.MethodInfo, inherit: false).OfType<ITestDataSource>();

        try
        {
            return ProcessITestDataSourceTests(test, new(testMethodInfo.MethodInfo, test.DisplayName), testDataSources, tests);
        }
        catch (Exception ex)
        {
            string message = string.Format(CultureInfo.CurrentCulture, Resource.CannotEnumerateIDataSourceAttribute, test.TestMethod.ManagedTypeName, test.TestMethod.ManagedMethodName, ex);
            PlatformServiceProvider.Instance.AdapterTraceLogger.LogInfo($"DynamicDataEnumerator: {message}");
            return false;
        }
    }

    private static bool ProcessITestDataSourceTests(UnitTestElement test, ReflectionTestMethodInfo methodInfo, IEnumerable<ITestDataSource> testDataSources,
        List<UnitTestElement> tests)
    {
        foreach (ITestDataSource dataSource in testDataSources)
        {
            IEnumerable<object?[]>? data;

            // This code is to discover tests. To run the tests code is in TestMethodRunner.ExecuteDataSourceBasedTests.
            // Any change made here should be reflected in TestMethodRunner.ExecuteDataSourceBasedTests as well.
            data = dataSource.GetData(methodInfo);

            if (!data.Any())
            {
                if (!MSTestSettings.CurrentSettings.ConsiderEmptyDataSourceAsInconclusive)
                {
                    throw dataSource.GetExceptionForEmptyDataSource(methodInfo);
                }

                UnitTestElement discoveredTest = test.Clone();
                // Make the test not data driven, because it had no data.
                discoveredTest.TestMethod.DataType = DynamicDataType.None;
                discoveredTest.DisplayName = dataSource.GetDisplayName(methodInfo, null) ?? discoveredTest.DisplayName;

                tests.Add(discoveredTest);

                continue;
            }

            var testDisplayNameFirstSeen = new Dictionary<string, int>();
            var discoveredTests = new List<UnitTestElement>();
            int index = 0;

            foreach (object?[] d in data)
            {
                UnitTestElement discoveredTest = test.Clone();
                discoveredTest.DisplayName = dataSource.GetDisplayName(methodInfo, d) ?? discoveredTest.DisplayName;

                // If strategy is DisplayName and we have a duplicate test name don't expand the test, bail out.
#pragma warning disable CS0618 // Type or member is obsolete
                if (test.TestMethod.TestIdGenerationStrategy == TestIdGenerationStrategy.DisplayName
                    && testDisplayNameFirstSeen.TryGetValue(discoveredTest.DisplayName!, out int firstIndexSeen))
                {
                    string warning = string.Format(CultureInfo.CurrentCulture, Resource.CannotExpandIDataSourceAttribute_DuplicateDisplayName, firstIndexSeen, index, discoveredTest.DisplayName);
                    warning = string.Format(CultureInfo.CurrentCulture, Resource.CannotExpandIDataSourceAttribute, test.TestMethod.ManagedTypeName, test.TestMethod.ManagedMethodName, warning);
                    PlatformServiceProvider.Instance.AdapterTraceLogger.LogWarning($"DynamicDataEnumerator: {warning}");

                    // Duplicated display name so bail out. Caller will handle adding the original test.
                    return false;
                }
#pragma warning restore CS0618 // Type or member is obsolete

                try
                {
                    discoveredTest.TestMethod.SerializedData = DataSerializationHelper.Serialize(d);
                    discoveredTest.TestMethod.DataType = DynamicDataType.ITestDataSource;
                }
                catch (SerializationException ex)
                {
                    string warning = string.Format(CultureInfo.CurrentCulture, Resource.CannotExpandIDataSourceAttribute_CannotSerialize, index, discoveredTest.DisplayName);
                    warning += Environment.NewLine;
                    warning += ex.ToString();
                    warning = string.Format(CultureInfo.CurrentCulture, Resource.CannotExpandIDataSourceAttribute, test.TestMethod.ManagedTypeName, test.TestMethod.ManagedMethodName, warning);
                    PlatformServiceProvider.Instance.AdapterTraceLogger.LogWarning($"DynamicDataEnumerator: {warning}");

                    // Serialization failed for the type, bail out. Caller will handle adding the original test.
                    return false;
                }

                discoveredTests.Add(discoveredTest);
                testDisplayNameFirstSeen[discoveredTest.DisplayName!] = index++;
            }

            tests.AddRange(discoveredTests);
        }

        return true;
    }
}
