using PactNet;
using PactNet.Matchers;
using Xunit;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using ItemsApi.Models;

namespace ItemsApi.Consumer.Tests
{
    public class PactItemsControllerTests : IDisposable
    {
        private readonly IPactV3 _pact;

        public PactItemsControllerTests()
        {
            //var config = new PactConfig
            //{
            //    PactDir = Path.Combine(Directory.GetCurrentDirectory(), "pacts") // Save pacts to a 'pacts' folder
            //};

            var projectDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".."));
            var pactsDir = Path.Combine(projectDir, "pacts");

            var config = new PactConfig
            {
                PactDir = pactsDir
            };

            _pact = Pact.V3("ItemsApiConsumer", "ItemsApiProvider", config);
        }

        [Fact]
        public async Task GetItems_ContractTest()
        {
            var pactBuilder = _pact.WithHttpInteractions();
            
            pactBuilder
                .UponReceiving("A GET request to retrieve all items")
                .Given("items exist")
                .WithRequest(HttpMethod.Get, "/api/items")
                .WillRespond()
                .WithStatus(200)
                .WithJsonBody(new[]
                {
                    new
                    {
                        Id = Match.Type(1),
                        Name = Match.Type("Sample Item 1")
                    }
                });

            await pactBuilder.VerifyAsync(async ctx =>
            {
                var httpClient = new HttpClient();
                // Remove the leading slash from "api/items"
                var response = await httpClient.GetAsync($"{ctx.MockServerUri}api/items");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var items = JsonSerializer.Deserialize<Item[]>(content);

                Assert.NotNull(items);
                Assert.NotEmpty(items);
                Assert.All(items, item =>
                {
                    Assert.True(item.Id > 0);
                    Assert.False(string.IsNullOrWhiteSpace(item.Name));
                });
            });
        }

        [Fact]
        public async Task PostItem_ContractTest()
        {
            var pactBuilder = _pact.WithHttpInteractions();
            
            pactBuilder
                .UponReceiving("A POST request to create a new item")
                .Given("the API is available")
                .WithRequest(HttpMethod.Post, "/api/items")
                .WithJsonBody(new { name = "New Item" })
                .WillRespond()
                .WithStatus(201)
                .WithJsonBody(new
                {
                    Id = Match.Type(3),
                    Name = Match.Type("New Item")
                });

            await pactBuilder.VerifyAsync(async ctx =>
            {
                var httpClient = new HttpClient();
                var postContent = new StringContent(
                    JsonSerializer.Serialize(new { name = "New Item" }),
                    Encoding.UTF8,
                    "application/json"
                );

                // Remove the leading slash from "api/items"
                var response = await httpClient.PostAsync($"{ctx.MockServerUri}api/items", postContent);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var item = JsonSerializer.Deserialize<Item>(content);

                Assert.NotNull(item);
                Assert.True(item.Id > 0);
                Assert.Equal("New Item", item.Name);
            });
        }

        //private record Item(int Id, string Name);

        public void Dispose()
        {
            
        }
    }
}