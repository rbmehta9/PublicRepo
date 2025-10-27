namespace ItemsApi
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
                        
                        // Enable Swagger for Development and non-Production environments
                        if (env.IsDevelopment() || !env.IsProduction())
                        {
                            app.UseSwagger();
                            app.UseSwaggerUI();
                        }

                        // Only use HTTPS redirection in production
                        if (env.IsProduction())
                        {
                            app.UseHttpsRedirection();
                        }
                        
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
                });
    }
}
