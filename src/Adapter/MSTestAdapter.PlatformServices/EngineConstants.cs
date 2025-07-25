﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Microsoft.VisualStudio.TestPlatform.MSTestAdapter.PlatformServices;

internal static class EngineConstants
{
#if NETFRAMEWORK
    /// <summary>
    ///  Constants for detecting .net framework.
    /// </summary>
    public const string TargetFrameworkAttributeFullName = "System.Runtime.Versioning.TargetFrameworkAttribute";

    public const string DotNetFrameWorkStringPrefix = ".NETFramework,Version=";

    public const string TargetFrameworkName = "TargetFrameworkName";

    public const string PublicAssemblies = "PublicAssemblies";

    public const string PrivateAssemblies = "PrivateAssemblies";
#endif

    internal const string DllExtension = ".dll";
    internal const string ExeExtension = ".exe";
    internal const string AppxPackageExtension = ".appx";

#if NETFRAMEWORK
    internal const string PhoneAppxPackageExtension = ".appx";
#endif

    /// <summary>
    /// The 3rd level entry (class) name in the hierarchy array.
    /// </summary>
    internal const string AssemblyFixturesHierarchyClassName = "[Assembly]";

    /// <summary>
    /// Discover fixtures or not.
    /// </summary>
    internal const string FixturesTestTrait = "FixturesTrait";

    /// <summary>
    /// Assembly initialize.
    /// </summary>
    internal const string AssemblyInitializeFixtureTrait = "AssemblyInitialize";

    /// <summary>
    /// Assembly cleanup.
    /// </summary>
    internal const string AssemblyCleanupFixtureTrait = "AssemblyCleanup";

    /// <summary>
    /// Class initialize.
    /// </summary>
    internal const string ClassInitializeFixtureTrait = "ClassInitialize";

    /// <summary>
    /// Class cleanup.
    /// </summary>
    internal const string ClassCleanupFixtureTrait = "ClassCleanup";

    /// <summary>
    /// The name of test run parameters node in the runsettings.
    /// </summary>
    internal const string TestRunParametersName = "TestRunParameters";

    #region Test Property registration

