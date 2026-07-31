using FCG.Games.Infrastructure.Events;
using FCG.Games.Application.Abstractions.Events;
using System.Diagnostics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text;
using FCG.Games.Application.Abstractions;
using FCG.Games.Application.Games;
using FCG.Games.Infrastructure.Http;
using FCG.Games.Infrastructure.Persistence;
using FCG.Games.Web.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Elastic.Clients.Elasticsearch;
using FCG.Games.Infrastructure.Search;

var b = WebApplication.CreateBuilder(args);

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;
const string serviceName = "FCG.GamesService";
var otlpEndpoint = b.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";

b.Logging.Configure(options =>
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId);

b.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.SetResourceBuilder(
        ResourceBuilder.CreateDefault().AddService(serviceName));
    options.AddOtlpExporter(exporter =>
    {
        exporter.Endpoint = new Uri(otlpEndpoint);
        exporter.Protocol = OtlpExportProtocol.Grpc;
    });
});

b.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = context =>
                    !context.Request.Path.StartsWithSegments("/health") &&
                    !context.Request.Path.StartsWithSegments("/swagger");
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(exporter =>
            {
                exporter.Endpoint = new Uri(otlpEndpoint);
                exporter.Protocol = OtlpExportProtocol.Grpc;
            });
    });


b.Services.AddControllers();
b.Services.AddEndpointsApiExplorer();

b.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FCG.Games.Web",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

b.Services.AddDbContext<GamesDbContext>(o =>
    o.UseSqlServer(
        b.Configuration.GetConnectionString("DefaultConnection")));

b.Services.AddScoped<IGamesRepository, GamesRepository>();
b.Services.AddScoped<IEventStore, EfEventStore>();
b.Services.AddScoped<GameService>();

b.Services.AddHttpClient<IPaymentsClient, PaymentsClient>(c =>
{
    c.BaseAddress = new Uri(
        b.Configuration["Services:Payments"]!);
});

var key = Encoding.UTF8.GetBytes(
    b.Configuration["Jwt:Key"]!);

b.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = b.Configuration["Jwt:Issuer"],
            ValidAudience = b.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

b.Services.AddAuthorization();


var elasticUrl = b.Configuration["Elasticsearch:Url"]
    ?? throw new InvalidOperationException(
        "Elasticsearch:Url n�o configurada.");

b.Services.AddSingleton(
    new ElasticsearchClient(new Uri(elasticUrl)));

b.Services.AddScoped<
    IGameSearchRepository,
    ElasticsearchGameSearchRepository>();

var app = b.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var s = app.Services.CreateScope())
{
    var db = s.ServiceProvider
        .GetRequiredService<GamesDbContext>();

    db.Database.Migrate();
}

app.Run();