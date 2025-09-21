namespace ContractTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        // WebApplicationFactory looks for this method specifically
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddControllers()
                            .AddJsonOptions(options =>
                            {
                                // Use PascalCase for JSON serialization to match your pact
                                options.JsonSerializerOptions.PropertyNamingPolicy = null;
                            });
                        services.AddEndpointsApiExplorer();
                        services.AddSwaggerGen();
                    })
                    .Configure((context, app) =>
                    {
                        var env = context.HostingEnvironment;
                        
                        if (env.IsDevelopment())
                        {
                            app.UseSwagger();
                            app.UseSwaggerUI();
                        }

                        if (!env.IsEnvironment("Test"))
                        {
                            app.UseHttpsRedirection();
                        }

                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapPost("/provider-states", async (HttpContext context) =>
                            {
                                var body = await context.Request.ReadFromJsonAsync<ProviderState>();

                                if (body?.State == "items exist")
                                {
                                    context.Response.StatusCode = 200;
                                    await context.Response.WriteAsync("Provider state 'items exist' set up successfully");
                                }
                                else if (body?.State == "the API is available")
                                {
                                    context.Response.StatusCode = 200;
                                    await context.Response.WriteAsync("Provider state 'the API is available' set up successfully");
                                }
                                else
                                {
                                    context.Response.StatusCode = 400;
                                    await context.Response.WriteAsync($"Unknown provider state: {body?.State}");
                                }
                            });

                            endpoints.MapControllers();
                        });
                    });
                });
    }

    public class ProviderState
    {
        public string? State { get; set; }
    }
}
