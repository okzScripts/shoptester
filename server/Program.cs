using Microsoft.Data.Sqlite;
using server.Records;
using server.Seeds;
using server.Extensions;

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


// Database setup
var createTableRoles = @"
CREATE TABLE IF NOT EXISTS roles (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE
)";

var createTableUsers = @"
CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT NOT NULL UNIQUE,
    email TEXT NOT NULL UNIQUE,
    password TEXT NOT NULL,
    role_id INTEGER,
    FOREIGN KEY(role_id) REFERENCES roles(id)
        ON DELETE SET NULL
        ON UPDATE CASCADE
)";

var createTableCategories = @"
CREATE TABLE IF NOT EXISTS categories (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE
)";

var createTableProducts = @"
CREATE TABLE IF NOT EXISTS products (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    price INTEGER NOT NULL,
    category_id INTEGER,
    FOREIGN KEY(category_id) REFERENCES categories(id)
        ON DELETE SET NULL
        ON UPDATE CASCADE
)";

var createProductsView = @"
CREATE VIEW IF NOT EXISTS products_view AS
SELECT products.id, products.name, products.price, categories.name AS category
FROM products
LEFT JOIN categories ON products.category_id = categories.id
";

var createUserView = @"
CREATE VIEW IF NOT EXISTS user_view AS
SELECT users.id, users.username, users.email, roles.name AS role
FROM users
LEFT JOIN roles ON users.role_id = roles.id
";

var createTableOrders = @"
CREATE TABLE IF NOT EXISTS orders (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    customer_id INTEGER,
    product_id INTEGER,
    quantity INTEGER NOT NULL,
    price INTEGER NOT NULL,
    total INTEGER GENERATED ALWAYS AS (quantity * price) VIRTUAL,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(customer_id) REFERENCES users(id)
        ON DELETE SET NULL
        ON UPDATE CASCADE,
    FOREIGN KEY(product_id) REFERENCES products(id)
        ON DELETE SET NULL
        ON UPDATE CASCADE
)";

var createTableOrderView = @"
CREATE VIEW IF NOT EXISTS order_view AS
SELECT orders.id, users.username, products.name as item, orders.quantity, orders.price, orders.total, orders.created_at
FROM orders
LEFT JOIN users ON orders.customer_id = users.id
LEFT JOIN products ON orders.product_id = products.id
";


// Static method to ensure connection is open - 
// used in all API endpoints - 
// in case of connection is closed this method will open it
static void EnsureConnectionOpen(SqliteConnection connection)
{
    if (connection.State != System.Data.ConnectionState.Open)
    {
        connection.Open();
    }
}

// Opening connection and creating tables
try
{
    var connection = app.Services.GetRequiredService<SqliteConnection>();
    await connection.OpenAsync();
    Console.WriteLine("**Connected to database**\n");

    Console.WriteLine("Enabling foreign keys...");
    using (var command = new SqliteCommand("PRAGMA foreign_keys = ON", connection))
    {
        await command.ExecuteNonQueryAsync();
    }
    Console.WriteLine("Foreign keys enabled\n");

    Console.WriteLine("Creating table roles...if not exists");
    using (var command = new SqliteCommand(createTableRoles, connection))
    {
        await command.ExecuteNonQueryAsync();
    }
    Console.WriteLine("Creating table roles completed\n");

    Console.WriteLine("Creating table users...if not exists");
    using (var command = new SqliteCommand(createTableUsers, connection))
    {
        await command.ExecuteNonQueryAsync();
    }
    Console.WriteLine("Creating table users completed\n");

    Console.WriteLine("Creating table categories...if not exists");
    using (var command = new SqliteCommand(createTableCategories, connection))
    {
        await command.ExecuteNonQueryAsync();
    }
    Console.WriteLine("Creating table categories completed\n");

    Console.WriteLine("Creating table products...if not exists");
    using (var command = new SqliteCommand(createTableProducts, connection))
    {
        await command.ExecuteNonQueryAsync();
    }
    Console.WriteLine("Creating table products completed\n");

    Console.WriteLine("Creating table orders...if not exists");
    using (var command = new SqliteCommand(createTableOrders, connection))
    {
        await command.ExecuteNonQueryAsync();
    }
    Console.WriteLine("Creating table orders completed\n");

    Console.WriteLine("Creating view order_view...if not exists");
    using (var command = new SqliteCommand(createTableOrderView, connection))
    {
        await command.ExecuteNonQueryAsync();
    }
    Console.WriteLine("View order_view created\n");

    Console.WriteLine("Creating view user_view...if not exists");
    using (var command = new SqliteCommand(createUserView, connection))
    {
        await command.ExecuteNonQueryAsync();
    }
    Console.WriteLine("View user_view created\n");

    Console.WriteLine("Creating view products_view...if not exists");
    using (var command = new SqliteCommand(createProductsView, connection))
    {
        await command.ExecuteNonQueryAsync();
    }
    Console.WriteLine("View products_view created\n");

    Console.WriteLine("**Database setup completed**");

    // Populate database with sample data
    Console.WriteLine("\nPopulating database with sample data...");
    await DatabaseSeeder.PopulateSampleData(connection);
    Console.WriteLine("**Sample data populated**\n");

}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

