// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bicep.Core.Diagnostics;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Samples;
using Bicep.Core.Semantics;
using Bicep.Core.UnitTests;
using Bicep.Core.UnitTests.Assertions;
using Bicep.Core.UnitTests.Mock;
using Bicep.Core.UnitTests.Utils;
using Bicep.Core.Workspaces;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static Bicep.Core.Samples.DataSet;

namespace Bicep.Core.IntegrationTests
{
    [TestClass]
    public class RegistryTests
    {
        [NotNull]
        public TestContext? TestContext { get; set; }

        [TestMethod]
        public async Task InvalidRootCachePathShouldProduceReasonableErrors()
        {
            var dataSet = DataSets.Registry_LF;

            var outputDirectory = dataSet.SaveFilesToTestDirectory(TestContext);
            var clientFactory = dataSet.CreateMockRegistryClients(TestContext);
            var templateSpecRepositoryFactory = dataSet.CreateMockTemplateSpecRepositoryFactory(TestContext);
            await dataSet.PublishModulesToRegistryAsync(clientFactory, TestContext);

            var fileUri = PathHelper.FilePathToFileUrl(Path.Combine(outputDirectory, DataSet.TestFileMain));

            var badCacheDirectory = FileHelper.GetCacheRootPath(TestContext);
            Directory.CreateDirectory(badCacheDirectory);

            var badCachePath = Path.Combine(badCacheDirectory, "file.txt");
            File.Create(badCachePath);
            File.Exists(badCachePath).Should().BeTrue();

            // cache root points to a file
            var features = new Mock<IFeatureProvider>(MockBehavior.Strict);
            features.Setup(m => m.RegistryEnabled).Returns(true);
            features.SetupGet(m => m.CacheRootDirectory).Returns(badCachePath);

            var dispatcher = new ModuleDispatcher(new DefaultModuleRegistryProvider(BicepTestConstants.FileResolver, clientFactory, templateSpecRepositoryFactory, features.Object));

            var workspace = new Workspace();
            var configuration = BicepTestConstants.ConfigurationManager.GetConfiguration(fileUri);
            var sourceFileGrouping = SourceFileGroupingBuilder.Build(BicepTestConstants.FileResolver, dispatcher, workspace, fileUri, configuration);
            if (await dispatcher.RestoreModules(configuration, dispatcher.GetValidModuleReferences(sourceFileGrouping.ModulesToRestore, configuration)))
            {
                sourceFileGrouping = SourceFileGroupingBuilder.Rebuild(dispatcher, workspace, sourceFileGrouping, configuration);
            }

            var compilation = new Compilation(BicepTestConstants.Features, BicepTestConstants.NamespaceProvider, sourceFileGrouping, configuration, BicepTestConstants.LinterAnalyzer);
            var diagnostics = compilation.GetAllDiagnosticsByBicepFile();
            diagnostics.Should().HaveCount(1);

            diagnostics.Single().Value.ExcludingLinterDiagnostics().Should().SatisfyRespectively(
                x =>
                {
                    x.Level.Should().Be(DiagnosticLevel.Error);
                    x.Code.Should().Be("BCP192");
                    x.Message.Should().StartWith("Unable to restore the module with reference \"br:mock-registry-one.invalid/demo/plan:v2\": Unable to create the local module directory \"");
                },
                x =>
                {
                    x.Level.Should().Be(DiagnosticLevel.Error);
                    x.Code.Should().Be("BCP192");
                    x.Message.Should().StartWith("Unable to restore the module with reference \"br:mock-registry-one.invalid/demo/plan:v2\": Unable to create the local module directory \"");
                },
                x =>
                {
                    x.Level.Should().Be(DiagnosticLevel.Error);
                    x.Code.Should().Be("BCP192");
                    x.Message.Should().StartWith("Unable to restore the module with reference \"br:mock-registry-two.invalid/demo/site:v3\": Unable to create the local module directory \"");
                },
                x =>
                {
                    x.Level.Should().Be(DiagnosticLevel.Error);
                    x.Code.Should().Be("BCP192");
                    x.Message.Should().StartWith("Unable to restore the module with reference \"br:mock-registry-two.invalid/demo/site:v3\": Unable to create the local module directory \"");
                },
                x =>
                {
                    x.Level.Should().Be(DiagnosticLevel.Error);
                    x.Code.Should().Be("BCP192");
                    x.Message.Should().StartWith("Unable to restore the module with reference \"ts:00000000-0000-0000-0000-000000000000/test-rg/storage-spec:1.0\": Unable to create the local module directory \"");
                },
                x =>
                {
                    x.Level.Should().Be(DiagnosticLevel.Error);
                    x.Code.Should().Be("BCP192");
                    x.Message.Should().StartWith("Unable to restore the module with reference \"ts:00000000-0000-0000-0000-000000000000/test-rg/storage-spec:1.0\": Unable to create the local module directory \"");
                },
                x =>
                {
                    x.Level.Should().Be(DiagnosticLevel.Error);
                    x.Code.Should().Be("BCP192");
                    x.Message.Should().StartWith("Unable to restore the module with reference \"ts:11111111-1111-1111-1111-111111111111/prod-rg/vnet-spec:v2\": Unable to create the local module directory \"");
                },
                x =>
                {
                    x.Level.Should().Be(DiagnosticLevel.Error);
                    x.Code.Should().Be("BCP062");
                    x.Message.Should().Be("The referenced declaration with name \"siteDeploy\" is not valid.");
                },
                x =>
                {
                    x.Level.Should().Be(DiagnosticLevel.Error);
                    x.Code.Should().Be("BCP192");
                    x.Message.Should().StartWith("Unable to restore the module with reference \"br:localhost:5000/passthrough/port:v1\": Unable to create the local module directory \"");
                },
                x =>
                {
                    x.Level.Should().Be(DiagnosticLevel.Error);
                    x.Code.Should().Be("BCP192");
                    x.Message.Should().StartWith("Unable to restore the module with reference \"br:127.0.0.1/passthrough/ipv4:v1\": Unable to create the local module directory \"");
                },
                x =>
                {
                    x.Level.Should().Be(DiagnosticLevel.Error);
                    x.Code.Should().Be("BCP192");
                    x.Message.Should().StartWith("Unable to restore the module with reference \"br:127.0.0.1:5000/passthrough/ipv4port:v1\": Unable to create the local module directory \"");
                },
                x =>
                {
                    x.Level.Should().Be(DiagnosticLevel.Error);
                    x.Code.Should().Be("BCP192");
                    x.Message.Should().StartWith("Unable to restore the module with reference \"br:[::1]/passthrough/ipv6:v1\": Unable to create the local module directory \"");
                },
                x =>
                {
                    x.Level.Should().Be(DiagnosticLevel.Error);
                    x.Code.Should().Be("BCP192");
                    x.Message.Should().StartWith("Unable to restore the module with reference \"br:[::1]:5000/passthrough/ipv6port:v1\": Unable to create the local module directory \"");
                });
        }

