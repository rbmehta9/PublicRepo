using ContractTests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PactNet.Infrastructure.Outputters;
using PactNet.Verifier;
using Xunit;
using Xunit.Abstractions;

namespace ProviderTests
{
    public class ItemsProviderTests : IDisposable
    {
        private readonly ITestOutputHelper _outputHelper;
        private IHost? _host;
        private string? _serverUrl;

        public ItemsProviderTests(ITestOutputHelper output)
        {
            _outputHelper = output;
            StartServer().Wait();
        }

        private async Task StartServer()
        {
            // Use your Program.CreateHostBuilder directly
            _host = Program.CreateHostBuilder(new string[0])
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseKestrel();
                    webBuilder.UseUrls("http://127.0.0.1:0"); // Dynamic port
                    webBuilder.UseEnvironment("Test");
                })
                .Build();

            await _host.StartAsync();

            // Get the actual server address
            var server = _host.Services.GetRequiredService<IServer>();
            var addressFeature = server.Features.Get<IServerAddressesFeature>();
            _serverUrl = addressFeature?.Addresses?.FirstOrDefault();

            _outputHelper.WriteLine($"Server started at: {_serverUrl}");

            if (string.IsNullOrEmpty(_serverUrl))
            {
                throw new InvalidOperationException("Could not determine server URL");
            }
        }

        [Fact]
        public async Task EnsureProviderApiHonoursPactWithConsumer()
        {
            if (string.IsNullOrEmpty(_serverUrl))
            {
                throw new InvalidOperationException("Server URL not available");
            }

            // Test the server
            using var httpClient = new HttpClient();
            var testResponse = await httpClient.GetAsync($"{_serverUrl}/api/items");
            _outputHelper.WriteLine($"Server test: {testResponse.StatusCode}");

            if (testResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var content = await testResponse.Content.ReadAsStringAsync();
                _outputHelper.WriteLine($"Error response: {content}");
                throw new InvalidOperationException($"Server test failed: {testResponse.StatusCode}");
            }

            var pactPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..", "..", "..", "..", "TestProject", "pacts",
                "ItemsApiConsumer-ItemsApiProvider.json");

            if (!File.Exists(pactPath))
            {
                throw new FileNotFoundException($"Pact file not found at: {pactPath}");
            }

            var pactVerifier = new PactVerifier("ItemsApiProvider", new PactVerifierConfig
            {
                Outputters = new List<IOutput> { new XunitOutput(_outputHelper) }
            });

            pactVerifier
                .WithHttpEndpoint(new Uri(_serverUrl))
                .WithFileSource(new FileInfo(pactPath))
                .WithProviderStateUrl(new Uri($"{_serverUrl}/provider-states"))
                .Verify();
        }

        public void Dispose()
        {
            _host?.StopAsync().Wait();
            _host?.Dispose();
        }
    }
}