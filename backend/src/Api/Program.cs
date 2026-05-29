using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<Core.Interfaces.IHtmlSanitizerService, Infrastructure.Services.HtmlSanitizerService>();
builder.Services.AddScoped<Core.Interfaces.IArticleService, Infrastructure.Services.ArticleService>();
builder.Services.AddScoped<Core.Interfaces.IServicePriceService, Infrastructure.Services.ServicePriceService>();
builder.Services.AddScoped<Core.Interfaces.ICallbackService, Infrastructure.Services.CallbackService>();
builder.Services.AddScoped<Core.Interfaces.IContactService, Infrastructure.Services.ContactService>();

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
}

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevCorsPolicy);
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program { }