app.MapGet("/api", () => "Hello World!");

// Products API
app.MapPost("/api/products", async (ProductCreate product, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);

    var sql = "INSERT INTO products (name, price, category_id) VALUES ($name, $price, $category_id)";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$name", product.Name);
    command.Parameters.AddWithValue("$price", product.Price);
    command.Parameters.AddWithValue("$category_id", product.CategoryId);
    await command.ExecuteNonQueryAsync();
    using var command2 = new SqliteCommand("SELECT last_insert_rowid()", connection);
    var id = (long?)await command2.ExecuteScalarAsync();
    Console.WriteLine($"Info: Product {product.Name} added to database");
    return Results.Ok(new { name = product.Name, price = product.Price, category = product.CategoryId, insertId = id });
}).RequireRole("admin");

app.MapGet("/api/products", async (SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);
    var sql = "SELECT * FROM products_view";
    using var command = new SqliteCommand(sql, connection);
    using var reader = await command.ExecuteReaderAsync();
    var products = new List<ProductRead>();
    while (await reader.ReadAsync())
    {
        var item = new ProductRead(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetString(3)
        );
        products.Add(item);
    }
    return Results.Ok(products);
});

app.MapGet("/api/products/{id}", async (int id, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);
    var sql = "SELECT * FROM products WHERE id = $id";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$id", id);
    using var reader = await command.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        var product = new ProductRead(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetString(3)
        );
        return Results.Ok(product);
    }
    return Results.NotFound("Product not found");
});

app.MapPatch("/api/products/{id}", async (int id, ProductPatch product, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);

    // Build SQL query based on which fields are provided
    var updates = new List<string>();
    if (product.Name != null) updates.Add("name = $name");
    if (product.Price != null) updates.Add("price = $price");
    if (product.CategoryId != null) updates.Add("category_id = $categoryId");

    if (updates.Count == 0)
        return Results.BadRequest("No fields to update");

    var sql = $"UPDATE products SET {string.Join(", ", updates)} WHERE id = $id";
    using var command = new SqliteCommand(sql, connection);

    // Only add parameters for fields that were provided
    if (product.Name != null) command.Parameters.AddWithValue("$name", product.Name);
    if (product.Price != null) command.Parameters.AddWithValue("$price", product.Price);
    if (product.CategoryId != null) command.Parameters.AddWithValue("$categoryId", product.CategoryId);
    command.Parameters.AddWithValue("$id", id);

    var rowsAffected = await command.ExecuteNonQueryAsync();
    if (rowsAffected == 0)
        return Results.NotFound();

    Console.WriteLine($"Info: Product ID:{id} updated in database");
    return Results.Ok(new { message = $"Product with id:{id} updated" });
}).RequireRole("admin");

app.MapDelete("/api/products/{id}", async (int id, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);
    var sql = "DELETE FROM products WHERE id = $id";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$id", id);
    var rowsAffected = await command.ExecuteNonQueryAsync();
    if (rowsAffected == 0)
        return Results.NotFound();
    Console.WriteLine($"Info: Product with id {id} deleted from database");
    return Results.Ok(new { message = $"Product with id:{id} deleted" });
}).RequireRole("admin");

