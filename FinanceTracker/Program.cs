using FinanceTracker.Data;
using FinanceTracker.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext with SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity with AppUser
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "supersecretkeywithatleast32characterslong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FinanceTracker";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FinanceTrackerUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(token))
                context.Token = token;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=About}/{id?}");

// Seed test user and default categories
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var testUser = await userManager.FindByNameAsync("test");
    if (testUser == null)
    {
        var newUser = new AppUser { UserName = "test", Email = "test@example.com" };
        var result = await userManager.CreateAsync(newUser, "test");
        if (result.Succeeded)
        {
            var defaultCategories = new List<Category>
            {
                new Category { Name = "Продукты", Icon = "🍎", Color = "#4CAF50", DefaultType = TransactionType.Expense, UserId = newUser.Id },
                new Category { Name = "Транспорт", Icon = "🚗", Color = "#2196F3", DefaultType = TransactionType.Expense, UserId = newUser.Id },
                new Category { Name = "Кафе", Icon = "🍕", Color = "#FF9800", DefaultType = TransactionType.Expense, UserId = newUser.Id },
                new Category { Name = "Развлечения", Icon = "🎬", Color = "#9C27B0", DefaultType = TransactionType.Expense, UserId = newUser.Id },
                new Category { Name = "Зарплата", Icon = "💰", Color = "#4CAF50", DefaultType = TransactionType.Income, UserId = newUser.Id },
                new Category { Name = "Фриланс", Icon = "💻", Color = "#00BCD4", DefaultType = TransactionType.Income, UserId = newUser.Id },
                new Category { Name = "Подарки", Icon = "🎁", Color = "#E91E63", DefaultType = TransactionType.Income, UserId = newUser.Id }
            };
            dbContext.Categories.AddRange(defaultCategories);
            await dbContext.SaveChangesAsync();
        }
    }
}

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();