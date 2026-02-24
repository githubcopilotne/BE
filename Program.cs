using BE.Models;
using BE.Services.Implementations;
using BE.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =============================================
// 1. DbContext — kết nối SQL Server
// =============================================
builder.Services.AddDbContext<ShopQuanAoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// =============================================
// 2. DI — đăng ký Service
// =============================================
builder.Services.AddScoped<IAuthService, AuthService>();

// =============================================
// 3. CORS — cho phép FE gọi API
// =============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFE", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Bật CORS (phải đặt trước UseAuthorization)
app.UseCors("AllowFE");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