// Products by category API
app.MapGet("/api/{category}/products", async (string category, SqliteConnection connection) =>
{
    Console.WriteLine($"Category: {category}");
    Console.WriteLine("Fetching products by category...");
    EnsureConnectionOpen(connection);
    var sql = "SELECT * FROM products_view WHERE LOWER(category) = LOWER($category)";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$category", category);
    using var reader = await command.ExecuteReaderAsync();
    var products = new List<ProductRead>();
    while (await reader.ReadAsync())
    {
        var item = new ProductRead(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetInt32(2),
            reader.GetString(3)
        );
        Console.WriteLine(item);
        products.Add(item);
    }
    return products.Count > 0 ? Results.Ok(products) : Results.NotFound($"No products found in category: {category}");
});

// Categories API
app.MapPost("/api/categories", async (CategoryCreate category, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);
    var sql = "INSERT INTO categories (name) VALUES ($name)";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$name", category.Name);
    await command.ExecuteNonQueryAsync();
    using var command2 = new SqliteCommand("SELECT last_insert_rowid()", connection);
    var id = (long?)await command2.ExecuteScalarAsync();
    Console.WriteLine($"Info: Category {category} added to database");
    return Results.Ok(new { category = category.Name, insertId = id });
}).RequireRole("admin");

app.MapGet("/api/categories", async (SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);
    var sql = "SELECT * FROM categories";
    using var command = new SqliteCommand(sql, connection);
    using var reader = await command.ExecuteReaderAsync();
    var categories = new List<CategoryRead>();
    while (await reader.ReadAsync())
    {
        var category = new CategoryRead(
            reader.GetInt32(0),
            reader.GetString(1)
        );

        categories.Add(category);
    }
    return Results.Ok(categories);
});

app.MapPatch("/api/categories/{id}", async (int id, CategoryPatch category, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);

    var sql = "UPDATE categories SET name = $name WHERE id = $id";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$name", category.Name);
    command.Parameters.AddWithValue("$id", id);
    var rowsAffected = await command.ExecuteNonQueryAsync();
    if (rowsAffected == 0)
        return Results.NotFound();
    Console.WriteLine($"Info: Category ID:{id} updated in database");
    return Results.Ok(new { message = $"Category with id:{id} updated" });
}).RequireRole("admin");

app.MapDelete("/api/categories/{id}", async (int id, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);
    var sql = "DELETE FROM categories WHERE id = $id";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$id", id);
    var rowsAffected = await command.ExecuteNonQueryAsync();
    if (rowsAffected == 0)
        return Results.NotFound();
    Console.WriteLine($"Info: Category with id {id} deleted from database");
    return Results.Ok(new { message = $"Category with id:{id} deleted" });
}).RequireRole("admin");

// Login API
app.MapPost("/api/login", async (HttpContext context, UserLogin user, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);

    if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.Password))
    {
        return Results.BadRequest("Email and password are required");
    }

    if (context.Session.GetInt32("id") != null)
    {
        return Results.Ok(new { message = "Already logged in" });
    }

    var sql = "SELECT * FROM users WHERE email = $email AND password = $password";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$email", user.Email);
    command.Parameters.AddWithValue("$password", user.Password);
    using var reader = await command.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        var id = reader.GetInt32(0);
        var username = reader.GetString(1);
        var email = reader.GetString(2);
        var role = reader.GetString(4);

        context.Session.SetInt32("id", id);
        context.Session.SetString("username", username);
        context.Session.SetString("email", email);
        if (role == "1")
        {
            role = "admin";
        }
        else
        {
            role = "user";
        }
        context.Session.SetString("role", role);
        return Results.Ok(new { id, username, email, role });
    }

    return Results.NotFound("User not found");

});

app.MapGet("/api/login", (HttpContext context) =>
{
    var id = context.Session.GetInt32("id");
    var username = context.Session.GetString("username");
    var email = context.Session.GetString("email");
    var role = context.Session.GetString("role");

    if (id == null)
    {
        return Results.Json(new { message = "Not logged in" }, statusCode: 401);
    }

    return Results.Ok(new { id, username, email, role });
});

