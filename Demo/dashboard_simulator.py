#!/usr/bin/env python3
"""
NetZero TestOps - Real-time Carbon Dashboard Simulator
Simulates a live dashboard showing carbon metrics during test execution
"""

import time
import json
import os
from datetime import datetime, timedelta
import random

class CarbonDashboardSimulator:
    def __init__(self):
        self.grid_intensity = 400  # g CO₂/kWh
        self.cpu_watts = 30
        self.memory_watts_per_gb = 0.372
        self.current_metrics = {
            'sustainable': {'energy': 0, 'carbon': 0, 'tests_run': 0},
            'wasteful': {'energy': 0, 'carbon': 0, 'tests_run': 0}
        }
        
    def simulate_test_execution(self, framework_type, test_duration):
        """Simulate test execution with real-time energy tracking"""
        if framework_type == 'sustainable':
            # Optimized framework uses less CPU and memory
            cpu_utilization = random.uniform(0.15, 0.25)  # 15-25% CPU
            memory_gb = random.uniform(0.5, 1.0)  # 0.5-1GB RAM
            network_efficiency = 0.8  # 80% efficient requests
        else:
            # Wasteful framework uses more resources
            cpu_utilization = random.uniform(0.6, 0.8)   # 60-80% CPU  
            memory_gb = random.uniform(2.0, 3.5)  # 2-3.5GB RAM
            network_efficiency = 0.3  # 30% efficient requests
            
        # Calculate energy consumption
        cpu_energy = (self.cpu_watts * cpu_utilization * test_duration) / 3600  # Wh
        memory_energy = (self.memory_watts_per_gb * memory_gb * test_duration) / 3600  # Wh
        
        # Add network overhead for inefficient frameworks
        network_overhead = 0 if framework_type == 'sustainable' else random.uniform(0.1, 0.2)
        
        total_energy_wh = cpu_energy + memory_energy + network_overhead
        total_energy_joules = total_energy_wh * 3600
        
        # Calculate carbon footprint
        carbon_g_co2e = (total_energy_wh * self.grid_intensity) / 1000
        
        return {
            'energy_joules': total_energy_joules,
            'carbon_g_co2e': carbon_g_co2e,
            'duration_seconds': test_duration,
            'cpu_utilization': cpu_utilization,
            'memory_gb': memory_gb,
            'network_efficiency': network_efficiency
        }
    
    def display_dashboard(self):
        """Display real-time dashboard"""
        print("\n" + "="*80)
        print("🌱 NETZERO TESTOPS - REAL-TIME CARBON DASHBOARD 🌱")
        print("="*80)
        
        print(f"📊 Test Execution Summary (Updated: {datetime.now().strftime('%H:%M:%S')})")
        print("-" * 80)
        
        # Display current metrics
        sustainable = self.current_metrics['sustainable']
        wasteful = self.current_metrics['wasteful']
        
        print(f"🟢 Sustainable Framework:")
        print(f"   Energy Consumed: {sustainable['energy']:.2f} J")
        print(f"   Carbon Footprint: {sustainable['carbon']:.6f} g CO₂e")
        print(f"   Tests Executed: {sustainable['tests_run']}")
        
        print(f"\n🔴 Wasteful Framework:")
        print(f"   Energy Consumed: {wasteful['energy']:.2f} J") 
        print(f"   Carbon Footprint: {wasteful['carbon']:.6f} g CO₂e")
        print(f"   Tests Executed: {wasteful['tests_run']}")
        
        # Calculate and display savings
        if wasteful['energy'] > 0:
            energy_savings = ((wasteful['energy'] - sustainable['energy']) / wasteful['energy']) * 100
            carbon_savings = ((wasteful['carbon'] - sustainable['carbon']) / wasteful['carbon']) * 100
            
            print(f"\n💰 SAVINGS ACHIEVED:")
            print(f"   Energy Reduction: {energy_savings:.1f}%")
            print(f"   Carbon Reduction: {carbon_savings:.1f}%")
            print(f"   Energy Saved: {wasteful['energy'] - sustainable['energy']:.2f} J")
            print(f"   Carbon Saved: {(wasteful['carbon'] - sustainable['carbon'])*1000:.3f} mg CO₂e")
        
        print("=" * 80)
    
    def run_simulation(self, duration_minutes=2):
        """Run the dashboard simulation"""
        print("🚀 Starting NetZero TestOps Dashboard Simulation...")
        print(f"⏱️  Running for {duration_minutes} minutes")
        
        start_time = time.time()
        end_time = start_time + (duration_minutes * 60)
        
        test_scenarios = [
            "User Login Test",
            "Product Search Test", 
            "Cart Functionality Test",
            "Checkout Process Test",
            "Payment Integration Test",
            "Order Confirmation Test"
        ]
        
        while time.time() < end_time:
            # Simulate test execution for both frameworks
            test_name = random.choice(test_scenarios)
            test_duration = random.uniform(3, 8)  # 3-8 second tests
            
            print(f"\n🔄 Executing: {test_name}")
            
            # Run on both frameworks
            sustainable_result = self.simulate_test_execution('sustainable', test_duration)
            wasteful_result = self.simulate_test_execution('wasteful', test_duration)
            
            # Update metrics
            self.current_metrics['sustainable']['energy'] += sustainable_result['energy_joules']
            self.current_metrics['sustainable']['carbon'] += sustainable_result['carbon_g_co2e']
            self.current_metrics['sustainable']['tests_run'] += 1
            
            self.current_metrics['wasteful']['energy'] += wasteful_result['energy_joules']
            self.current_metrics['wasteful']['carbon'] += wasteful_result['carbon_g_co2e'] 
            self.current_metrics['wasteful']['tests_run'] += 1
            
            # Display updated dashboard
            self.display_dashboard()
            
            # Wait before next test
            time.sleep(random.uniform(2, 4))
        
        # Save final results
        self.save_simulation_results()
        
        print("\n✅ Dashboard simulation completed!")
        print("📊 Results saved to Reports/simulation_results.json in the project directory")
    
    def save_simulation_results(self):
        """Save simulation results to file"""
        results = {
            'timestamp': datetime.now().isoformat(),
            'simulation_summary': {
                'total_tests_per_framework': self.current_metrics['sustainable']['tests_run'],
                'sustainable_framework': self.current_metrics['sustainable'],
                'wasteful_framework': self.current_metrics['wasteful'],
                'savings': {
                    'energy_reduction_percent': ((self.current_metrics['wasteful']['energy'] - self.current_metrics['sustainable']['energy']) / self.current_metrics['wasteful']['energy']) * 100,
                    'carbon_reduction_percent': ((self.current_metrics['wasteful']['carbon'] - self.current_metrics['sustainable']['carbon']) / self.current_metrics['wasteful']['carbon']) * 100,
                    'energy_saved_joules': self.current_metrics['wasteful']['energy'] - self.current_metrics['sustainable']['energy'],
                    'carbon_saved_g_co2e': self.current_metrics['wasteful']['carbon'] - self.current_metrics['sustainable']['carbon']
                }
            }
        }
        
        # Get the script's directory and create proper path to Reports folder
        script_dir = os.path.dirname(os.path.abspath(__file__))
        project_root = os.path.dirname(script_dir)  # Go up one level from Demo to NetZero TestOps
        reports_dir = os.path.join(project_root, 'Reports')
        
        # Create Reports directory if it doesn't exist
        os.makedirs(reports_dir, exist_ok=True)
        
        # Save the results file
        results_file = os.path.join(reports_dir, 'simulation_results.json')
        with open(results_file, 'w') as f:
            json.dump(results, f, indent=2)
            
        print(f"💾 Results saved to: {results_file}")

def main():
    """Main function to run the dashboard simulation"""
    dashboard = CarbonDashboardSimulator()
    
    print("🌱 Welcome to NetZero TestOps Dashboard Simulator!")
    print("This simulation shows real-time carbon impact comparison")
    print("between sustainable and wasteful testing frameworks.")
    
    try:
        dashboard.run_simulation(duration_minutes=2)
    except KeyboardInterrupt:
        print("\n\n⏹️  Simulation stopped by user")
        dashboard.save_simulation_results()
    except Exception as e:
        print(f"\n❌ Simulation error: {e}")
    
    print("\n🎉 NetZero TestOps Dashboard Demo Complete!")

if __name__ == "__main__":
    main()
