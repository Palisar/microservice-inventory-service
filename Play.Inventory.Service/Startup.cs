using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Play.Common.MassTransit;
using Play.Common.MongoDB;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Entities;
using Polly;
using Polly.Timeout;

namespace Play.Inventory.Service
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddMongo()
                .AddMongoRepository<InventoryItem>("inventoryItems")
                .AddMongoRepository<CatalogItem>("catalogItems")
                .AddMassTransitWithRabbitMq();

            AddCatalogClient(services);

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Inventory.Service", Version = "v1" });
            });
        }
        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Play.Inventory.Service v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }


        private static void AddCatalogClient(IServiceCollection services)
        {
            Random jitterer = new Random();

            services.AddHttpClient<CatalogClient>(client => { client.BaseAddress = new Uri("https://localhost:44317"); })
                .AddTransientHttpErrorPolicy(builder => builder.Or<TimeoutRejectedException>().WaitAndRetryAsync(
                    5,
                    retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                        + TimeSpan.FromMilliseconds((jitterer.Next(0, 1000))), // add this before your Policy handler,
                    onRetry: (outcome, timespan, retryAttempt) =>
                    {
                        //in here shouldl not be used in production this is just a hack to get logger workings
                        var serviceProvider = services.BuildServiceProvider();
                        serviceProvider.GetService<ILogger<CatalogClient>>()?
                            .LogWarning($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}");
                    }
                ))

                //below is the circuit breaker pattern, this will cause the circuit to break when there more 
                .AddTransientHttpErrorPolicy(builder => builder.Or<TimeoutRejectedException>().CircuitBreakerAsync(
                    3,
                    TimeSpan.FromSeconds(15),
                    onBreak: (outcome, timespan) =>
                    {
                        var serviceProvider = services.BuildServiceProvider();
                        serviceProvider.GetService<ILogger<CatalogClient>>()?
                            .LogWarning($"Opening the circuit for {timespan.TotalSeconds} seconds.");
                    },
                    onReset: () =>
                    {
                        var serviceProvider = services.BuildServiceProvider();
                        serviceProvider.GetService<ILogger<CatalogClient>>()?
                            .LogWarning($"Closing the circuit....");
                    }
                ))

                //Any time we invoke anything on this Uri we will wait 1 second before giving up
                .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
        }
    }
}