app.MapDelete("/api/login", (HttpContext context) =>
{
    context.Session.Clear();
    return Results.Ok(new { message = "Logged out" });
});

// Users API
app.MapGet("/api/users", async (SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);
    var sql = "SELECT * FROM user_view";
    using var command = new SqliteCommand(sql, connection);
    using var reader = await command.ExecuteReaderAsync();
    var users = new List<UserRead>();
    while (await reader.ReadAsync())
    {
        var user = new UserRead(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3)
        );
        users.Add(user);
    }
    return Results.Ok(users);
}).RequireRole("admin");

app.MapPost("/api/users", async (UserCreate user, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);
    var sql = "INSERT INTO users (username, email, password, role_id) VALUES ($username, $email, $password, $role_id)";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$username", user.Username);
    command.Parameters.AddWithValue("$email", user.Email);
    command.Parameters.AddWithValue("$password", user.Password);
    command.Parameters.AddWithValue("$role_id", user.RoleId);
    await command.ExecuteNonQueryAsync();
    using var command2 = new SqliteCommand("SELECT last_insert_rowid()", connection);
    var id = (long?)await command2.ExecuteScalarAsync();
    Console.WriteLine($"Info: User {user.Username} added to database");
    return Results.Ok(new { username = user.Username, email = user.Email, password = user.Password, role = user.RoleId, insertId = id });
}).RequireRole("admin");

app.MapGet("/api/users/{id}", async (int id, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);
    var sql = "SELECT * FROM user_view WHERE id = $id";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$id", id);
    using var reader = await command.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        var user = new UserRead(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3)
        );
        return Results.Ok(user);
    }
    return Results.NotFound("User not found");
}).RequireRole("admin");

app.MapPatch("/api/users/{id}", async (int id, UserPatch user, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);

    // Build SQL query based on which fields are provided
    var updates = new List<string>();
    if (user.Username != null) updates.Add("username = $username");
    if (user.Email != null) updates.Add("email = $email");
    if (user.Password != null) updates.Add("password = $password");
    if (user.RoleId != null) updates.Add("role_id = $role_id");

    if (updates.Count == 0)
        return Results.BadRequest("No fields to update");

    var sql = $"UPDATE users SET {string.Join(", ", updates)} WHERE id = $id";
    using var command = new SqliteCommand(sql, connection);

    // Only add parameters for fields that were provided
    if (user.Username != null) command.Parameters.AddWithValue("$username", user.Username);
    if (user.Email != null) command.Parameters.AddWithValue("$email", user.Email);
    if (user.Password != null) command.Parameters.AddWithValue("$password", user.Password);
    if (user.RoleId != null) command.Parameters.AddWithValue("$role_id", user.RoleId);
    command.Parameters.AddWithValue("$id", id);

    var rowsAffected = await command.ExecuteNonQueryAsync();
    if (rowsAffected == 0)
        return Results.NotFound();

    Console.WriteLine($"Info: User ID:{id} updated in database");
    return Results.Ok(new { message = $"User with id:{id} updated" });
}).RequireRole("admin");

app.MapDelete("/api/users/{id}", async (int id, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);
    var sql = "DELETE FROM users WHERE id = $id";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$id", id);
    var rowsAffected = await command.ExecuteNonQueryAsync();
    if (rowsAffected == 0)
        return Results.NotFound();
    Console.WriteLine($"Info: User with id {id} deleted from database");
    return Results.Ok(new { message = $"User with id:{id} deleted" });
}).RequireRole("admin");

