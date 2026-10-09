using System;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Running;

namespace DijkstraFast.Benchmarks
{
    /*
    HOW TO RUN (CLI)

    1) Open a shell in this project's folder (where the .csproj lives).

    2) Release (recommended):
       dotnet run -c Release -- --anyCategories PointToPoint Search Graph

       That is the quick set: maps of 10,000 and 100,000 intersections, about 10 minutes.
       `--filter *` runs everything, including the Scaling and Libraries categories below (about 30 minutes in all).

       Debug (single dry run in-process, so breakpoints work; Scaling and Libraries keep their own config):
       dotnet run -c Debug -- --debug --anyCategories PointToPoint Search Graph

    3) Useful filters & options (BenchmarkDotNet built-ins):
       https://benchmarkdotnet.org/articles/guides/console-args.html

       There is no option to pick parameter values, such as one map size, from the command line.

    4) Examples:
       # Run one category: PointToPoint, Search or Graph
       dotnet run -c Release -- --anyCategories PointToPoint

       # Route time from 10,000 to 10 million intersections (about 11 minutes, peaks at about 4 GB of memory)
       dotnet run -c Release -- --anyCategories Scaling

       # DijkstraFast against QuikGraph and Dijkstra.NET, 10,000 to 1 million intersections (about 8 minutes)
       dotnet run -c Release -- --anyCategories Libraries

       # Run one specific method
       dotnet run -c Release -- --filter *PointToPointBenchmarks.AStar*

       # Memory each library's graph keeps alive (not a benchmark; prints a table in about a minute)
       dotnet run -c Release -- --graph-memory

    5) Where are the results?
       In the BenchmarkResults folder under the directory you run from (see BenchmarkConfigs),
       as ...-report.html, ...-report.csv, ...-report-github.md and ...-report-brief.json.
    */
    public static class Program
    {
        private const string ArgDebug = "--debug";
        private const string ArgRelease = "--release";
        private const string ArgGraphMemory = "--graph-memory";

        public static int Main(string[] args)
        {
            if (HasArg(args, ArgGraphMemory))
            {
                GraphMemory.Print();

                return 0;
            }

            var isDebugFlag = HasArg(args, ArgDebug);
            var isReleaseFlag = HasArg(args, ArgRelease);

            if (isDebugFlag && isReleaseFlag)
            {
                Console.WriteLine($"Cannot use {ArgDebug} and {ArgRelease} together.");

                return 1;
            }

            var config = isDebugFlag || (!isReleaseFlag && Debugger.IsAttached)
                ? BenchmarkConfigs.Debug
                : BenchmarkConfigs.Release;

            BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(RemoveArgs(args, ArgDebug, ArgRelease), config);

            return 0;
        }

        private static bool HasArg(string[] args, string arg)
        {
            return args.Any(a => string.Equals(a, arg, StringComparison.OrdinalIgnoreCase));
        }

        private static string[] RemoveArgs(string[] args, params string[] argsToRemove)
        {
            return args.Where(a => !argsToRemove.Any(r => string.Equals(a, r, StringComparison.OrdinalIgnoreCase))).ToArray();
        }
    }
}