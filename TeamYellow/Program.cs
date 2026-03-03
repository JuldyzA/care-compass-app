using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamYellow.Data;
using TeamYellow.Data.Seed;
using TeamYellow.Repositories;
using TeamYellow.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

// Services
builder.Services.AddScoped<CounsellorService>();

// Repositories
builder.Services.AddScoped<ICounsellorRepository, CounsellorRepository>();

// Seeders
builder.Services.AddTransient<RoleSeeder>();
builder.Services.AddTransient<IdentitySeeder>();
builder.Services.AddTransient<UserProfileSeeder>();
builder.Services.AddTransient<UserLogSeeder>();
builder.Services.AddTransient<CounsellorSeeder>();
builder.Services.AddTransient<ClientSeeder>();
builder.Services.AddTransient<SubscriptionSeeder>();
builder.Services.AddTransient<PaymentTransactionSeeder>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Seeding
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    await services.GetRequiredService<RoleSeeder>().SeedAsync();     
    await services.GetRequiredService<IdentitySeeder>().SeedAsync(); 

    await services.GetRequiredService<UserProfileSeeder>().SeedAsync();
    await services.GetRequiredService<CounsellorSeeder>().SeedAsync();
    await services.GetRequiredService<UserLogSeeder>().SeedAsync();
    await services.GetRequiredService<ClientSeeder>().SeedAsync();
    await services.GetRequiredService<SubscriptionSeeder>().SeedAsync();
    await services.GetRequiredService<PaymentTransactionSeeder>().SeedAsync();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
