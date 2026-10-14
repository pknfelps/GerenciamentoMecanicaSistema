using DependencyInjection;
using GerenciamentoMecanicaSistema.Middleware;
using GerenciamentoMecanicaSistema.OpenApi;
using Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Reflection;
using System.Text;

namespace GerenciamentoMecanicaSistema
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var generatingDocument = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

            builder.Services.AddControllers();
            builder.Services.AddOpenApi(options =>
            {
                options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
                options.AddDocumentTransformer<ApiDocumentTransformer>();
                options.AddOperationTransformer<BearerSecurityOperationTransformer>();
            });

            var usesAwsConfiguration = !generatingDocument && await ConfigureRuntimeAsync(builder);
            var app = builder.Build();

            if (!generatingDocument)
                app.UseMiddleware<ExceptionHandlingMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "mechanic api" ));
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

        private static async Task<bool> ConfigureRuntimeAsync(WebApplicationBuilder builder)
        {
            var runtimeConfiguration = await AwsRuntimeConfigurationLoader.LoadIfConfiguredAsync(
                builder.Configuration,
                Path.Combine(AppContext.BaseDirectory, "Certificates", "rds-ca.pem"));
            
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

            builder.Services
                .AddApplication()
                .AddInfrastructure(builder.Configuration, usesAwsConfiguration)
                .AddPersistence(builder.Configuration);

            return usesAwsConfiguration;
        }
    }
}
