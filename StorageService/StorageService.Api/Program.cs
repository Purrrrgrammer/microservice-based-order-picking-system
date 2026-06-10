using MassTransit;
using Microsoft.EntityFrameworkCore;
using StorageService.Api.Application.Interfaces;
using StorageService.Api.Application.Services;
using StorageService.Api.Consumers;
using StorageService.Api.Helpers;
using StorageService.Api.Infrastructure.Data;
using StorageService.Api.Infrastructure.Interfaces;
using StorageService.Api.Infrastructure.Repositories;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Database
var pgHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
var pgPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
var pgUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
var pgPass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
var pgDb = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "storagedb";

var conn = $"Host={pgHost};Port={pgPort};Username={pgUser};Password={pgPass};Database={pgDb}";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options
            .UseSqlite("Data Source=products-service-db.db")
            .UseSeeding((contextToSeed, _) => ApplicationDbContext.SeedData(contextToSeed))
            .UseAsyncSeeding((contextToSeed, _, _) =>
            {
                ApplicationDbContext.SeedData(contextToSeed);
                return Task.CompletedTask;
            });
    }
    else
    {
        options.UseNpgsql(builder.Configuration["CONNECTION_STRING"] 
                          ?? throw new InvalidProgramException("No connection for data base"));
        
        options.UseSeeding((contextToSeed, _) => ApplicationDbContext.SeedData(contextToSeed));
        options.UseAsyncSeeding((contextToSeed, _, _) =>
        {
            ApplicationDbContext.SeedData(contextToSeed);
            return Task.CompletedTask;
        });
    }
});

builder.Services.AddMassTransit(x => {
    x.AddConsumer<OrderCompleteConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        RabbitConfigurator.ConfigureRmq(cfg, builder.Configuration);
        RabbitConfigurator.RegisterEndPoints(cfg, context, builder.Configuration);
    });
});

builder.Services.AddCors(options =>
    options.AddPolicy("myCors", policy =>
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin()));

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ISectionRepository, SectionRepository>();
builder.Services.AddScoped<IManufacturerRepository, ManufacturerRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

builder.Services.AddScoped<ISectionService, SectionService>();
builder.Services.AddScoped<IManufacturerService, ManufacturerService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReservationItemsService, ReservationItemsService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    using var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (app.Environment.IsDevelopment())
        context.Database.EnsureDeleted();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(policy =>
    policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        //.AllowAnyOrigin()
        .SetIsOriginAllowed(x => true)
        .AllowCredentials());

if (!app.Environment.IsDevelopment() || !app.Environment.IsStaging())
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Task.FromResult<IResult>(TypedResults.Text("Storage service working")))
    .WithName("CheckHealth")
    .WithTags("Health")
    .Produces(200)
    .AllowAnonymous();

app.Run();

public partial class Program { }