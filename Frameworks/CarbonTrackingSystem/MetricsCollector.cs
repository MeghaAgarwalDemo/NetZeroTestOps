using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CarbonAwareTestMetrics;

public static class CarbonConfig
{
    public static double GridIntensityGramsPerKwh { get; private set; } = 400; // default global avg
    public static double AvgCpuWatts { get; private set; } = 30; // approximate share per test process
    public static double MemWattsPerGb { get; private set; } = 0.372; // research heuristic

    public static void LoadFromEnv()
    {
        GridIntensityGramsPerKwh = ReadDouble("GRID_INTENSITY", GridIntensityGramsPerKwh);
        AvgCpuWatts = ReadDouble("AVG_CPU_WATTS", AvgCpuWatts);
        MemWattsPerGb = ReadDouble("MEM_WATTS_PER_GB", MemWattsPerGb);
    }

    private static double ReadDouble(string key, double fallback)
    {
        var v = Environment.GetEnvironmentVariable(key);
        return double.TryParse(v, out var d) ? d : fallback;
    }
}

public record TestMetric
(
    string TestId,
    string? Suite,
    string Outcome,
    double DurationSeconds,
    double CpuSeconds,
    long PeakWorkingSetBytes,
    double AvgWorkingSetBytes,
    double CpuJoules,
    double MemJoules,
    double TotalJoules,
    double Kwh,
    double Co2eGrams
)
{
    public Dictionary<string, object> ToDictionary() => new()
    {
        ["test_id"] = TestId,
        ["suite"] = Suite ?? string.Empty,
        ["outcome"] = Outcome,
        ["duration_s"] = DurationSeconds,
        ["cpu_s"] = CpuSeconds,
        ["peak_ws_mb"] = PeakWorkingSetBytes / 1024d / 1024d,
        ["avg_ws_mb"] = AvgWorkingSetBytes / 1024d / 1024d,
        ["cpu_joules"] = CpuJoules,
        ["mem_joules"] = MemJoules,
        ["total_joules"] = TotalJoules,
        ["kwh"] = Kwh,
        ["co2e_g"] = Co2eGrams
    };
}

internal class TestRuntimeState
{
    public TestRuntimeState(string? suite)
    {
        Suite = suite;
    }
    public string? Suite { get; }
    public DateTimeOffset StartWall { get; } = DateTimeOffset.UtcNow;
    public TimeSpan StartCpu { get; } = Process.GetCurrentProcess().TotalProcessorTime;
    public long StartWorkingSet { get; } = Process.GetCurrentProcess().WorkingSet64;
    public long PeakWorkingSet { get; private set; }
    public long WorkingSetSamplesTotal { get; private set; }
    public int WorkingSetSamples { get; private set; }

    public void Sample()
    {
        var ws = Process.GetCurrentProcess().WorkingSet64;
        PeakWorkingSet = Math.Max(PeakWorkingSet, ws);
        WorkingSetSamplesTotal += ws;
        WorkingSetSamples++;
    }

    public double AverageWorkingSetBytes => WorkingSetSamples == 0 ? StartWorkingSet : (double)WorkingSetSamplesTotal / WorkingSetSamples;
}

public static class MetricsCollector
{
    private static readonly ConcurrentDictionary<string, TestRuntimeState> _running = new();
    private static readonly ConcurrentBag<TestMetric> _completed = new();
    private static readonly Stopwatch _samplerSw = Stopwatch.StartNew();
    private static readonly TimeSpan SampleInterval = TimeSpan.FromMilliseconds(200);
    private static readonly object _flushLock = new();
    private static bool _samplerActive;

    static MetricsCollector()
    {
        CarbonConfig.LoadFromEnv();
        StartBackgroundSampler();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => FlushIfAny("carbon_metrics_last_run");
    }

    public static void Start(string testId, string? suite = null)
    {
        _running[testId] = new TestRuntimeState(suite);
    }

    public static void End(string testId, string outcome = "passed")
    {
        if (!_running.TryRemove(testId, out var state)) return;
        var proc = Process.GetCurrentProcess();
        var now = DateTimeOffset.UtcNow;
        var cpuTotal = proc.TotalProcessorTime - state.StartCpu;
        var duration = (now - state.StartWall).TotalSeconds;

        var cpuJoules = cpuTotal.TotalSeconds * CarbonConfig.AvgCpuWatts; // J = W * s
        var memJoules = (state.AverageWorkingSetBytes / 1024d / 1024d / 1024d) * CarbonConfig.MemWattsPerGb * duration; // W * s ⇒ J
        var totalJ = cpuJoules + memJoules;
        var kwh = totalJ / 3_600_000d;
        var co2g = kwh * CarbonConfig.GridIntensityGramsPerKwh;

        var metric = new TestMetric(
            testId,
            state.Suite,
            outcome,
            duration,
            cpuTotal.TotalSeconds,
            state.PeakWorkingSet,
            state.AverageWorkingSetBytes,
            cpuJoules,
            memJoules,
            totalJ,
            kwh,
            co2g
        );
        _completed.Add(metric);
    }

