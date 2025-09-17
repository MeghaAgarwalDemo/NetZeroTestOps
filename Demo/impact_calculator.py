#!/usr/bin/env python3
"""
NetZero TestOps - Carbon Impact Calculator
Advanced calculator for measuring and projecting carbon impact of testing frameworks
"""

import json
import math
from datetime import datetime, timedelta
from typing import Dict, List, Tuple

class CarbonImpactCalculator:
    def __init__(self):
        # Standard emission factors (based on industry averages)
        self.GRID_INTENSITY = 400  # g CO₂/kWh (US average)
        self.CPU_BASE_WATTS = 30   # Base CPU power consumption
        self.MEMORY_WATTS_PER_GB = 0.372  # Memory power per GB
        self.STORAGE_WATTS_PER_GB = 0.65  # Storage power per GB accessed
        self.NETWORK_WATTS_PER_MB = 0.0036  # Network power per MB transferred
        
        # Cloud computing emission factors
        self.CLOUD_PUE = 1.12  # Power Usage Effectiveness for modern data centers
        
        # Economic factors for ROI calculations
        self.ENERGY_COST_PER_KWH = 0.12  # USD per kWh
        self.CARBON_CREDIT_COST_PER_TON = 25  # USD per ton CO₂e
        
    def calculate_base_consumption(self, duration_seconds: float, cpu_utilization: float, 
                                 memory_gb: float, storage_gb: float = 0, 
                                 network_mb: float = 0) -> Dict:
        """Calculate base energy consumption and carbon footprint"""
        
        # Convert duration to hours for energy calculations
        duration_hours = duration_seconds / 3600
        
        # Calculate component energy consumption (Wh)
        cpu_energy = self.CPU_BASE_WATTS * cpu_utilization * duration_hours
        memory_energy = self.MEMORY_WATTS_PER_GB * memory_gb * duration_hours
        storage_energy = self.STORAGE_WATTS_PER_GB * storage_gb * duration_hours
        network_energy = self.NETWORK_WATTS_PER_MB * network_mb * duration_hours
        
        # Total energy consumption
        total_energy_wh = cpu_energy + memory_energy + storage_energy + network_energy
        
        # Apply cloud PUE factor (accounts for cooling, power distribution losses)
        total_energy_with_pue_wh = total_energy_wh * self.CLOUD_PUE
        
        # Convert to other units
        total_energy_joules = total_energy_with_pue_wh * 3600
        total_energy_kwh = total_energy_with_pue_wh / 1000
        
        # Calculate carbon footprint
        carbon_g_co2e = total_energy_kwh * self.GRID_INTENSITY
        carbon_kg_co2e = carbon_g_co2e / 1000
        
        return {
            'energy': {
                'total_wh': total_energy_with_pue_wh,
                'total_joules': total_energy_joules,
                'total_kwh': total_energy_kwh,
                'breakdown': {
                    'cpu_wh': cpu_energy,
                    'memory_wh': memory_energy,
                    'storage_wh': storage_energy,
                    'network_wh': network_energy,
                    'pue_overhead_wh': total_energy_with_pue_wh - total_energy_wh
                }
            },
            'carbon': {
                'total_g_co2e': carbon_g_co2e,
                'total_kg_co2e': carbon_kg_co2e,
                'grid_intensity_used': self.GRID_INTENSITY
            },
            'input_parameters': {
                'duration_seconds': duration_seconds,
                'cpu_utilization': cpu_utilization,
                'memory_gb': memory_gb,
                'storage_gb': storage_gb,
                'network_mb': network_mb
            }
        }
    
    def calculate_framework_comparison(self, sustainable_metrics: Dict, 
                                     wasteful_metrics: Dict) -> Dict:
        """Compare two testing frameworks and calculate improvements"""
        
        # Calculate consumption for both frameworks
        sustainable_result = self.calculate_base_consumption(**sustainable_metrics)
        wasteful_result = self.calculate_base_consumption(**wasteful_metrics)
        
        # Calculate improvements
        energy_reduction = ((wasteful_result['energy']['total_joules'] - 
                           sustainable_result['energy']['total_joules']) / 
                          wasteful_result['energy']['total_joules']) * 100
        
        carbon_reduction = ((wasteful_result['carbon']['total_g_co2e'] - 
                           sustainable_result['carbon']['total_g_co2e']) / 
                          wasteful_result['carbon']['total_g_co2e']) * 100
        
        performance_improvement = ((wasteful_metrics['duration_seconds'] - 
                                  sustainable_metrics['duration_seconds']) / 
                                 wasteful_metrics['duration_seconds']) * 100
        
        # Calculate absolute savings
        energy_saved_joules = (wasteful_result['energy']['total_joules'] - 
                              sustainable_result['energy']['total_joules'])
        carbon_saved_g_co2e = (wasteful_result['carbon']['total_g_co2e'] - 
                              sustainable_result['carbon']['total_g_co2e'])
        
        return {
            'timestamp': datetime.now().isoformat(),
            'comparison_summary': {
                'energy_reduction_percent': energy_reduction,
                'carbon_reduction_percent': carbon_reduction,
                'performance_improvement_percent': performance_improvement,
                'energy_saved_joules': energy_saved_joules,
                'carbon_saved_g_co2e': carbon_saved_g_co2e
            },
            'sustainable_framework': sustainable_result,
            'wasteful_framework': wasteful_result,
            'efficiency_analysis': {
                'cpu_efficiency_improvement': ((wasteful_metrics['cpu_utilization'] - 
                                              sustainable_metrics['cpu_utilization']) / 
                                             wasteful_metrics['cpu_utilization']) * 100,
                'memory_efficiency_improvement': ((wasteful_metrics['memory_gb'] - 
                                                 sustainable_metrics['memory_gb']) / 
                                                wasteful_metrics['memory_gb']) * 100
            }
        }
    
    def calculate_annual_projections(self, daily_tests: int, comparison_result: Dict) -> Dict:
        """Calculate annual carbon and cost savings projections"""
        
        # Daily savings
        daily_energy_saved_kwh = (comparison_result['comparison_summary']['energy_saved_joules'] * 
                                 daily_tests) / 3600000  # Convert J to kWh
        daily_carbon_saved_kg = (comparison_result['comparison_summary']['carbon_saved_g_co2e'] * 
                                daily_tests) / 1000  # Convert g to kg
        
        # Annual projections (365 days)
        annual_energy_saved_kwh = daily_energy_saved_kwh * 365
        annual_carbon_saved_kg = daily_carbon_saved_kg * 365
        annual_carbon_saved_tons = annual_carbon_saved_kg / 1000
        
        # Cost savings
        annual_energy_cost_savings = annual_energy_saved_kwh * self.ENERGY_COST_PER_KWH
        annual_carbon_credit_savings = annual_carbon_saved_tons * self.CARBON_CREDIT_COST_PER_TON
        total_annual_savings = annual_energy_cost_savings + annual_carbon_credit_savings
        
        return {
            'projection_parameters': {
                'daily_tests': daily_tests,
                'projection_days': 365,
                'energy_cost_per_kwh': self.ENERGY_COST_PER_KWH,
                'carbon_credit_cost_per_ton': self.CARBON_CREDIT_COST_PER_TON
            },
            'annual_environmental_impact': {
                'energy_saved_kwh': annual_energy_saved_kwh,
                'carbon_saved_kg': annual_carbon_saved_kg,
                'carbon_saved_tons': annual_carbon_saved_tons,
                'equivalent_cars_removed': annual_carbon_saved_tons / 4.6,  # Average car emits 4.6 tons CO₂/year
                'equivalent_trees_planted': annual_carbon_saved_kg / 22  # One tree absorbs ~22kg CO₂/year
            },
            'annual_cost_savings': {
                'energy_cost_savings': annual_energy_cost_savings,
                'carbon_credit_savings': annual_carbon_credit_savings,
                'total_savings': total_annual_savings
            },
            'daily_impact': {
                'energy_saved_kwh': daily_energy_saved_kwh,
                'carbon_saved_kg': daily_carbon_saved_kg
            }
        }
    
    def generate_carbon_report(self, test_scenarios: List[Dict]) -> Dict:
        """Generate comprehensive carbon impact report"""
        
        total_comparisons = []
        
        for scenario in test_scenarios:
            comparison = self.calculate_framework_comparison(
                scenario['sustainable_metrics'],
                scenario['wasteful_metrics']
            )
            comparison['scenario_name'] = scenario['name']
            total_comparisons.append(comparison)
        
        # Calculate aggregated metrics
        total_energy_saved = sum(c['comparison_summary']['energy_saved_joules'] 
                               for c in total_comparisons)
        total_carbon_saved = sum(c['comparison_summary']['carbon_saved_g_co2e'] 
                               for c in total_comparisons)
        avg_carbon_reduction = sum(c['comparison_summary']['carbon_reduction_percent'] 
                                 for c in total_comparisons) / len(total_comparisons)
        avg_energy_reduction = sum(c['comparison_summary']['energy_reduction_percent'] 
                                 for c in total_comparisons) / len(total_comparisons)
        
        # Generate annual projections (assuming 100 tests per day)
        mock_comparison = {
            'comparison_summary': {
                'energy_saved_joules': total_energy_saved / len(test_scenarios),
                'carbon_saved_g_co2e': total_carbon_saved / len(test_scenarios)
            }
        }
        annual_projections = self.calculate_annual_projections(100, mock_comparison)
        
        return {
            'report_metadata': {
                'generated_timestamp': datetime.now().isoformat(),
                'scenarios_analyzed': len(test_scenarios),
                'calculator_version': '1.0.0'
            },
            'executive_summary': {
                'average_carbon_reduction_percent': avg_carbon_reduction,
                'average_energy_reduction_percent': avg_energy_reduction,
                'total_energy_saved_joules': total_energy_saved,
                'total_carbon_saved_g_co2e': total_carbon_saved
            },
            'detailed_scenarios': total_comparisons,
            'annual_projections': annual_projections,
            'methodology': {
                'grid_intensity_g_co2_kwh': self.GRID_INTENSITY,
                'cpu_base_watts': self.CPU_BASE_WATTS,
                'memory_watts_per_gb': self.MEMORY_WATTS_PER_GB,
                'cloud_pue_factor': self.CLOUD_PUE,
                'standards_compliance': [
                    'Software Carbon Intensity (SCI) Specification',
                    'GHG Protocol Scope 2 Guidance',
                    'Green Software Foundation Standards'
                ]
            }
        }