        [TestMethod]
        public async Task ModuleRestoreContentionShouldProduceConsistentState()
        {
            var dataSet = DataSets.Registry_LF;

            var outputDirectory = dataSet.SaveFilesToTestDirectory(TestContext);
            var clientFactory = dataSet.CreateMockRegistryClients(TestContext);
            var templateSpecRepositoryFactory = dataSet.CreateMockTemplateSpecRepositoryFactory(TestContext);
            await dataSet.PublishModulesToRegistryAsync(clientFactory, TestContext);

            var cacheDirectory = FileHelper.GetCacheRootPath(TestContext);
            Directory.CreateDirectory(cacheDirectory);

            var features = StrictMock.Of<IFeatureProvider>();
            features.Setup(m => m.RegistryEnabled).Returns(true);
            features.Setup(m => m.CacheRootDirectory).Returns(cacheDirectory);

            var dispatcher = new ModuleDispatcher(new DefaultModuleRegistryProvider(new FileResolver(), clientFactory, templateSpecRepositoryFactory, features.Object));

            var configuration = BicepTestConstants.BuiltInConfigurationWithAnalyzersDisabled;
            var moduleReferences = dataSet.RegistryModules.Values
                .OrderBy(m => m.Metadata.Target)
                .Select(m => dispatcher.TryGetModuleReference(m.Metadata.Target, configuration, out _) ?? throw new AssertFailedException($"Invalid module target '{m.Metadata.Target}'."))
                .ToImmutableList();

            moduleReferences.Should().HaveCount(7);

            // initially the cache should be empty
            foreach (var moduleReference in moduleReferences)
            {
                dispatcher.GetModuleRestoreStatus(moduleReference, configuration, out _).Should().Be(ModuleRestoreStatus.Unknown);
            }

            const int ConcurrentTasks = 50;
            var tasks = new List<Task<bool>>();
            for (int i = 0; i < ConcurrentTasks; i++)
            {
                tasks.Add(Task.Run(() => dispatcher.RestoreModules(BicepTestConstants.BuiltInConfiguration, moduleReferences)));
            }

            var result = await Task.WhenAll(tasks);
            result.Should().HaveCount(ConcurrentTasks);

            // modules should now be in the cache
            foreach (var moduleReference in moduleReferences)
            {
                dispatcher.GetModuleRestoreStatus(moduleReference, configuration, out _).Should().Be(ModuleRestoreStatus.Succeeded);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(GetModuleInfoData), DynamicDataSourceType.Method)]
        public async Task ModuleRestoreWithStuckFileLockShouldFailAfterTimeout(IEnumerable<ExternalModuleInfo> moduleInfos, int moduleCount)
        {
            var dataSet = DataSets.Registry_LF;

            var outputDirectory = dataSet.SaveFilesToTestDirectory(TestContext);
            var clientFactory = dataSet.CreateMockRegistryClients(TestContext);
            var templateSpecRepositoryFactory = dataSet.CreateMockTemplateSpecRepositoryFactory(TestContext);
            await dataSet.PublishModulesToRegistryAsync(clientFactory, TestContext);

            var cacheDirectory = FileHelper.GetCacheRootPath(TestContext);
            Directory.CreateDirectory(cacheDirectory);

            var features = StrictMock.Of<IFeatureProvider>();
            features.Setup(m => m.RegistryEnabled).Returns(true);
            features.Setup(m => m.CacheRootDirectory).Returns(cacheDirectory);

            FileResolver fileResolver = new FileResolver();
            var dispatcher = new ModuleDispatcher(new DefaultModuleRegistryProvider(fileResolver, clientFactory, templateSpecRepositoryFactory, features.Object));

            var configuration = BicepTestConstants.BuiltInConfigurationWithAnalyzersDisabled;
            var moduleReferences = moduleInfos
                .OrderBy(m => m.Metadata.Target)
                .Select(m => dispatcher.TryGetModuleReference(m.Metadata.Target, configuration, out _) ?? throw new AssertFailedException($"Invalid module target '{m.Metadata.Target}'."))
                .ToImmutableList();

            moduleReferences.Should().HaveCount(moduleCount);

            // initially the cache should be empty
            foreach (var moduleReference in moduleReferences)
            {
                dispatcher.GetModuleRestoreStatus(moduleReference, configuration, out _).Should().Be(ModuleRestoreStatus.Unknown);
            }

            var moduleFileUri = dispatcher.TryGetLocalModuleEntryPointUri(new Uri("file://main.bicep"), moduleReferences[0], configuration, out _)!;
            moduleFileUri.Should().NotBeNull();

            var moduleFilePath = moduleFileUri.LocalPath;
            var moduleDirectory = Path.GetDirectoryName(moduleFilePath)!;
            Directory.CreateDirectory(moduleDirectory);

            var lockFileName = Path.Combine(moduleDirectory, "lock");
            var lockFileUri = new Uri(lockFileName);

            var @lock = fileResolver.TryAcquireFileLock(lockFileUri);
            @lock.Should().NotBeNull();

            // let's try to restore a module while holding a lock
            using (@lock)
            {
                (await dispatcher.RestoreModules(BicepTestConstants.BuiltInConfiguration, moduleReferences)).Should().BeTrue();
            }

            // the first module should have failed due to a timeout
            dispatcher.GetModuleRestoreStatus(moduleReferences[0], configuration, out var failureBuilder).Should().Be(ModuleRestoreStatus.Failed);
            using (new AssertionScope())
            {
                failureBuilder!.Should().HaveCode("BCP192");
                failureBuilder!.Should().HaveMessageStartWith($"Unable to restore the module with reference \"{moduleReferences[0].FullyQualifiedReference}\": Exceeded the timeout of \"00:00:05\" to acquire the lock on file \"");
            }

            // all other modules should have succeeded
            foreach (var moduleReference in moduleReferences.Skip(1))
            {
                dispatcher.GetModuleRestoreStatus(moduleReference, configuration, out _).Should().Be(ModuleRestoreStatus.Succeeded);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(GetModuleInfoData), DynamicDataSourceType.Method)]
        public async Task ForceModuleRestoreWithStuckFileLockShouldFailAfterTimeout(IEnumerable<ExternalModuleInfo> moduleInfos, int moduleCount)
        {
            var dataSet = DataSets.Registry_LF;

            var outputDirectory = dataSet.SaveFilesToTestDirectory(TestContext);
            var clientFactory = dataSet.CreateMockRegistryClients(TestContext);
            var templateSpecRepositoryFactory = dataSet.CreateMockTemplateSpecRepositoryFactory(TestContext);
            await dataSet.PublishModulesToRegistryAsync(clientFactory, TestContext);

            var cacheDirectory = FileHelper.GetCacheRootPath(TestContext);
            Directory.CreateDirectory(cacheDirectory);

            var features = StrictMock.Of<IFeatureProvider>();
            features.Setup(m => m.RegistryEnabled).Returns(true);
            features.Setup(m => m.CacheRootDirectory).Returns(cacheDirectory);

            FileResolver fileResolver = new FileResolver();
            var dispatcher = new ModuleDispatcher(new DefaultModuleRegistryProvider(fileResolver, clientFactory, templateSpecRepositoryFactory, features.Object));

            var configuration = BicepTestConstants.BuiltInConfigurationWithAnalyzersDisabled;
            var moduleReferences = moduleInfos
                .OrderBy(m => m.Metadata.Target)
                .Select(m => dispatcher.TryGetModuleReference(m.Metadata.Target, configuration, out _) ?? throw new AssertFailedException($"Invalid module target '{m.Metadata.Target}'."))
                .ToImmutableList();

            moduleReferences.Should().HaveCount(moduleCount);

            // initially the cache should be empty
            foreach (var moduleReference in moduleReferences)
            {
                dispatcher.GetModuleRestoreStatus(moduleReference, configuration, out _).Should().Be(ModuleRestoreStatus.Unknown);
            }

            var moduleFileUri = dispatcher.TryGetLocalModuleEntryPointUri(new Uri("file://main.bicep"), moduleReferences[0], configuration, out _)!;
            moduleFileUri.Should().NotBeNull();

            var moduleFilePath = moduleFileUri.LocalPath;
            var moduleDirectory = Path.GetDirectoryName(moduleFilePath)!;
            Directory.CreateDirectory(moduleDirectory);

            var lockFileName = Path.Combine(moduleDirectory, "lock");
            var lockFileUri = new Uri(lockFileName);

            var @lock = fileResolver.TryAcquireFileLock(lockFileUri);
            @lock.Should().NotBeNull();

            // let's try to restore a module while holding a lock
            using (@lock)
            {
                (await dispatcher.RestoreModules(BicepTestConstants.BuiltInConfiguration, moduleReferences, forceModulesRestore: true)).Should().BeTrue();
            }

            // REF: FileLockTests.cs/FileLockShouldNotThrowIfLockFileIsDeleted()
            // Delete will succeed on Linux and Mac due to advisory nature of locks there
            using (new AssertionScope())
            {
#if WINDOWS_BUILD
                dispatcher.GetModuleRestoreStatus(moduleReferences[0], configuration, out var failureBuilder).Should().Be(ModuleRestoreStatus.Failed);

                failureBuilder!.Should().HaveCode("BCP233");
                failureBuilder!.Should().HaveMessageStartWith($"Unable to delete the module with reference \"{moduleReferences[0].FullyQualifiedReference}\" from cache: Exceeded the timeout of \"00:00:05\" for the lock on file \"{lockFileUri}\" to be released.");
#else
                dispatcher.GetModuleRestoreStatus(moduleReferences[0], configuration, out _).Should().Be(ModuleRestoreStatus.Succeeded);
#endif

                // all other modules should have succeeded
                foreach (var moduleReference in moduleReferences.Skip(1))
                {
                    dispatcher.GetModuleRestoreStatus(moduleReference, configuration, out _).Should().Be(ModuleRestoreStatus.Succeeded);
                }
            }

        }

