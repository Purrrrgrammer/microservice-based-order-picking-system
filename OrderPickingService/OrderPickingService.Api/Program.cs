using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using OrderPickingService.Infrastructure.Database;
using OrderPickingService.Infrastructure.Database.Migrations;
using OrderPickingService.Infrastructure.ExternalServices;
using OrderPickingService.Infrastructure.Outbox;
using OrderPickingService.Services;

namespace OrderPickingService.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
        services
            .AddDatabase(builder.Configuration)
            .AddDomainServices()
            .AddStorageHttpClient(builder.Configuration)
            .AddValidatorsFromAssembly(typeof(Program).Assembly)
            .AddRabbitMq(builder.Configuration)
            .AddOutbox()
            ;
        
        var app = builder.Build();
        
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            await dbContext.Database.MigrateAsync();  
        }
        
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        app.UseHealthChecks("/health");
        app.UseHealthChecks("/order_picking_service", new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains("order_picking_service")
        });
        
        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        await app.RunAsync();
    }
}