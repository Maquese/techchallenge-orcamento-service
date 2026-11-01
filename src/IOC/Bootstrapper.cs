using Application.Interfaces;
using Infra;
using Infra.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.UseCases.Orcamentos;
using Amazon.DynamoDBv2;
using Amazon;
using Amazon.DynamoDBv2.DataModel;
using System.Diagnostics.CodeAnalysis;
//using Application.Controllers;

namespace IOC;

[ExcludeFromCodeCoverage]
 public static class BootStrapper
    {
        public static void AddAppServices(this IServiceCollection services, IConfiguration configuration)
        {
            //services.AddScoped<AuthService>();

            // var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            //     ?? configuration["ConnectionStrings:DefaultConnection"]
            //     ?? configuration.GetConnectionString("DefaultConnection");

            // if (string.IsNullOrWhiteSpace(connectionString))
            // {
            //     throw new InvalidOperationException("A connection string 'DefaultConnection' must be configured.");
            // }
            
            // // Console.WriteLine($"Using connection string: {connectionString}");
//              string connStr = "Server=localhost;Port=3308;Database=Orcamento;User=appuser;Password=orcamentoservice;";
//             //string connStr = "Server=mysql-ositemestoque;Port=3306;Database=OsItemEstoque;User=appuser;Password=ositemestoqueservice;";
// Console.WriteLine($"Using connection string: {connStr}");
//             services.AddDbContext<EFContext>(options =>
//                 options.UseMySQL(connStr));
//             using var serviceProvider = services.BuildServiceProvider();
//             using var scope = serviceProvider.CreateScope();
//             var dbContext = scope.ServiceProvider.GetRequiredService<EFContext>();
//             dbContext.Database.Migrate();

            var regionName = configuration["AWS:Region"]
            ?? throw new InvalidOperationException("A configuração AWS:Region é obrigatória.");

            services.AddSingleton<IAmazonDynamoDB>(
                _ => new AmazonDynamoDBClient(RegionEndpoint.GetBySystemName(regionName)));


            services.AddScoped<IDynamoDBContext>(sp =>
            new DynamoDBContext(
                sp.GetRequiredService<IAmazonDynamoDB>(),
                new DynamoDBContextConfig
                {
                    DisableFetchingTableMetadata = true
             }));


            services.AddTransient<OrcamentoRepository, OrcamentoRepositoryImp>();

            services.AddScoped<AdicionarOrcamentoHandler>();
            services.AddScoped<AprovarOrcamentoHandler>();
            services.AddScoped<PagarOrcamentoHandler>();
            services.AddScoped<ListarOrcamentoHandler>();
            services.AddScoped<NegarOrcamentoHandler>();
            services.AddScoped<OrcamentoAprovadoHandler>();

        }
    }
