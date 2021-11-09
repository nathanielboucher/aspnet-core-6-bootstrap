using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Company.Product.WebApi.Api.ExceptionHandlers;
using Company.Product.WebApi.Api.Filters.Validation;
using Company.Product.WebApi.Api.Logging;
using Company.Product.WebApi.Api.Services;
using Company.Product.WebApi.Api.Swashbuckle;
using Company.Product.WebApi.Common;
using Company.Product.WebApi.Data;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Serilog;
using Swashbuckle.AspNetCore.Filters;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;

#pragma warning disable IDE0058

var apiAssembly = Assembly.GetExecutingAssembly();
var builder = WebApplication.CreateBuilder(args);

/*
 * Logging
 */

builder.WebHost.UseSerilog(
    (_, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.With<UtcTimestampEnricher>(),
    true);

/*
 * Caching
 */

builder.Services.AddMemoryCache();

/*
 * Web host configuration
 */

// No web server advertising allowed!
builder.WebHost.ConfigureKestrel(a => a.AddServerHeader = false);

/*
 * Registrations
 */

// General-purpose

builder
    .Services
    .AddSingleton<IClock>(SystemClock.Instance)
    .AddScoped<IClockSnapshot, ClockSnapshot>()
    .AddSingleton<IGuidFactory, GuidFactory>();

// Business logic services

// Automatically add all services in the Services namespace that have a matching concrete/interface type
builder.Services.Scan(
    selector => selector
        .FromAssemblyDependencies(apiAssembly)
        .AddClasses(
            filter => filter
                .InNamespaceOf<ServicesRootNamespace>()
                .Where(type => type.Name.EndsWith("Service", StringComparison.Ordinal)))
        .AsMatchingInterface()
        .WithScopedLifetime());

/*
 * Entity Framework Core
 */

const string connectionStringAlias = "company_product";
var connectionString = builder.Configuration.GetConnectionString(connectionStringAlias);

if (string.IsNullOrWhiteSpace(connectionString))
{
    ThrowInvalidOperationException(
        $@"Connection string alias ""{connectionStringAlias}"" not found. Use ""dotnet user-secrets"" against this project to store the connection string securely.");
}

builder.Services.AddDbContextPool<DatabaseContext>(
    options => options
        .UseNpgsql(connectionString, optionsBuilder => optionsBuilder.UseNodaTime())
        .UseSnakeCaseNamingConvention());

/*
 * ASP.NET
 */

// System.Text.Json

// JSON serializer options need to be available outside of an inside controllers

static void ConfigureJsonSerializerOptions(JsonSerializerOptions options)
{
    options.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    options.PropertyNameCaseInsensitive = true;
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.WriteIndented = true;
}

builder.Services.Configure<JsonOptions>(options => ConfigureJsonSerializerOptions(options.SerializerOptions));

// Fluent validation

builder.Services.AddFluentValidation(
    options =>
    {
        options.RegisterValidatorsFromAssembly(apiAssembly);
        options.DisableDataAnnotationsValidation = true;
    });

// Custom 400 responses
builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

// Controllers

builder
    .Services
    .AddControllers(options => options.Filters.Add<ValidationFailureFilter>())
    .AddJsonOptions(options => ConfigureJsonSerializerOptions(options.JsonSerializerOptions))
    .ConfigureApiBehaviorOptions(options => options.SuppressMapClientErrors = true);

// Swagger

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerExamplesFromAssemblies(apiAssembly);
builder.Services.AddSwaggerGen(
    options =>
    {
        options.EnableAnnotations();
        options.ExampleFilters();
        options.SchemaFilter<NullableSchemaFilter>();
        options.SchemaFilter<TitleFilter>();
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Company Product Web API", Version = "v1" });
    });

/*
 * App
 */

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(
        options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Company Product Web API");
            options.DefaultModelExpandDepth(-1);
        });
}
else
{
    app.UseHsts();
}

// Custom 500 responses
app.UseExceptionHandler(new ExceptionHandlerOptions { ExceptionHandler = UnhandledExceptionHandler.Handle });

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