// Orders API
app.MapPost("/api/orders", async (OrderCreate order, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);

    // Get the price of the product
    var priceCommand = new SqliteCommand("SELECT price FROM products WHERE id = $id", connection);
    priceCommand.Parameters.AddWithValue("$id", order.ProductId);
    var price = await priceCommand.ExecuteScalarAsync();
    if (price == null)
        return Results.NotFound("Product not found");

    // Then insert the order with the correct price
    var sql = "INSERT INTO orders (customer_id, product_id, quantity, price) VALUES ($customer_id, $product_id, $quantity, $price)";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$customer_id", order.CustomerId);
    command.Parameters.AddWithValue("$product_id", order.ProductId);
    command.Parameters.AddWithValue("$quantity", order.Quantity);
    command.Parameters.AddWithValue("$price", price);
    await command.ExecuteNonQueryAsync();
    using var command2 = new SqliteCommand("SELECT last_insert_rowid()", connection);
    var id = (long?)await command2.ExecuteScalarAsync();
    Console.WriteLine($"Info: Order added to database");
    var total = order.Quantity * (Int64)price;
    return Results.Ok(new { customer_id = order.CustomerId, product_id = order.ProductId, quantity = order.Quantity, price, total, insertId = id });
});

app.MapGet("/api/orders", async (SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);
    var sql = "SELECT * FROM order_view";
    using var command = new SqliteCommand(sql, connection);
    using var reader = await command.ExecuteReaderAsync();
    var orders = new List<OrderRead>();
    while (await reader.ReadAsync())
    {
        var order = new OrderRead(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetDateTime(6)
        );
        orders.Add(order);
    }
    return Results.Ok(orders);
}).RequireRole("admin");

app.MapGet("/api/orders/{id}", async (int id, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);
    var sql = "SELECT * FROM order_view WHERE id = $id";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$id", id);
    using var reader = await command.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        var order = new OrderRead(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetDateTime(6)
        );
        return Results.Ok(order);
    }
    return Results.NotFound("Order not found");
}).RequireRole("admin");

app.MapDelete("/api/orders/{id}", async (int id, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);
    var sql = "DELETE FROM orders WHERE id = $id";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$id", id);
    var rowsAffected = await command.ExecuteNonQueryAsync();
    if (rowsAffected == 0)
        return Results.NotFound();
    Console.WriteLine($"Info: Order with id {id} deleted from database");
    return Results.Ok(new { message = $"Order with id:{id} deleted" });
}).RequireRole("admin");


// NOTE: This endpoint adress is a little different due to risk of conflict with the previous ones like /api/orders/{id} and /api/users/{id} 
app.MapGet("/api/user/{id}/orders", async (int id, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);
    var sql = "SELECT * FROM order_view WHERE username = (SELECT username FROM users WHERE id = $id)";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$id", id);
    using var reader = await command.ExecuteReaderAsync();
    var orders = new List<OrderRead>();
    while (await reader.ReadAsync())
    {
        var order = new OrderRead(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetDateTime(6)
        );
        orders.Add(order);
    }
    return Results.Ok(orders);
});
app.MapGet("/api/user/{id}/orders/{orderId}", async (int id, int orderId, SqliteConnection connection) =>
{
    EnsureConnectionOpen(connection);
    var sql = "SELECT * FROM order_view WHERE id = $orderId AND username = (SELECT username FROM users WHERE id = $id)";
    using var command = new SqliteCommand(sql, connection);
    command.Parameters.AddWithValue("$id", id);
    command.Parameters.AddWithValue("$orderId", orderId);
    using var reader = await command.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        var order = new OrderRead(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetInt32(5),
            reader.GetDateTime(6)
        );
        return Results.Ok(order);
    }
    return Results.NotFound("Order not found");
});

//*************************
// Thoughts and cookies about the code:
// 1. I can make it much more readable by creating a method that will execute the SQL query and return the result
// 2. I can make the use of sending in user/customer id to the endpoint redundant -
//     by instead checking if the user is logged in and use the id from the session
// 3. Will change the table for orders to have also have email instead of just user_id - 
//     in case the user is deleted or the buyer is not a registered user and creating an order -
//     does not require the customer to be a registered user. -
//     The endpoint to get orders by user will then be changed to use email instead of user_id. -
//     This is needed to make the frontend more user friendly and not require the customer to be a registered user
// 4. I should move out the SQL create table queries to the DatabaseSeeder class and call them from there...too lazy to do it now
//************************

// Closing connection when application stops
app.Lifetime.ApplicationStopping.Register(() =>
{
    var connection = app.Services.GetRequiredService<SqliteConnection>();
    connection.Close();
    connection.Dispose();
    Console.WriteLine("Connection closed");
});

await app.RunAsync();
