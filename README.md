# Shop API Application

A .NET Core Web API application for managing an online shop with products, categories, users, and orders.

## Features

- **Authentication & Authorization**
  - Session-based authentication
  - Role-based access control (Admin/User)
  - Login/Logout functionality

- **Product Management**
  - Create, read, update, and delete products
  - Products categorization
  - Product price management
  - Filter products by category

- **User Management**
  - User registration and profile management
  - Role assignment
  - User authentication

- **Order System**
  - Create orders
  - Track order history
  - View orders by user
  - Automatic price calculation
  - Order timestamps

## Database Schema

### Tables
- **roles** - User role definitions
  - `id` (Primary Key)
  - `name` (Unique)

- **users** - User accounts
  - `id` (Primary Key)
  - `username` (Unique)
  - `email` (Unique)
  - `password`
  - `role_id` (Foreign Key to roles)

- **categories** - Product categories
  - `id` (Primary Key)
  - `name` (Unique)

- **products** - Product catalog
  - `id` (Primary Key)
  - `name` (Unique)
  - `price`
  - `category_id` (Foreign Key to categories)

- **orders** - Customer orders
  - `id` (Primary Key)
  - `customer_id` (Foreign Key to users)
  - `product_id` (Foreign Key to products)
  - `quantity`
  - `price`
  - `total` (Virtual, calculated)
  - `created_at` (Timestamp)

## API Endpoints

### Products
- `GET /api/products` - List all products
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create product (Admin only)
- `PATCH /api/products/{id}` - Update product (Admin only)
- `DELETE /api/products/{id}` - Delete product (Admin only)
- `GET /api/{category}/products` - Get products by category

### Categories
- `GET /api/categories` - List all categories
- `POST /api/categories` - Create category (Admin only)
- `PATCH /api/categories/{id}` - Update category (Admin only)
- `DELETE /api/categories/{id}` - Delete category (Admin only)

### Users
- `GET /api/users` - List all users (Admin only)
- `POST /api/users` - Create user (Admin only)
- `GET /api/users/{id}` - Get user by ID (Admin only)
- `PATCH /api/users/{id}` - Update user (Admin only)
- `DELETE /api/users/{id}` - Delete user (Admin only)

### Authentication
- `POST /api/login` - Login user
- `GET /api/login` - Check login status
- `DELETE /api/login` - Logout user

### Orders
- `POST /api/orders` - Create order
- `GET /api/orders` - List all orders (Admin only)
- `GET /api/orders/{id}` - Get order by ID (Admin only)
- `DELETE /api/orders/{id}` - Delete order (Admin only)
- `GET /api/user/{id}/orders` - Get user's orders
- `GET /api/user/{id}/orders/{orderId}` - Get specific order for user

## Setup and Configuration

1. Install .NET 7.0 or later
2. Clone the repository
3. Run `dotnet restore` to install dependencies
4. (Optional) - Configure the database connection in `Program.cs`
5. Run `dotnet run` to start the application

## Dependencies

- Microsoft.Data.Sqlite
- ASP.NET Core 7.0
- .NET Core Session Middleware

## Security Features

- Session-based authentication
- Role-based authorization
- SQL injection prevention using parameterized queries
- HTTP-only cookies
- Foreign key constraints
- Unique constraints on sensitive data

## Future Improvements

1. Implement query execution abstraction for better code readability
2. Remove redundant user ID parameters in favor of session-based identification
3. Add email support for orders to accommodate guest purchases
4. Refactor table creation logic into DatabaseSeeder class