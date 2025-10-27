using ItemsApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PactNet.Infrastructure.Outputters;
using PactNet.Verifier;
using Xunit;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace ItemsApi.Provider.Tests
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
            _host = Program.CreateHostBuilder(new string[0])
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseKestrel();
                    webBuilder.UseUrls("http://127.0.0.1:0"); // Dynamic port
                    webBuilder.UseEnvironment("Test");
                    
                    // Add startup filter to inject provider-states endpoint
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddTransient<IStartupFilter, ProviderStatesStartupFilter>();
                    });
                })
                .Build();

            await _host.StartAsync();

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
                "..", "..", "..", "..", "ConsumerTests", "pacts",
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

    public class ProviderStatesStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                // Add provider-states endpoint before the main pipeline
                app.Map("/provider-states", providerApp =>
                {
                    providerApp.Run(async context =>
                    {
                        if (context.Request.Method == "POST")
                        {
                            var body = await context.Request.ReadFromJsonAsync<ProviderState>();

                            switch (body?.State)
                            {
                                case "items exist":
                                case "the API is available":
                                case "item with ID 1 exists":
                                case "item with ID 6 exists":
                                    context.Response.StatusCode = 200;
                                    context.Response.ContentType = "application/json";
                                    await context.Response.WriteAsync($"{{\"message\":\"Provider state '{body.State}' set up successfully\"}}");
                                    break;
                                case "no item with ID 999 exists":
                                case "item with name 'Sample Item 1' already exists":
                                    context.Response.StatusCode = 200;
                                    context.Response.ContentType = "application/json";
                                    await context.Response.WriteAsync($"{{\"message\":\"Provider state '{body.State}' set up successfully\"}}");
                                    break;
                                default:
                                    context.Response.StatusCode = 400;
                                    context.Response.ContentType = "application/json";
                                    await context.Response.WriteAsync($"{{\"error\":\"Unknown provider state: {body?.State}\"}}");
                                    break;
                            }
                        }
                        else
                        {
                            context.Response.StatusCode = 405; // Method not allowed
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync("{\"error\":\"Method not allowed. Use POST.\"}");
                        }
                    });
                });
                
                next(app); // Execute Program.cs configuration
            };
        }
    }

    public class ProviderState
    {
        public string? State { get; set; }
    }
}