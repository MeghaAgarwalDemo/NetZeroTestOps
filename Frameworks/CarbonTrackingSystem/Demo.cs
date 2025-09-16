using System;
using System.Text.Json;
using CarbonAwareTestMetrics;

// Simple demo harness (can be removed in real integration)
// Simulates a couple of tests and writes metrics & delta file when baseline + optimized are produced.

class Demo
{
    static void Main(string[] args)
    {
        // REPORT_ONLY mode: just generate HTML if baseline & optimized summaries exist
        var reportsDir = Path.Combine(Directory.GetCurrentDirectory(), "Reports");
        var reportOnly = Environment.GetEnvironmentVariable("REPORT_ONLY");
        var baselineSummary = Path.Combine(reportsDir, "baseline_summary.json");
        var optimizedSummary = Path.Combine(reportsDir, "optimized_summary.json");
        if (reportOnly == "1")
        {
            Console.WriteLine("[CarbonAwareTestMetrics Demo] REPORT_ONLY=1 - Skipping simulation, generating HTML if data present.");
            if (File.Exists(baselineSummary) && File.Exists(optimizedSummary))
            {
                if (!File.Exists(Path.Combine(reportsDir, "delta_summary.json")))
                {
                    MetricsCollector.ComputeDelta(baselineSummary, optimizedSummary);
                }
                var html = HtmlReportGenerator.Generate(reportsDir);
                Console.WriteLine($"HTML report generated: {html}");
            }
            else
            {
                Console.WriteLine("Baseline or optimized summaries missing. Cannot generate HTML.");
            }
            return;
        }

        // Normal simulated mode: baseline or optimized
        var mode = args.Length > 0 ? args[0] : "baseline";
        Console.WriteLine($"[CarbonAwareTestMetrics Demo] Run mode = {mode}");

        // Simulate running tests
        RunFakeTest("Test_Login", "Auth", 500);
        RunFakeTest("Test_Checkout", "Order", 900);
        RunFakeTest("Test_Inventory", "Catalog", 650);

        // Flush metrics
        MetricsCollector.Flush(reportsDir, mode, new() {{"mode", mode}});
        Console.WriteLine($"Metrics written for {mode} run.");

        // If both baseline and optimized exist, compute delta
        if (File.Exists(baselineSummary) && File.Exists(optimizedSummary))
        {
            var delta = MetricsCollector.ComputeDelta(baselineSummary, optimizedSummary);
            Console.WriteLine("Delta summary:");
            Console.WriteLine(JsonSerializer.Serialize(delta, new JsonSerializerOptions{WriteIndented = true}));
            // Generate HTML report if baseline & optimized present
            var htmlPath = HtmlReportGenerator.Generate(reportsDir);
            Console.WriteLine($"HTML report generated: {htmlPath}");
        }
    }

    static void RunFakeTest(string id, string suite, int workMs)
    {
        MetricsCollector.Start(id, suite);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        // Simulate CPU + memory usage
        var rnd = new Random();
        var waste = 0.0;
        while (sw.ElapsedMilliseconds < workMs)
        {
            // Busy loop chunk
            for (int i = 0; i < 50_000; i++)
            {
                waste += Math.Sqrt(i + rnd.NextDouble());
            }
            System.Threading.Thread.Sleep(10); // introduce small idle to vary CPU profile
        }
        MetricsCollector.End(id, "passed");
    }
}
