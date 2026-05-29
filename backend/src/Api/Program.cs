using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Infrastructure.Auth;
using Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

if (Environment.GetEnvironmentVariable("JWT_SECRET") is { Length: > 0 } jwtSecret)
    builder.Configuration["Jwt:Secret"] = jwtSecret;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<Core.Interfaces.IHtmlSanitizerService, Infrastructure.Services.HtmlSanitizerService>();
builder.Services.AddScoped<Core.Interfaces.IArticleService, Infrastructure.Services.ArticleService>();
builder.Services.AddScoped<Core.Interfaces.IServicePriceService, Infrastructure.Services.ServicePriceService>();
builder.Services.AddScoped<Core.Interfaces.ICallbackService, Infrastructure.Services.CallbackService>();
builder.Services.AddScoped<Core.Interfaces.IContactService, Infrastructure.Services.ContactService>();
builder.Services.AddScoped<Core.Interfaces.IAuthService, Infrastructure.Services.AuthService>();
builder.Services.AddScoped<Core.Interfaces.IMediaService, Infrastructure.Services.MediaService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS только для dev: в проде фронт ходит через nginx на тот же origin (/api).
const string DevCorsPolicy = "dev";
builder.Services.AddCors(options => options.AddPolicy(DevCorsPolicy, policy =>
    policy.WithOrigins("http://localhost:3001", "http://127.0.0.1:3001")
          .AllowAnyHeader()
          .AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        db.Database.Migrate();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdminSeeder");
    await AdminSeeder.SeedAsync(db, app.Configuration, logger);
}

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevCorsPolicy);
    app.UseSwagger();
    app.UseSwaggerUI();
}

// За nginx работаем по чистому HTTP — редирект на https только в локальной dev-разработке.
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

var uploadsPath = Path.Combine(builder.Environment.ContentRootPath,
    builder.Configuration["Storage:UploadsPath"] ?? "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();

app.Run();

public partial class Program { }
