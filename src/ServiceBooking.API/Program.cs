using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using ServiceBooking.API;
using ServiceBooking.Application;
using ServiceBooking.Application.Auth;
using ServiceBooking.Domain.Entities;
using ServiceBooking.Infrastructure;
using ServiceBooking.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    const string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";
    var logPath = context.Configuration["Serilog:FilePath"] ?? "logs/service-booking-.log";
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: outputTemplate)
        .WriteTo.File(
            logPath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            outputTemplate: outputTemplate);
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Configuration.GetValue("Database:AutoMigrate", true))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ServiceBookingDbContext>();
    dbContext.Database.Migrate();
    await SeedAdministrationAsync(scope.ServiceProvider, app.Configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseMiddleware<AuditMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/login", async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "login.html"));
});
app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("HealthCheck");

app.Run();

static async Task SeedAdministrationAsync(IServiceProvider services, IConfiguration configuration)
{
    var dbContext = services.GetRequiredService<ServiceBookingDbContext>();
    var passwordHasher = services.GetRequiredService<IPasswordHasher>();
    const string requiredAdminEmail = "admin@minicrm";
    var defaultEmail = requiredAdminEmail;
    var defaultPassword = configuration["Admin:DefaultPassword"] ?? "Admin12345";
    var normalizedEmail = defaultEmail.Trim().ToLowerInvariant();

    var existingAdmin = await dbContext.AdminUsers.SingleOrDefaultAsync(admin => admin.Email == normalizedEmail);
    if (existingAdmin is null)
    {
        dbContext.AdminUsers.Add(new AdminUser(
            "Platform Admin",
            normalizedEmail,
            passwordHasher.Hash(defaultPassword)));
    }
    else
    {
        existingAdmin.Activate();
    }

    var extraAdmins = await dbContext.AdminUsers
        .Where(admin => admin.Email != normalizedEmail && admin.IsActive)
        .ToArrayAsync();
    foreach (var admin in extraAdmins)
    {
        admin.Deactivate();
    }

    if (!await dbContext.SubscriptionPlans.AnyAsync(plan => plan.Name == "Free"))
    {
        dbContext.SubscriptionPlans.Add(new SubscriptionPlan(
            "Free",
            0m,
            50,
            5,
            "Starter plan for new specialists."));
    }

    if (!await dbContext.SubscriptionPlans.AnyAsync(plan => plan.Name == "Pro"))
    {
        dbContext.SubscriptionPlans.Add(new SubscriptionPlan(
            "Pro",
            1990m,
            0,
            0,
            "Unlimited bookings and services."));
    }

    if (dbContext.ChangeTracker.HasChanges())
    {
        await dbContext.SaveChangesAsync();
    }
}

public partial class Program;
