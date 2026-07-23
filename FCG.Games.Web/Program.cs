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

var b = WebApplication.CreateBuilder(args);

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