using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace DijkstraFast.Benchmarks
{
    public static class BenchmarkConfigs
    {
        public static IConfig Release { get; } = CreateRelease();

        public static IConfig Debug { get; } = CreateDebug();

        internal static ManualConfig BaseCommon()
        {
            return ManualConfig.Create(DefaultConfig.Instance)
                .WithArtifactsPath("BenchmarkResults")
                .AddExporter(JsonExporter.Brief)
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddColumn(RankColumn.Arabic)
                .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Declared));
        }

        private static IConfig CreateRelease()
        {
            // Out-of-process, so each benchmark runs in a fresh, fully optimised process.
            return BaseCommon()
                .AddJob(Job.Default
                    .WithId("Release-Default")
                    .WithGcForce(true)
                    .WithGcServer(true));
        }

        private static IConfig CreateDebug()
        {
            // In-process, so breakpoints work.
            return BaseCommon()
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddJob(Job.Dry
                    .WithId("Debug-InProcess")
                    .WithToolchain(InProcessEmitToolchain.Instance));
        }
    }

    /// <summary>
    /// The config for the long-running <see cref="ScalingBenchmarks"/> and <see cref="LibraryBenchmarks"/>. It replaces the global config instead of adding to it,
    /// so only this lighter job runs: at 10 million intersections one invocation takes many seconds, and the
    /// default job's 15 or more iterations would take hours.
    /// </summary>
    public class ScalingConfig : ManualConfig
    {
        public ScalingConfig()
        {
            Add(BenchmarkConfigs.BaseCommon());

            AddJob(Job.Default
                .WithId("Scaling")
                .WithLaunchCount(1)
                .WithWarmupCount(1)
                .WithIterationCount(5)
                .WithGcForce(true)
                .WithGcServer(true));

            UnionRule = ConfigUnionRule.AlwaysUseLocal;
        }
    }
}