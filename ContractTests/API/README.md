# Contract Testing with Pact.NET

ASP.NET Core 9 Web API demonstrating contract testing with clean separation of test and production code.

## Structure

- **ContractTests** - Main Web API 
- **TestProject** - Consumer tests generating Pact contracts
- **ProviderTests** - Provider verification tests

## Key Feature

Provider states endpoint (`/provider-states`) is injected via startup filter in test project only. Production code stays clean.

## Ideal roles & steps (consumer / provider — ideal world)

Consumer:
- Write contract tests that define the requests you make and the responses you expect (run against a mock provider).
- Run tests to generate a pact JSON and publish it to a Pact Broker (or share the pact file).
- Maintain and run these tests in consumer CI; update and republish the pact when your needs change.

Provider:
- Pull consumer pacts from the Broker (or receive pact files).
- Implement provider-state handlers and any test hooks required to prepare the provider for each interaction.
- Run pact verification against the running provider in provider CI; fix mismatches and re-run.
- Publish verification results to the Broker so consumers and teams can see compatibility.

Outcome: consumers own the contract they need; providers prove they fulfil it — enabling independent, safe evolution.

## Running Tests

```text
# Consumer tests (generate pact)
cd TestProject
dotnet test

# Provider verification (verify against generated pact)
cd ProviderTests
dotnet test
```