using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Services;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database (SQL Server via EF Core, code-first) ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

// ---------- Azure Blob Storage (Part 3) ----------
builder.Services.Configure<AzureBlobOptions>(builder.Configuration.GetSection("AzureBlob"));
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

// ---------- Session-based authentication ----------
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Api.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(60);
});

// ---------- Controllers / Swagger ----------
builder.Services.AddControllers(options => options.Filters.Add<StorageExceptionFilter>())
    .ConfigureApiBehaviorOptions(options =>
    {
        // Return validation problems in the same { "message": "..." } shape as every other error.
        options.InvalidModelStateResponseFactory = context =>
        {
            var messages = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "One of the values supplied is invalid." : e.ErrorMessage)
                .Distinct();
            return new BadRequestObjectResult(new ApiError(string.Join(" ", messages)));
        };
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---------- Create the database (and seed demo data) ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // When running in Docker the SQL Server container can take a while to accept connections, so retry.
    const int maxAttempts = 15;
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            // Code-first: creates the database and tables from the entity classes.
            // (Swap for db.Database.Migrate() if you add EF migrations.)
            await db.Database.EnsureCreatedAsync();

            if (app.Configuration.GetValue("Seed:DemoData", false))
                await DbSeeder.SeedAsync(db);
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning("Database not ready yet (attempt {Attempt}/{Max}): {Message}", attempt, maxAttempts, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(4));
        }
    }
}

// ---------- HTTP pipeline ----------
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new ApiError("An unexpected error occurred. Please try again."));
}));

// Inside a container we only listen on plain HTTP; the host/reverse proxy handles TLS.
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    app.UseHttpsRedirection();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseSession();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();

app.Run();

public partial class Program { }