    internal static readonly TestProperty WorkItemIdsProperty = TestProperty.Register("WorkItemIds", WorkItemIdsLabel, typeof(string[]), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty CssIterationProperty = TestProperty.Register("CssIteration", CssIterationLabel, typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty CssProjectStructureProperty = TestProperty.Register("CssProjectStructure", CssProjectStructureLabel, typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty TestClassNameProperty = TestProperty.Register("MSTestDiscoverer.TestClassName", TestClassNameLabel, typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty DeclaringClassNameProperty = TestProperty.Register("MSTestDiscoverer.DeclaringClassName", DeclaringClassNameLabel, typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

#pragma warning disable CS0618 // Type or member is obsolete
    internal static readonly TestProperty TestCategoryProperty = TestProperty.Register("MSTestDiscoverer.TestCategory", TestCategoryLabel, typeof(string[]), TestPropertyAttributes.Hidden | TestPropertyAttributes.Trait, typeof(TestCase));
#pragma warning restore CS0618 // Type or member is obsolete

    internal static readonly TestProperty PriorityProperty = TestProperty.Register("MSTestDiscoverer.Priority", PriorityLabel, typeof(int), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty DeploymentItemsProperty = TestProperty.Register("MSTestDiscoverer.DeploymentItems", DeploymentItemsLabel, typeof(KeyValuePair<string, string>[]), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty DoNotParallelizeProperty = TestProperty.Register("MSTestDiscoverer.DoNotParallelize", DoNotParallelizeLabel, typeof(bool), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty ExecutionIdProperty = TestProperty.Register("ExecutionId", ExecutionIdLabel, typeof(Guid), TestPropertyAttributes.Hidden, typeof(TestResult));

    internal static readonly TestProperty ParentExecIdProperty = TestProperty.Register("ParentExecId", ParentExecIdLabel, typeof(Guid), TestPropertyAttributes.Hidden, typeof(TestResult));

    internal static readonly TestProperty InnerResultsCountProperty = TestProperty.Register("InnerResultsCount", InnerResultsCountLabel, typeof(int), TestPropertyAttributes.Hidden, typeof(TestResult));

    internal static readonly TestProperty TestRunIdProperty = TestProperty.Register(TestRunId, TestRunId, typeof(int), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty TestPlanIdProperty = TestProperty.Register(TestPlanId, TestPlanId, typeof(int), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty TestCaseIdProperty = TestProperty.Register(TestCaseId, TestCaseId, typeof(int), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty TestPointIdProperty = TestProperty.Register(TestPointId, TestPointId, typeof(int), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty TestConfigurationIdProperty = TestProperty.Register(TestConfigurationId, TestConfigurationId, typeof(int), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty TestConfigurationNameProperty = TestProperty.Register(TestConfigurationName, TestConfigurationName, typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty IsInLabEnvironmentProperty = TestProperty.Register(IsInLabEnvironment, IsInLabEnvironment, typeof(bool), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty BuildConfigurationIdProperty = TestProperty.Register(BuildConfigurationId, BuildConfigurationId, typeof(int), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty BuildDirectoryProperty = TestProperty.Register(BuildDirectory, BuildDirectory, typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty BuildFlavorProperty = TestProperty.Register(BuildFlavor, BuildFlavor, typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty BuildNumberProperty = TestProperty.Register(BuildNumber, BuildNumber, typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty BuildPlatformProperty = TestProperty.Register(BuildPlatform, BuildPlatform, typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty BuildUriProperty = TestProperty.Register(BuildUri, BuildUri, typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty TfsServerCollectionUrlProperty = TestProperty.Register(TfsServerCollectionUrl, TfsServerCollectionUrl, typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty TfsTeamProjectProperty = TestProperty.Register(TfsTeamProject, TfsTeamProject, typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty TestDynamicDataTypeProperty = TestProperty.Register("MSTest.DynamicDataType", "DynamicDataType", typeof(int), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty TestDynamicDataProperty = TestProperty.Register("MSTest.DynamicData", "DynamicData", typeof(string[]), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty TestIdGenerationStrategyProperty = TestProperty.Register("MSTest.TestIdGenerationStrategy", "TestIdGenerationStrategy", typeof(int), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty TestDataSourceIgnoreMessageProperty = TestProperty.Register("MSTest.TestDataSourceIgnoreMessageProperty", "TestDataSourceIgnoreMessageProperty", typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));
    #endregion

    #region Private Constants

    /// <summary>
    /// These are the Test properties used by the adapter, which essentially correspond
    /// to attributes on tests, and may be available in command line/TeamBuild to filter tests.
    /// These Property names should not be localized.
    /// </summary>
    private const string TestClassNameLabel = "ClassName";
    private const string DeclaringClassNameLabel = "DeclaringClassName";
    private const string TestCategoryLabel = "TestCategory";
    private const string PriorityLabel = "Priority";
    private const string DeploymentItemsLabel = "DeploymentItems";
    private const string DoNotParallelizeLabel = "DoNotParallelize";
    private const string ExecutionIdLabel = "ExecutionId";
    private const string ParentExecIdLabel = "ParentExecId";
    private const string InnerResultsCountLabel = "InnerResultsCount";
    private const string CssIterationLabel = "CssIteration";
    private const string CssProjectStructureLabel = "CssProjectStructure";
    private const string WorkItemIdsLabel = "WorkItemIds";

    private const string TestRunId = "__Tfs_TestRunId__";
    private const string TestPlanId = "__Tfs_TestPlanId__";
    private const string TestCaseId = "__Tfs_TestCaseId__";
    private const string TestPointId = "__Tfs_TestPointId__";
    private const string TestConfigurationId = "__Tfs_TestConfigurationId__";
    private const string TestConfigurationName = "__Tfs_TestConfigurationName__";
    private const string IsInLabEnvironment = "__Tfs_IsInLabEnvironment__";
    private const string BuildConfigurationId = "__Tfs_BuildConfigurationId__";
    private const string BuildDirectory = "__Tfs_BuildDirectory__";
    private const string BuildFlavor = "__Tfs_BuildFlavor__";
    private const string BuildNumber = "__Tfs_BuildNumber__";
    private const string BuildPlatform = "__Tfs_BuildPlatform__";
    private const string BuildUri = "__Tfs_BuildUri__";
    private const string TfsServerCollectionUrl = "__Tfs_TfsServerCollectionUrl__";
    private const string TfsTeamProject = "__Tfs_TeamProject__";

    #endregion

    /// <summary>
    /// Uri of the MSTest executor.
    /// </summary>
    internal const string ExecutorUriString = "executor://MSTestAdapter/v2";

    /// <summary>
    /// The executor uri for this adapter.
    /// </summary>
    internal static readonly Uri ExecutorUri = new(ExecutorUriString);
}
