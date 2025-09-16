# NonGreenTestFramework

This is a purposely inefficient, non-eco-friendly .NET 6 test automation framework for API testing (Amazon.in example).

## Characteristics
- Redundant test execution (loops, repeated resource usage)
- No resource cleanup or optimization
- Inefficient config and HTTP client usage

## Structure
- `Tests/` - Test cases
- `Utilities/` - API helpers
- `Config/` - Config files

## Usage
1. Restore NuGet packages
2. Run tests: `dotnet test`

---
This framework is for demonstration and should NOT be used in production.
