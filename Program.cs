using Npgsql;
using PosBackend.Repositories;
using PosBackend.Services;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers().AddDapr();
builder.Services.AddDaprClient(builder => builder
    .UseHttpEndpoint($"http://localhost:3500")
    .UseGrpcEndpoint($"http://localhost:50001"));

// Configure PostgreSQL connection
// Configure PostgreSQL connection
var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");
builder.Services.AddScoped<IDbConnection>(_ =>
{
    var connection = new NpgsqlConnection(connectionString);
    connection.Open();  // Explicitly open the connection
    return connection;
});

// Register repositories
builder.Services.AddScoped<OrdersRepository>();
builder.Services.AddScoped<ProductRepository>();
// Register services
builder.Services.AddScoped<PosService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseSwagger();
app.UseSwaggerUI();


app.UseAuthorization();
app.MapControllers();

// Enable Dapr pub/sub
app.UseCloudEvents();
app.MapSubscribeHandler();

app.Run();