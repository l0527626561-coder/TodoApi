using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BCrypt.Net;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Services;
using Microsoft.IdentityModel.JsonWebTokens;

// Main application entry point
var builder = WebApplication.CreateBuilder(args);

// --- Configu3ration defaults (overridden by appsettings.json or environment) ---
builder.Configuration["ConnectionStrings:ToDoDB"] ??= "Server=bstb8t7djxp4h4xbbbap-mysql.services.clever-cloud.com;Port=3306;Database=bstb8t7djxp4h4xbbbap;Uid=u59yqsvvgeu51dai;Pwd=cswzmQyF1IBIvQxwlpMR;";

builder.Configuration["Jwt:Key"] ??= "DevSecretKey_DoNotUseInProd_ReplaceThis";
builder.Configuration["Jwt:Issuer"] ??= "TodoApi";
builder.Configuration["Jwt:Audience"] ??= "TodoClient";
builder.Configuration["Jwt:ExpireMinutes"] ??= "120";

// --- Configure Logging ---
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
// Configure MySQL database
var connectionString = builder.Configuration.GetConnectionString("ToDoDB");
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Add Authentication and Authorization
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.WithOrigins("http://localhost:3000")
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
        });
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TodoApi", Version = "v1" });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Register JWT service
builder.Services.AddScoped<IJwtService>(provider => 
    new JwtService(
        builder.Configuration["Jwt:Key"]!,
        builder.Configuration["Jwt:Issuer"]!,
        builder.Configuration["Jwt:Audience"]!,
        int.Parse(builder.Configuration["Jwt:ExpireMinutes"]!)));

// Build the application
// Configure the HTTP request pipeline.
var app = builder.Build();

// Configure the URLs the app listens on
app.Urls.Add("http://localhost:8080");
app.Urls.Add("https://localhost:8081");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TodoApi v1"));
}

app.UseCors("AllowAll");
// Disable HTTPS redirect in development to avoid mixed content issues
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ToDoDbContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// --------------------------- API Endpoints ---------------------------

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("System")
    .Produces(StatusCodes.Status200OK);

// User registration
app.MapPost("/api/auth/register", async (RegisterDto registerDto, ToDoDbContext db, IJwtService jwtService, ILogger<Program> logger) =>
{
    logger.LogInformation($"Registration attempt for user: {registerDto.Username}");
    
    // Check if user already exists
    if (await db.Users.AnyAsync(u => u.Username == registerDto.Username))
    {
        logger.LogWarning($"Registration failed - Username {registerDto.Username} already exists");
        return Results.Conflict(new { message = "Username already exists" });
    }

    // Create new user
    var user = new User
    {
        Username = registerDto.Username!,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password!)
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();
    
    logger.LogInformation($"User {user.Username} registered successfully with ID: {user.Id}");
    
    // Generate JWT token
    var token = jwtService.GenerateToken(user.Id.ToString(), user.Username);
    
    return Results.Ok(new 
    { 
        user.Id,
        user.Username,
        Token = token
    });
})
.WithName("RegisterUser")
.WithTags("Authentication")
.Produces(StatusCodes.Status200OK)
.Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status409Conflict);

// User login
app.MapPost("/api/auth/login", async (LoginDto loginDto, ToDoDbContext db, IJwtService jwtService, ILogger<Program> logger) =>
{
    logger.LogInformation($"Login attempt for user: {loginDto.Username}");
    
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == loginDto.Username);
    
    if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
    {
        logger.LogWarning($"Login failed for user: {loginDto.Username}");
        return Results.Unauthorized();
    }
    
    logger.LogInformation($"User {user.Username} logged in successfully");
    
    // Generate JWT token
    var token = jwtService.GenerateToken(user.Id.ToString(), user.Username);
    
    return Results.Ok(new 
    { 
        user.Id,
        user.Username,
        Token = token
    });
})
.WithName("LoginUser")
.WithTags("Authentication")
.Produces(StatusCodes.Status200OK)
.Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized);

// Get current user profile
app.MapGet("/api/auth/me", (ClaimsPrincipal user) =>
{
    return Results.Ok(new 
    { 
        Id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
        Username = user.FindFirst(ClaimTypes.Name)?.Value
    });
})
.RequireAuthorization()
.WithName("GetCurrentUser")
.WithTags("Authentication")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized);

