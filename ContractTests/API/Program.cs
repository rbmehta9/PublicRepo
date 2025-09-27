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
                        
                        if (env.IsDevelopment())
                        {
                            app.UseSwagger();
                            app.UseSwaggerUI();
                        }

                        app.UseHttpsRedirection();
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapControllers();
                        });
                    });
                });
    }
}