        [DataTestMethod]
        [DynamicData(nameof(GetModuleInfoData), DynamicDataSourceType.Method)]
        public async Task ForceModuleRestoreShouldRestoreAllModules(IEnumerable<ExternalModuleInfo> moduleInfos, int moduleCount)
        {
            var dataSet = DataSets.Registry_LF;

            var outputDirectory = dataSet.SaveFilesToTestDirectory(TestContext);
            var clientFactory = dataSet.CreateMockRegistryClients(TestContext);
            var templateSpecRepositoryFactory = dataSet.CreateMockTemplateSpecRepositoryFactory(TestContext);
            await dataSet.PublishModulesToRegistryAsync(clientFactory, TestContext);

            var cacheDirectory = FileHelper.GetCacheRootPath(TestContext);
            Directory.CreateDirectory(cacheDirectory);

            var features = StrictMock.Of<IFeatureProvider>();
            features.Setup(m => m.RegistryEnabled).Returns(true);
            features.Setup(m => m.CacheRootDirectory).Returns(cacheDirectory);

            FileResolver fileResolver = new FileResolver();
            var dispatcher = new ModuleDispatcher(new DefaultModuleRegistryProvider(fileResolver, clientFactory, templateSpecRepositoryFactory, features.Object));

            var configuration = BicepTestConstants.BuiltInConfigurationWithAnalyzersDisabled;
            var moduleReferences = moduleInfos
                .OrderBy(m => m.Metadata.Target)
                .Select(m => dispatcher.TryGetModuleReference(m.Metadata.Target, configuration, out _) ?? throw new AssertFailedException($"Invalid module target '{m.Metadata.Target}'."))
                .ToImmutableList();

            moduleReferences.Should().HaveCount(moduleCount);

            // initially the cache should be empty
            foreach (var moduleReference in moduleReferences)
            {
                dispatcher.GetModuleRestoreStatus(moduleReference, configuration, out _).Should().Be(ModuleRestoreStatus.Unknown);
            }

            var moduleFileUri = dispatcher.TryGetLocalModuleEntryPointUri(new Uri("file://main.bicep"), moduleReferences[0], configuration, out _)!;
            moduleFileUri.Should().NotBeNull();

            var moduleFilePath = moduleFileUri.LocalPath;
            var moduleDirectory = Path.GetDirectoryName(moduleFilePath)!;
            Directory.CreateDirectory(moduleDirectory);

            (await dispatcher.RestoreModules(BicepTestConstants.BuiltInConfiguration, moduleReferences, forceModulesRestore: true)).Should().BeTrue();

            // all other modules should have succeeded
            foreach (var moduleReference in moduleReferences)
            {
                dispatcher.GetModuleRestoreStatus(moduleReference, configuration, out _).Should().Be(ModuleRestoreStatus.Succeeded);
            }
        }        

        public static IEnumerable<object[]> GetModuleInfoData()
        {
            yield return new object[] { DataSets.Registry_LF.RegistryModules.Values, 7 };
            yield return new object[] { DataSets.Registry_LF.TemplateSpecs.Values, 2 };
        }
    }
}
