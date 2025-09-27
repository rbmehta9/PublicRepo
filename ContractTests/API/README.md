# Contract Testing with Pact.NET

ASP.NET Core 9 Web API demonstrating contract testing with clean separation of test and production code.

## Structure

- **ContractTests** - Main Web API 
- **TestProject** - Consumer tests generating Pact contracts
- **ProviderTests** - Provider verification tests

## Key Feature

Provider states endpoint (`/provider-states`) is injected via startup filter in test project only. Production code stays clean.

## Running Tests
