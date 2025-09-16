using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CarbonAwareTestMetrics;

public static class HtmlReportGenerator
{
    public static string Generate(string reportsDir, string outputFileName = "carbon_report.html")
    {
        Directory.CreateDirectory(reportsDir);
        var baselinePath = Path.Combine(reportsDir, "baseline_summary.json");
        var optimizedPath = Path.Combine(reportsDir, "optimized_summary.json");
        var deltaPath = Path.Combine(reportsDir, "delta_summary.json");
        var baselinePerTest = Path.Combine(reportsDir, "baseline_per_test.json");
        var optimizedPerTest = Path.Combine(reportsDir, "optimized_per_test.json");

        if (!File.Exists(baselinePath) || !File.Exists(optimizedPath))
        {
            throw new FileNotFoundException("Baseline or optimized summary JSON not found in reports directory.");
        }

        JsonNode? baseline = JsonNode.Parse(File.ReadAllText(baselinePath));
        JsonNode? optimized = JsonNode.Parse(File.ReadAllText(optimizedPath));
        JsonNode? delta = File.Exists(deltaPath) ? JsonNode.Parse(File.ReadAllText(deltaPath)) : null;
        JsonArray? baselineTests = File.Exists(baselinePerTest) ? JsonNode.Parse(File.ReadAllText(baselinePerTest)) as JsonArray : null;
        JsonArray? optimizedTests = File.Exists(optimizedPerTest) ? JsonNode.Parse(File.ReadAllText(optimizedPerTest)) as JsonArray : null;

        string Safe(JsonNode? n, string prop, string d = "0") => n?[prop]?.ToString() ?? d;

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang='en'><head><meta charset='UTF-8'>");
        sb.AppendLine("<title>Carbon Aware Test Metrics Report</title>");
        sb.AppendLine("<style>body{font-family:Arial,Helvetica,sans-serif;margin:24px;color:#222;}h1{color:#0a5d52;}table{border-collapse:collapse;margin:16px 0;width:100%;}th,td{border:1px solid #ddd;padding:6px;font-size:14px;}th{background:#0a5d52;color:#fff;}tr:nth-child(even){background:#f6f6f6;}code{background:#eee;padding:2px 4px;border-radius:4px;} .kpi{display:flex;gap:16px;flex-wrap:wrap;margin:12px 0;} .kpi div{flex:1 1 160px;background:#0a5d5214;padding:10px;border-radius:6px;} footer{margin-top:40px;font-size:12px;color:#666;} .improved{color:#0a5d52;font-weight:600;} .worse{color:#b30000;font-weight:600;} </style>");
        sb.AppendLine("<script src='https://cdn.jsdelivr.net/npm/chart.js'></script></head><body>");
        sb.AppendLine("<h1>Carbon Aware Test Metrics Report</h1>");
        sb.AppendLine("<p>Automatically generated comparison of baseline vs optimized test execution runs, including energy and carbon footprint deltas.</p>");

        sb.AppendLine("<h2>Summary KPIs</h2><div class='kpi'>");
        double baseCo2 = double.Parse(Safe(baseline, "total_co2e_g"));
        double optCo2 = double.Parse(Safe(optimized, "total_co2e_g"));
        double co2DeltaPct = baseCo2 == 0 ? 0 : (baseCo2 - optCo2) / baseCo2 * 100.0;
        double baseJ = double.Parse(Safe(baseline, "total_joules"));
        double optJ = double.Parse(Safe(optimized, "total_joules"));
        double jDeltaPct = baseJ == 0 ? 0 : (baseJ - optJ) / baseJ * 100.0;

        void Kpi(string label, string value, string? extraClass = null) => sb.AppendLine($"<div><strong>{label}</strong><br><span class='{extraClass}'>{value}</span></div>");

        Kpi("Baseline CO₂e (g)", baseCo2.ToString("G6"));
        Kpi("Optimized CO₂e (g)", optCo2.ToString("G6"));
        Kpi("CO₂e Reduction %", co2DeltaPct.ToString("F2") + "%", co2DeltaPct >= 0 ? "improved" : "worse");
        Kpi("Baseline Energy (J)", baseJ.ToString("F4"));
        Kpi("Optimized Energy (J)", optJ.ToString("F4"));
        Kpi("Energy Reduction %", jDeltaPct.ToString("F2") + "%", jDeltaPct >= 0 ? "improved" : "worse");
        Kpi("Tests (Baseline)", Safe(baseline, "tests"));
        Kpi("Tests (Optimized)", Safe(optimized, "tests"));
        sb.AppendLine("</div>");

        if (delta != null)
        {
            sb.AppendLine("<h2>Delta Detail</h2><table><thead><tr><th>Metric</th><th>Baseline</th><th>Optimized</th><th>Saved</th><th>% Reduction</th></tr></thead><tbody>");
            double savedCo2 = baseCo2 - optCo2;
            double savedJ = baseJ - optJ;
            sb.AppendLine($"<tr><td>CO₂e (g)</td><td>{baseCo2:G6}</td><td>{optCo2:G6}</td><td>{savedCo2:G6}</td><td>{co2DeltaPct:F2}%</td></tr>");
            sb.AppendLine($"<tr><td>Energy (J)</td><td>{baseJ:F4}</td><td>{optJ:F4}</td><td>{savedJ:F4}</td><td>{jDeltaPct:F2}%</td></tr>");
            sb.AppendLine("</tbody></table>");
        }

        if (baselineTests != null && optimizedTests != null)
        {
            sb.AppendLine("<h2>Per-Test Comparison</h2>");
            var baseDict = baselineTests.OfType<JsonObject>().ToDictionary(o => o["test_id"]!.ToString());
            var optDict = optimizedTests.OfType<JsonObject>().ToDictionary(o => o["test_id"]!.ToString());
            var allIds = baseDict.Keys.Union(optDict.Keys).OrderBy(k => k);
            sb.AppendLine("<table><thead><tr><th>Test</th><th>Baseline Joules</th><th>Optimized Joules</th><th>Δ Joules</th><th>Δ %</th><th>Baseline CO₂e (g)</th><th>Optimized CO₂e (g)</th></tr></thead><tbody>");
            foreach (var id in allIds)
            {
                double bj = baseDict.TryGetValue(id, out var bObj) ? double.Parse(bObj["total_joules"]!.ToString()) : 0;
                double oj = optDict.TryGetValue(id, out var oObj) ? double.Parse(oObj["total_joules"]!.ToString()) : 0;
                double bco2 = baseDict.TryGetValue(id, out bObj) && bObj.ContainsKey("co2e_g") ? double.Parse(bObj["co2e_g"]!.ToString()) : 0;
                double oco2 = optDict.TryGetValue(id, out oObj) && oObj.ContainsKey("co2e_g") ? double.Parse(oObj["co2e_g"]!.ToString()) : 0;
                double diff = bj - oj;
                double pct = bj == 0 ? 0 : diff / bj * 100.0;
                var cls = pct >= 0 ? "improved" : "worse";
                sb.AppendLine($"<tr><td>{id}</td><td>{bj:F4}</td><td>{oj:F4}</td><td class='{cls}'>{diff:F4}</td><td class='{cls}'>{pct:F2}%</td><td>{bco2:G6}</td><td>{oco2:G6}</td></tr>");
            }
            sb.AppendLine("</tbody></table>");
        }

        sb.AppendLine("<h2>Charts</h2><canvas id='energyChart' height='120'></canvas><canvas id='co2Chart' height='120' style='margin-top:30px;'></canvas>");
        // Chart data arrays
        var labelsJson = "[]"; var baseEnergySeries = "[]"; var optEnergySeries = "[]"; var baseCo2Series = "[]"; var optCo2Series = "[]";
        if (baselineTests != null && optimizedTests != null)
        {
            var baseList = baselineTests.OfType<JsonObject>().ToList();
            var optList = optimizedTests.OfType<JsonObject>().ToList();
            var union = baseList.Select(o => o["test_id"]!.ToString()).Union(optList.Select(o => o["test_id"]!.ToString())).Distinct().OrderBy(s => s).ToList();
            labelsJson = JsonSerializer.Serialize(union);
            baseEnergySeries = JsonSerializer.Serialize(union.Select(id => baseList.FirstOrDefault(o => o["test_id"]!.ToString() == id)?["total_joules"]?.ToString() ?? "0"));
            optEnergySeries = JsonSerializer.Serialize(union.Select(id => optList.FirstOrDefault(o => o["test_id"]!.ToString() == id)?["total_joules"]?.ToString() ?? "0"));
            baseCo2Series = JsonSerializer.Serialize(union.Select(id => baseList.FirstOrDefault(o => o["test_id"]!.ToString() == id)?["co2e_g"]?.ToString() ?? "0"));
            optCo2Series = JsonSerializer.Serialize(union.Select(id => optList.FirstOrDefault(o => o["test_id"]!.ToString() == id)?["co2e_g"]?.ToString() ?? "0"));
        }
        sb.AppendLine("<script>");
        sb.AppendLine($"const labels = {labelsJson};");
        sb.AppendLine($"const baseEnergy = {baseEnergySeries}.map(parseFloat);");
        sb.AppendLine($"const optEnergy = {optEnergySeries}.map(parseFloat);");
        sb.AppendLine($"const baseCo2 = {baseCo2Series}.map(parseFloat);");
        sb.AppendLine($"const optCo2 = {optCo2Series}.map(parseFloat);");
        sb.AppendLine("new Chart(document.getElementById('energyChart').getContext('2d'), {type:'bar',data:{labels:labels,datasets:[{label:'Baseline Joules',backgroundColor:'#ff7043',data:baseEnergy},{label:'Optimized Joules',backgroundColor:'#26a69a',data:optEnergy}]},options:{responsive:true,plugins:{legend:{position:'top'},title:{display:true,text:'Per-Test Energy (J)'}}}});");
        sb.AppendLine("new Chart(document.getElementById('co2Chart').getContext('2d'), {type:'bar',data:{labels:labels,datasets:[{label:'Baseline CO₂e (g)',backgroundColor:'#ffa000',data:baseCo2},{label:'Optimized CO₂e (g)',backgroundColor:'#2e7d32',data:optCo2}]},options:{responsive:true,plugins:{legend:{position:'top'},title:{display:true,text:'Per-Test CO₂e (g)'}}}});");
        sb.AppendLine("</script>");

        sb.AppendLine("<h2>Assumptions</h2><ul>");
        sb.AppendLine("<li>Grid intensity (g CO₂/kWh): " + Safe(baseline, "grid_intensity_g_per_kwh") + "</li>");
        sb.AppendLine("<li>Average CPU power (W): " + Safe(baseline, "avg_cpu_watts") + "</li>");
        sb.AppendLine("<li>Memory power per GB (W): " + Safe(baseline, "mem_watts_per_gb") + "</li>");
        sb.AppendLine("</ul>");

        sb.AppendLine("<footer>Generated " + DateTime.UtcNow.ToString("u") + " | CarbonAwareTestMetrics</footer>");
        sb.AppendLine("</body></html>");

        var outPath = Path.Combine(reportsDir, outputFileName);
        File.WriteAllText(outPath, sb.ToString());
        return outPath;
    }
}