    public static void Flush(string directory, string runLabel, Dictionary<string, object>? meta = null)
    {
        lock (_flushLock)
        {
            Directory.CreateDirectory(directory);
            var list = _completed.ToArray();
            var runSummary = BuildSummary(list, meta, runLabel);
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(Path.Combine(directory, $"{runLabel}_per_test.json"), JsonSerializer.Serialize(list.Select(l => l.ToDictionary()), options));
            File.WriteAllText(Path.Combine(directory, $"{runLabel}_summary.json"), JsonSerializer.Serialize(runSummary, options));
        }
    }

    private static Dictionary<string, object> BuildSummary(IEnumerable<TestMetric> metrics, Dictionary<string, object>? meta, string runLabel)
    {
        var arr = metrics.ToArray();
        double sum(Func<TestMetric, double> f) => arr.Sum(f);
        double avg(Func<TestMetric, double> f) => arr.Length == 0 ? 0 : arr.Average(f);
        return new Dictionary<string, object>
        {
            ["run_label"] = runLabel,
            ["tests"] = arr.Length,
            ["duration_s_total"] = sum(m => m.DurationSeconds),
            ["cpu_s_total"] = sum(m => m.CpuSeconds),
            ["total_joules"] = sum(m => m.TotalJoules),
            ["total_kwh"] = sum(m => m.Kwh),
            ["total_co2e_g"] = sum(m => m.Co2eGrams),
            ["avg_co2e_g_per_test"] = avg(m => m.Co2eGrams),
            ["avg_joules_per_test"] = avg(m => m.TotalJoules),
            ["grid_intensity_g_per_kwh"] = CarbonConfig.GridIntensityGramsPerKwh,
            ["avg_cpu_watts"] = CarbonConfig.AvgCpuWatts,
            ["mem_watts_per_gb"] = CarbonConfig.MemWattsPerGb,
            ["generated_utc"] = DateTimeOffset.UtcNow,
            ["meta"] = meta ?? new Dictionary<string, object>()
        };
    }

    public static Dictionary<string, object> ComputeDelta(string baselineSummaryPath, string optimizedSummaryPath)
    {
        var baseline = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(baselineSummaryPath))!;
        var optimized = JsonSerializer.Deserialize<Dictionary<string, object>>(File.ReadAllText(optimizedSummaryPath))!;
        double Get(Dictionary<string, object> d, string k) => d.TryGetValue(k, out var v) && double.TryParse(v.ToString(), out var dd) ? dd : 0;

        var delta = new Dictionary<string, object>
        {
            ["baseline_total_co2e_g"] = Get(baseline, "total_co2e_g"),
            ["optimized_total_co2e_g"] = Get(optimized, "total_co2e_g"),
            ["co2e_g_saved"] = Get(baseline, "total_co2e_g") - Get(optimized, "total_co2e_g"),
            ["percent_reduction_co2e"] = PercentReduction(Get(baseline, "total_co2e_g"), Get(optimized, "total_co2e_g")),
            ["baseline_total_joules"] = Get(baseline, "total_joules"),
            ["optimized_total_joules"] = Get(optimized, "total_joules"),
            ["joules_saved"] = Get(baseline, "total_joules") - Get(optimized, "total_joules"),
            ["percent_reduction_joules"] = PercentReduction(Get(baseline, "total_joules"), Get(optimized, "total_joules"))
        };
        var options = new JsonSerializerOptions { WriteIndented = true };
        var deltaPath = Path.Combine(Path.GetDirectoryName(optimizedSummaryPath)!, "delta_summary.json");
        File.WriteAllText(deltaPath, JsonSerializer.Serialize(delta, options));
        return delta;
    }

    private static double PercentReduction(double baseline, double current) => baseline <= 0 ? 0 : (baseline - current) / baseline * 100.0;

    private static void StartBackgroundSampler()
    {
        if (_samplerActive) return;
        _samplerActive = true;
        var thread = new Thread(() =>
        {
            while (_samplerActive)
            {
                if (_samplerSw.Elapsed >= SampleInterval)
                {
                    foreach (var kv in _running)
                    {
                        kv.Value.Sample();
                    }
                    _samplerSw.Restart();
                }
                Thread.Sleep(50);
            }
        }) { IsBackground = true, Name = "CarbonMetricsSampler" };
        thread.Start();
    }

    private static void FlushIfAny(string runLabel)
    {
        if (_completed.IsEmpty) return;
        Flush(Path.Combine(Directory.GetCurrentDirectory(), "Reports"), runLabel);
    }
}
