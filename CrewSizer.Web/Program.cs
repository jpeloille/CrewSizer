using CrewSizer.Application.Common;
using CrewSizer.Infrastructure.Identity;
using CrewSizer.Infrastructure.Persistence;
using CrewSizer.Web.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Blazor Server + DevExpress
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDevExpressBlazor(options =>
{
    options.SizeMode = DevExpress.Blazor.SizeMode.Medium;
});

// PostgreSQL + EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=crewsizer;Username=crewsizer;Password=crewsizer";

var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContextFactory<CrewSizerDbContext>(options =>
    options.UseNpgsql(dataSource));

// ASP.NET Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<CrewSizerDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Services UI
builder.Services.AddScoped<CrewSizer.Web.Components.Services.ActiveScenarioService>();

// Application layer (MediatR + FluentValidation)
builder.Services.AddApplication();

var app = builder.Build();

// Migration auto + seed admin en mode dev
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CrewSizerDbContext>();
    await dbContext.Database.MigrateAsync();

    // Seed admin user
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    if (await userManager.FindByNameAsync("admin") is null)
    {
        var admin = new ApplicationUser
        {
            UserName = "admin",
            Email = "admin@crewsizer.local",
            NomComplet = "Administrateur",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(admin, "Admin1!");
    }
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