// CRUD Endpoints for Todo Items

// Get all todo items
app.MapGet("/api/items", async (ToDoDbContext db, ClaimsPrincipal user, ILogger<Program> logger) =>
{
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
    logger.LogInformation($"User {userId} is fetching all todo items");
    
    var items = await db.Items
        .Where(i => i.UserId == userId)
        .AsNoTracking()
        .ToListAsync();
        
    return Results.Ok(items);
})
.RequireAuthorization()
.WithName("GetAllItems")
.WithTags("Items")
.Produces<List<Item>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized);

// Get todo item by ID
app.MapGet("/api/items/{id}", async (int id, ToDoDbContext db, ClaimsPrincipal user, ILogger<Program> logger) =>
{
    var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
    logger.LogInformation($"User {userId} is fetching todo item {id}");
    
    var item = await db.Items
        .AsNoTracking()
        .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
        
    if (item == null)
    {
        logger.LogWarning($"Todo item {id} not found for user {userId}");
        return Results.NotFound();
    }
    
    return Results.Ok(item);
})
.RequireAuthorization()
.WithName("GetItemById")
.WithTags("Items")
.Produces<Item>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status404NotFound);

// Create a new todo item
app.MapPost("/api/items", [Authorize] async (CreateItemDto createDto, ToDoDbContext db, ILogger<Program> logger, HttpContext httpContext) =>
{
    try
    {
        // Get user from the HttpContext
        var userId = int.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        
        // Create new item
        var item = new Item
        {
            Name = createDto.Name!,
            IsComplete = false,
            CreatedAt = DateTime.UtcNow,
            UserId = userId
        };

        db.Items.Add(item);
        await db.SaveChangesAsync();
        
        logger.LogInformation($"Created new todo item with ID {item.Id} for user {userId}");
        
        return Results.Created($"/api/items/{item.Id}", item);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating todo item");
        return Results.Problem("An error occurred while creating the todo item");
    }
})
.WithName("CreateItem")
.WithTags("Items")
.Produces<Item>(StatusCodes.Status201Created)
.Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized);

// Update an existing todo item
app.MapPut("/api/items/{id}", [Authorize] async (int id, UpdateItemDto updateDto, ToDoDbContext db, ILogger<Program> logger, HttpContext httpContext) =>
{
    try
    {
        var userId = int.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        
        var item = await db.Items.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
        
        if (item == null)
        {
            logger.LogWarning($"Todo item {id} not found for user {userId}");
            return Results.NotFound();
        }
        
        item.Name = updateDto.Name ?? item.Name;
        item.IsComplete = updateDto.IsComplete;
        
        await db.SaveChangesAsync();
        
        logger.LogInformation($"Updated todo item {id} for user {userId}");
        
        return Results.Ok(item);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, $"Error updating todo item {id}");
        return Results.Problem("An error occurred while updating the todo item");
    }
})
.WithName("UpdateItem")
.WithTags("Items")
.Produces<Item>(StatusCodes.Status200OK)
.Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status404NotFound);

// Delete a todo item
app.MapDelete("/api/items/{id}", [Authorize] async (int id, ToDoDbContext db, ILogger<Program> logger, HttpContext httpContext) =>
{
    try
    {
        var userId = int.Parse(httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        
        var item = await db.Items.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
        
        if (item == null)
        {
            logger.LogWarning($"Todo item {id} not found for user {userId}");
            return Results.NotFound();
        }
        
        db.Items.Remove(item);
        await db.SaveChangesAsync();
        
        logger.LogInformation($"Deleted todo item {id} for user {userId}");
        
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, $"Error deleting todo item {id}");
        return Results.Problem("An error occurred while deleting the todo item");
    }
})
.WithName("DeleteItem")
.WithTags("Items")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status404NotFound);


// --------------------------- Models and DTOs ---------------------------

// Moved to separate files:
// - Models/Models.cs: Contains Item, User, and DTO classes
// - Data/ToDoDbContext.cs: Contains the database context
// - Services/JwtService.cs: Contains JWT service implementation

// --------------------------- Run ---------------------------
app.MapGet("/", () => "API is running!");

app.Run();
