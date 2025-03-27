using Microsoft.Data.Sqlite;
using server.Seeds;
using server.Extensions;
using server.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Creates a SQLite dabasefile in the root folder of the project - if it does not exist already
builder.Services.AddSingleton(ServiceProvider =>
{
    return new SqliteConnection("Data Source=shoptester.db");
});

var app = builder.Build();
app.UseSession();

// Seed the database with sample data
var connection = app.Services.GetRequiredService<SqliteConnection>();
await DatabaseSeeder.PopulateSampleData(connection);

app.MapGet("/api", () => "Hello World!");

// Products API
app.MapPost("/api/products", ProductHandlers.CreateProduct).RequireRole("admin");

app.MapGet("/api/products", ProductHandlers.GetProducts);

app.MapGet("/api/products/{id}", ProductHandlers.GetProduct);

app.MapPatch("/api/products/{id}", ProductHandlers.UpdateProduct).RequireRole("admin");

app.MapDelete("/api/products/{id}", ProductHandlers.DeleteProduct).RequireRole("admin");

// Products by category API
app.MapGet("/api/{category}/products", ProductHandlers.GetProductsByCategory);

// Categories API
app.MapPost("/api/categories", CategoryHandlers.CreateCategory).RequireRole("admin");

app.MapGet("/api/categories", CategoryHandlers.GetCategories);

app.MapPatch("/api/categories/{id}", CategoryHandlers.UpdateCategory).RequireRole("admin");

app.MapDelete("/api/categories/{id}", CategoryHandlers.DeleteCategory).RequireRole("admin");

// Login API
app.MapPost("/api/login", LoginHandlers.Login);

app.MapGet("/api/login", (Delegate)LoginHandlers.GetLogin);

app.MapDelete("/api/login", (Delegate)LoginHandlers.Logout);

// Users API
app.MapGet("/api/users", UserHandlers.GetUsers).RequireRole("admin");

app.MapPost("/api/users", UserHandlers.CreateUser).RequireRole("admin");

app.MapGet("/api/users/{id}", UserHandlers.GetUser).RequireRole("admin");

app.MapPatch("/api/users/{id}", UserHandlers.UpdateUser).RequireRole("admin");

app.MapDelete("/api/users/{id}", UserHandlers.DeleteUser).RequireRole("admin");

// Orders API
app.MapPost("/api/orders", OrderHandlers.CreateOrder).RequireAuthentication();

app.MapGet("/api/orders", OrderHandlers.GetOrders).RequireRole("admin");

app.MapGet("/api/orders/{id}", OrderHandlers.GetOrder).RequireRole("admin");

app.MapDelete("/api/orders/{id}", OrderHandlers.DeleteOrder).RequireRole("admin");


// NOTE: This endpoint adress is a little different due to risk of conflict with the previous ones like /api/orders/{id} and /api/users/{id} 
app.MapGet("/api/user/{id}/orders", OrderHandlers.GetOrdersByUser).RequireAuthenticationWithId();
app.MapGet("/api/user/{id}/orders/{orderId}", OrderHandlers.GetOrderByUser).RequireAuthenticationWithId();

// Closing connection when application stops
app.Lifetime.ApplicationStopping.Register(() =>
{
    var connection = app.Services.GetRequiredService<SqliteConnection>();
    connection.Close();
    connection.Dispose();
    Console.WriteLine("Connection closed");
});

await app.RunAsync();