def main():
    """Demonstration of the Carbon Impact Calculator"""
    calculator = CarbonImpactCalculator()
    
    # Sample test scenarios for demonstration
    test_scenarios = [
        {
            'name': 'E-commerce Login Test',
            'sustainable_metrics': {
                'duration_seconds': 2.1,
                'cpu_utilization': 0.20,
                'memory_gb': 0.8,
                'storage_gb': 0.1,
                'network_mb': 2.5
            },
            'wasteful_metrics': {
                'duration_seconds': 6.8,
                'cpu_utilization': 0.75,
                'memory_gb': 3.2,
                'storage_gb': 0.5,
                'network_mb': 8.1
            }
        },
        {
            'name': 'Product Search Test',
            'sustainable_metrics': {
                'duration_seconds': 1.8,
                'cpu_utilization': 0.18,
                'memory_gb': 0.9,
                'storage_gb': 0.2,
                'network_mb': 3.1
            },
            'wasteful_metrics': {
                'duration_seconds': 5.9,
                'cpu_utilization': 0.68,
                'memory_gb': 2.8,
                'storage_gb': 0.7,
                'network_mb': 9.4
            }
        },
        {
            'name': 'Checkout Process Test',
            'sustainable_metrics': {
                'duration_seconds': 3.2,
                'cpu_utilization': 0.25,
                'memory_gb': 1.1,
                'storage_gb': 0.3,
                'network_mb': 4.2
            },
            'wasteful_metrics': {
                'duration_seconds': 8.7,
                'cpu_utilization': 0.82,
                'memory_gb': 3.8,
                'storage_gb': 0.9,
                'network_mb': 12.6
            }
        }
    ]
    
    print("🧮 NetZero TestOps - Carbon Impact Calculator")
    print("=" * 60)
    
    # Generate comprehensive report
    report = calculator.generate_carbon_report(test_scenarios)
    
    # Save report to file
    import os
    os.makedirs('../Reports', exist_ok=True)
    with open('../Reports/carbon_impact_analysis.json', 'w') as f:
        json.dump(report, f, indent=2)
    
    # Display summary
    print("\n📊 CARBON IMPACT ANALYSIS COMPLETE")
    print("-" * 60)
    print(f"Average Carbon Reduction: {report['executive_summary']['average_carbon_reduction_percent']:.1f}%")
    print(f"Average Energy Reduction: {report['executive_summary']['average_energy_reduction_percent']:.1f}%")
    print(f"Total Energy Saved: {report['executive_summary']['total_energy_saved_joules']:.2f} J")
    print(f"Total Carbon Saved: {report['executive_summary']['total_carbon_saved_g_co2e']:.6f} g CO₂e")
    
    annual = report['annual_projections']['annual_environmental_impact']
    print(f"\n🌍 ANNUAL PROJECTIONS (100 tests/day):")
    print(f"Energy Savings: {annual['energy_saved_kwh']:.0f} kWh/year")
    print(f"Carbon Savings: {annual['carbon_saved_tons']:.2f} tons CO₂e/year")
    print(f"Equivalent to: {annual['equivalent_cars_removed']:.1f} cars removed from roads")
    print(f"Equivalent to: {annual['equivalent_trees_planted']:.0f} trees planted")
    
    cost_savings = report['annual_projections']['annual_cost_savings']
    print(f"\n💰 ANNUAL COST SAVINGS:")
    print(f"Energy Cost Savings: ${cost_savings['energy_cost_savings']:.2f}")
    print(f"Carbon Credit Savings: ${cost_savings['carbon_credit_savings']:.2f}")
    print(f"Total Annual Savings: ${cost_savings['total_savings']:.2f}")
    
    print(f"\n✅ Detailed report saved to: ../Reports/carbon_impact_analysis.json")

if __name__ == "__main__":
    main()
