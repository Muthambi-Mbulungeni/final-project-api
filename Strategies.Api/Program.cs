using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. CORS Policy (Unblocks Riaad's Vite 5173 and CRA 3000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 3. Add Controllers (Crucial for routing)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. Enable Swagger for you and Kundai
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 5. Apply CORS before Authorization
app.UseCors("AllowFrontend");

app.UseAuthorization();

// 6. Map the Controllers (Fixes the 404 issue)
app.MapControllers();

app.Run();