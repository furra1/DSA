using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using DSAManager.API.Services;
using DSAManager.Backend.Application.Services;
using DSAManager.Backend.Domain.Entities;
using DSAManager.Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var connectionString = builder.Configuration.GetConnectionString("SqlServer")
    ?? throw new InvalidOperationException("Set ConnectionStrings:SqlServer.");
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(connectionString));

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Set Jwt:Key (at least 32 UTF-8 bytes).");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key must contain at least 32 UTF-8 bytes.");

var issuer = builder.Configuration["Jwt:Issuer"] ?? "DSAManager";
var audience = builder.Configuration["Jwt:Audience"] ?? "DSAManager.Client";
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton(new JwtTokenService(jwtKey, issuer, audience));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = issuer,
        ValidateAudience = true, ValidAudience = audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = ClaimTypes.NameIdentifier, RoleClaimType = ClaimTypes.Role
    });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o => o.AddPolicy("frontend", p =>
    p.SetIsOriginAllowed(origin =>
        origin == "http://localhost:5500" || origin == "http://127.0.0.1:5500" ||
        (builder.Environment.IsDevelopment() && Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
         uri.Scheme == "http" && (uri.Host.StartsWith("192.168.") || uri.Host.StartsWith("10.") || uri.Host.StartsWith("172."))))
     .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
// Additive, non-destructive schema upgrade for task quantity tracking.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.ExecuteSqlRaw("IF COL_LENGTH('dsa.Tasks','ProgressTarget') IS NULL ALTER TABLE [dsa].[Tasks] ADD [ProgressTarget] int NULL;");
    db.Database.ExecuteSqlRaw("IF COL_LENGTH('dsa.Tasks','ProgressCompleted') IS NULL ALTER TABLE [dsa].[Tasks] ADD [ProgressCompleted] int NOT NULL CONSTRAINT [DF_Tasks_ProgressCompleted] DEFAULT (0) WITH VALUES;");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();
