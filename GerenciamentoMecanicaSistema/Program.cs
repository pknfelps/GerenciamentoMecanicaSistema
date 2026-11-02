using Infrastructure.Observability;
using DependencyInjection;
using GerenciamentoMecanicaSistema.Middleware;
using GerenciamentoMecanicaSistema.OpenApi;
using GerenciamentoMecanicaSistema.Observability;
using Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.Extensions.Configuration.Json;
using System.Reflection;
using System.Text;

namespace GerenciamentoMecanicaSistema
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var generatingDocument = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
            try
            {
                await RunAsync(args, generatingDocument);
                return 0;
            }
            catch (Exception exception) when (!generatingDocument)
            {
                Console.Error.WriteLine("Falha na inicialização ou execução da API. ExceptionType: {0}", exception.GetType().FullName);
                return 1;
            }
        }

        private static async Task RunAsync(string[] args, bool generatingDocument)
        {
            var builder = WebApplication.CreateBuilder(args);
            if (!generatingDocument)
            {
                var observability = new JsonConfigurationSource
                {
                    Path = Path.Combine(AppContext.BaseDirectory, "Observability", "observability.json"),
                    Optional = true,
                    ReloadOnChange = false
                };
                observability.ResolveFileProvider();
                builder.Configuration.Sources.Insert(0, observability);
            }

            builder.Services.AddControllers();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddOpenApi(options =>
            {
                options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
                options.AddDocumentTransformer<ApiDocumentTransformer>();
                options.AddSchemaTransformer<BudgetRequestSchemaTransformer>();
                options.AddOperationTransformer<BearerSecurityOperationTransformer>();
            });

            using var metrics = generatingDocument ? null : ApiMetricProvider.Create(builder);
            var usesAwsConfiguration = !generatingDocument && await ConfigureRuntimeAsync(builder, metrics?.Integrations);
            if (!generatingDocument)
                builder.AddApiObservability(metrics);
            var app = builder.Build();

            if (!generatingDocument)
                app.UseMiddleware<ExceptionHandlingMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "mechanic api"));
            }

            if (!generatingDocument)
            {
                // Cloud traffic reaches this pod over HTTP; public TLS belongs to the Gateway.
                if (!usesAwsConfiguration)
                    app.UseHttpsRedirection();

                app.UseAuthentication();
                app.UseAuthorization();
            }

            app.MapControllers();

            await app.RunAsync();
        }

        private static async Task<bool> ConfigureRuntimeAsync(WebApplicationBuilder builder, IntegrationMetrics? metrics)
        {
            var runtimeConfiguration = await AwsRuntimeConfigurationLoader.LoadIfConfiguredAsync(
                builder.Configuration,
                Path.Combine(AppContext.BaseDirectory, "Certificates", "rds-ca.pem"), metrics: metrics);

            var usesAwsConfiguration = runtimeConfiguration is not null;
            if (usesAwsConfiguration)
                builder.Configuration.AddInMemoryCollection(runtimeConfiguration);

            var jwtIssuer = builder.Configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException("JWT issuer not configured.");
            var jwtAudience = builder.Configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException("JWT audience not configured.");
            var jwtKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT signing key not configured.");

            if (jwtKey.Length < 32)
                throw new InvalidOperationException("JWT signing key must contain at least 32 characters.");

            builder.Services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services
                .AddApplication()
                .AddInfrastructure(builder.Configuration, usesAwsConfiguration, provider => provider.GetRequiredService<IHttpContextAccessor>().HttpContext?.RequestAborted ?? default)
                .AddPersistence(builder.Configuration);


            return usesAwsConfiguration;
        }
    }
}
