var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure to listen on all interfaces
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

// Configure the HTTP request pipeline - enable Swagger in all environments for MVP
app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kashout API V1");
    c.RoutePrefix = string.Empty; // Set Swagger UI at root
});

app.UseAuthorization();
app.MapControllers();

Console.WriteLine("🚀 Kashout MVP API is running!");
Console.WriteLine("📍 API: http://localhost:5000/api/v1/verification");
Console.WriteLine("📖 Swagger UI: http://localhost:5000");

app.Run();

// Make Program accessible to tests
public partial class Program { }
