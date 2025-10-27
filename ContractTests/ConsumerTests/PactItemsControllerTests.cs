using PactNet;
using PactNet.Matchers;
using Xunit;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using ItemsApi.Models;
using System.Net;

namespace ItemsApi.Consumer.Tests
{
    public class PactItemsControllerTests : IDisposable
    {
        private readonly IPactV3 _pact;

        public PactItemsControllerTests()
        {
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
                .WithJsonBody(Match.MinType(
                    new
                    {
                        Id = Match.Type(1),
                        Name = Match.Type("Sample Item 1")
                    },
                 1));

            await pactBuilder.VerifyAsync(async ctx =>
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"{ctx.MockServerUri}api/items");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var items = JsonSerializer.Deserialize<Item[]>(content);

                Assert.NotNull(items);
                Assert.All(items, item =>
                {
                    Assert.True(item.Id > 0);
                    Assert.False(string.IsNullOrWhiteSpace(item.Name));
                });
            });
        }

        [Fact]
        public async Task GetItemById_Success_ContractTest()
        {
            var pactBuilder = _pact.WithHttpInteractions();

            pactBuilder
                .UponReceiving("A GET request to retrieve a specific item by ID")
                .Given("item with ID 1 exists")
                .WithRequest(HttpMethod.Get, "/api/items/1")
                .WillRespond()
                .WithStatus(200)
                .WithJsonBody(new
                {
                    Id = Match.Type(1),
                    Name = Match.Type("Sample Item 1")
                });

            await pactBuilder.VerifyAsync(async ctx =>
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"{ctx.MockServerUri}api/items/1");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var item = JsonSerializer.Deserialize<Item>(content);

                Assert.NotNull(item);
                Assert.Equal(1, item.Id);
                Assert.False(string.IsNullOrWhiteSpace(item.Name));
            });
        }

        [Fact]
        public async Task GetItemById_NotFound_ContractTest()
        {
            var pactBuilder = _pact.WithHttpInteractions();

            pactBuilder
                .UponReceiving("A GET request for a non-existent item")
                .Given("no item with ID 999 exists")
                .WithRequest(HttpMethod.Get, "/api/items/999")
                .WillRespond()
                .WithStatus(404)
                .WithJsonBody(new { error = Match.Type("Item with ID 999 not found.") });

            await pactBuilder.VerifyAsync(async ctx =>
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"{ctx.MockServerUri}api/items/999");
                
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            });
        }

        [Fact]
        public async Task GetItemById_BadRequest_ContractTest()
        {
            var pactBuilder = _pact.WithHttpInteractions();

            pactBuilder
                .UponReceiving("A GET request with invalid item ID")
                .Given("the API is available")
                .WithRequest(HttpMethod.Get, "/api/items/0")
                .WillRespond()
                .WithStatus(400)
                .WithJsonBody(new { error = Match.Type("Invalid item ID.") });

            await pactBuilder.VerifyAsync(async ctx =>
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"{ctx.MockServerUri}api/items/0");
                
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            });
        }

        [Fact]
        public async Task PostItem_Success_ContractTest()
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

                var response = await httpClient.PostAsync($"{ctx.MockServerUri}api/items", postContent);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var item = JsonSerializer.Deserialize<Item>(content);

                Assert.NotNull(item);
                Assert.True(item.Id > 0);
                Assert.Equal("New Item", item.Name);
            });
        }

        [Fact]
        public async Task PostItem_BadRequest_EmptyName_ContractTest()
        {
            var pactBuilder = _pact.WithHttpInteractions();
            
            pactBuilder
                .UponReceiving("A POST request with empty item name")
                .Given("the API is available")
                .WithRequest(HttpMethod.Post, "/api/items")
                .WithJsonBody(new { name = "" })
                .WillRespond()
                .WithStatus(400)
                .WithJsonBody(new { error = Match.Type("Item name cannot be empty.") });

            await pactBuilder.VerifyAsync(async ctx =>
            {
                var httpClient = new HttpClient();
                var postContent = new StringContent(
                    JsonSerializer.Serialize(new { name = "" }),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.PostAsync($"{ctx.MockServerUri}api/items", postContent);
                
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            });
        }

        [Fact]
        public async Task PostItem_Conflict_DuplicateName_ContractTest()
        {
            var pactBuilder = _pact.WithHttpInteractions();
            
            pactBuilder
                .UponReceiving("A POST request with duplicate item name")
                .Given("item with name 'Sample Item 1' already exists")
                .WithRequest(HttpMethod.Post, "/api/items")
                .WithJsonBody(new { name = "Sample Item 1" })
                .WillRespond()
                .WithStatus(409)
                .WithJsonBody(new { error = Match.Type("Item with this name already exists.") });

            await pactBuilder.VerifyAsync(async ctx =>
            {
                var httpClient = new HttpClient();
                var postContent = new StringContent(
                    JsonSerializer.Serialize(new { name = "Sample Item 1" }),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.PostAsync($"{ctx.MockServerUri}api/items", postContent);
                
                Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            });
        }

        [Fact]
        public async Task CreateItemWorkflow_Success_ContractTest()
        {
            // Multi-interaction scenario: Check existing items, create new one, verify by ID
            var pactBuilder = _pact.WithHttpInteractions();

            // Step 1: Get existing items
            pactBuilder
                .UponReceiving("A GET request to check existing items before creation")
                .Given("items exist")
                .WithRequest(HttpMethod.Get, "/api/items")
                .WillRespond()
                .WithStatus(200)
                .WithJsonBody(Match.MinType(
                    new
                    {
                        Id = Match.Type(1),
                        Name = Match.Type("Sample Item 1")
                    },
                    1));

            // Step 2: Create new item
            pactBuilder
                .UponReceiving("A POST request to create a workflow item")
                .Given("the API is available")
                .WithRequest(HttpMethod.Post, "/api/items")
                .WithJsonBody(new { name = "Workflow Item" })
                .WillRespond()
                .WithStatus(201)
                .WithJsonBody(new
                {
                    Id = Match.Type(6),
                    Name = Match.Type("Workflow Item")
                });

            // Step 3: Verify created item by ID
            pactBuilder
                .UponReceiving("A GET request to verify the newly created item")
                .Given("item with ID 6 exists")
                .WithRequest(HttpMethod.Get, "/api/items/6")
                .WillRespond()
                .WithStatus(200)
                .WithJsonBody(new
                {
                    Id = Match.Type(6),
                    Name = Match.Type("Workflow Item")
                });

            await pactBuilder.VerifyAsync(async ctx =>
            {
                var httpClient = new HttpClient();

                // Step 1: Check existing items
                var getResponse = await httpClient.GetAsync($"{ctx.MockServerUri}api/items");
                getResponse.EnsureSuccessStatusCode();

                // Step 2: Create new item
                var postContent = new StringContent(
                    JsonSerializer.Serialize(new { name = "Workflow Item" }),
                    Encoding.UTF8,
                    "application/json"
                );
                var createResponse = await httpClient.PostAsync($"{ctx.MockServerUri}api/items", postContent);
                createResponse.EnsureSuccessStatusCode();

                var createdContent = await createResponse.Content.ReadAsStringAsync();
                var createdItem = JsonSerializer.Deserialize<Item>(createdContent);

                // Step 3: Verify by getting the specific item
                var verifyResponse = await httpClient.GetAsync($"{ctx.MockServerUri}api/items/{createdItem.Id}");
                verifyResponse.EnsureSuccessStatusCode();

                var verifiedContent = await verifyResponse.Content.ReadAsStringAsync();
                var verifiedItem = JsonSerializer.Deserialize<Item>(verifiedContent);
                Assert.NotNull(verifiedItem);
                Assert.Equal(createdItem.Id, verifiedItem.Id);
            });
        }

        [Fact]
        public async Task ValidationAndLookupWorkflow_ContractTest()
        {
            // Multi-interaction scenario: Try invalid, get error, lookup existing, then create valid
            var pactBuilder = _pact.WithHttpInteractions();

            // Step 1: Try invalid creation
            pactBuilder
                .UponReceiving("A POST request with empty name for validation")
                .Given("the API is available")
                .WithRequest(HttpMethod.Post, "/api/items")
                .WithJsonBody(new { name = "" })
                .WillRespond()
                .WithStatus(400)
                .WithJsonBody(new { error = Match.Type("Item name cannot be empty.") });

            // Step 2: Look up existing items to see what's there
            pactBuilder
                .UponReceiving("A GET request to check existing items after validation error")
                .Given("items exist")
                .WithRequest(HttpMethod.Get, "/api/items")
                .WillRespond()
                .WithStatus(200)
                .WithJsonBody(Match.MinType(
                    new
                    {
                        Id = Match.Type(1),
                        Name = Match.Type("Sample Item 1")
                    },
                    1));

            // Step 3: Look up specific item
            pactBuilder
                .UponReceiving("A GET request to check specific existing item")
                .Given("item with ID 1 exists")
                .WithRequest(HttpMethod.Get, "/api/items/1")
                .WillRespond()
                .WithStatus(200)
                .WithJsonBody(new
                {
                    Id = Match.Type(1),
                    Name = Match.Type("Sample Item 1")
                });

            // Step 4: Create valid item
            pactBuilder
                .UponReceiving("A POST request with valid data after validation")
                .Given("the API is available")
                .WithRequest(HttpMethod.Post, "/api/items")
                .WithJsonBody(new { name = "Valid Item" })
                .WillRespond()
                .WithStatus(201)
                .WithJsonBody(new
                {
                    Id = Match.Type(6),
                    Name = Match.Type("Valid Item")
                });

            await pactBuilder.VerifyAsync(async ctx =>
            {
                var httpClient = new HttpClient();

                // Step 1: Try invalid creation
                var invalidContent = new StringContent(
                    JsonSerializer.Serialize(new { name = "" }),
                    Encoding.UTF8,
                    "application/json"
                );
                var invalidResponse = await httpClient.PostAsync($"{ctx.MockServerUri}api/items", invalidContent);
                Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

                // Step 2: Look up existing items
                var getAllResponse = await httpClient.GetAsync($"{ctx.MockServerUri}api/items");
                getAllResponse.EnsureSuccessStatusCode();

                // Step 3: Look up specific item
                var getSpecificResponse = await httpClient.GetAsync($"{ctx.MockServerUri}api/items/1");
                getSpecificResponse.EnsureSuccessStatusCode();

                // Step 4: Create valid item
                var validContent = new StringContent(
                    JsonSerializer.Serialize(new { name = "Valid Item" }),
                    Encoding.UTF8,
                    "application/json"
                );
                var validResponse = await httpClient.PostAsync($"{ctx.MockServerUri}api/items", validContent);
                validResponse.EnsureSuccessStatusCode();
            });
        }

        public void Dispose()
        {
            
        }
    }
}