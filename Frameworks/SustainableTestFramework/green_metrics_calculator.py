import os
import json
import pandas as pd
import matplotlib.pyplot as plt
import psutil
import time
from datetime import datetime

class GreenMetricsCalculator:
    def __init__(self, results_dir="./test_results", output_dir="./reports"):
        self.results_dir = results_dir
        self.output_dir = output_dir
        self.metrics_data = []
        os.makedirs(output_dir, exist_ok=True)
    
    def collect_test_results(self):
        if not os.path.exists(self.results_dir):
            print(f"Results directory {self.results_dir} not found!")
            return
        for filename in os.listdir(self.results_dir):
            if filename.endswith('.json'):
                with open(os.path.join(self.results_dir, filename), 'r') as f:
                    try:
                        data = json.load(f)
                        self.metrics_data.append(data)
                    except json.JSONDecodeError:
                        print(f"Error parsing {filename}")
        print(f"Collected {len(self.metrics_data)} test result files")
    
    def calculate_energy_metrics(self):
        if not self.metrics_data:
            print("No metrics data available!")
            return {}
        df = pd.DataFrame(self.metrics_data)
        energy_metrics = {
            "total_energy_consumed_joules": df.get('energy_joules', df.get('energy', 0)).sum(),
            "avg_energy_per_test": df.get('energy_joules', df.get('energy', 0)).mean(),
            "total_execution_time_sec": df.get('execution_time', 0).sum(),
            "avg_execution_time_sec": df.get('execution_time', 0).mean(),
            "total_carbon_footprint_g": df.get('carbon_footprint', df.get('co2', 0)).sum(),
            "peak_memory_usage_mb": df.get('memory_mb', df.get('memory', 0)).max(),
            "avg_cpu_utilization_percent": df.get('cpu_percent', df.get('cpu', 0)).mean(),
        }
        if 'test_complexity' in df.columns:
            energy_metrics["energy_per_complexity"] = (
                df['energy_joules'] / df['test_complexity']
            ).mean()
            efficiency_col = df['energy_joules'] / df['test_complexity']
            energy_metrics["most_efficient_test"] = df.loc[efficiency_col.idxmin()]['test_name']
            energy_metrics["least_efficient_test"] = df.loc[efficiency_col.idxmax()]['test_name']
        return energy_metrics
    
    def generate_report(self):
        self.collect_test_results()
        metrics = self.calculate_energy_metrics()
        with open(os.path.join(self.output_dir, 'green_metrics_summary.json'), 'w') as f:
            json.dump(metrics, f, indent=2)
        if self.metrics_data:
            df = pd.DataFrame(self.metrics_data)
            plt.figure(figsize=(12, 10))
            plt.subplot(2, 2, 1)
            if 'test_name' in df.columns and 'energy_joules' in df.columns:
                df.plot(kind='bar', x='test_name', y='energy_joules', ax=plt.gca())
                plt.title('Energy Consumption by Test')
                plt.xticks(rotation=45)
            plt.subplot(2, 2, 2)
            if 'execution_time' in df.columns and 'energy_joules' in df.columns:
                plt.scatter(df['execution_time'], df['energy_joules'])
                plt.title('Energy vs Execution Time')
                plt.xlabel('Execution Time (s)')
                plt.ylabel('Energy (J)')
            plt.subplot(2, 2, 3)
            if 'memory_mb' in df.columns and 'cpu_percent' in df.columns:
                plt.scatter(df['memory_mb'], df['cpu_percent'])
                plt.title('Memory vs CPU Usage')
                plt.xlabel('Memory (MB)')
                plt.ylabel('CPU (%)')
            plt.subplot(2, 2, 4)
            if 'test_suite' in df.columns and 'carbon_footprint' in df.columns:
                df.groupby('test_suite')['carbon_footprint'].sum().plot(kind='bar')
                plt.title('Carbon Footprint by Test Suite')
                plt.xticks(rotation=45)
            plt.tight_layout()
            plt.savefig(os.path.join(self.output_dir, 'green_metrics_report.png'))
            df.to_csv(os.path.join(self.output_dir, 'detailed_metrics.csv'), index=False)
        print(f"Report generated in {self.output_dir}")
        return metrics

if __name__ == "__main__":
    calculator = GreenMetricsCalculator()
    metrics = calculator.generate_report()
    print("\n=== Green QA Metrics Summary ===")
    for key, value in metrics.items():
        print(f"{key}: {value}")
