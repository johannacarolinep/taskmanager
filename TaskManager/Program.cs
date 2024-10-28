using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using dotenv.net;
using TaskManager.Models;
using TaskManager.Models.Services;
using TaskManager.Models.Stores;

// Cloudinary credentials
DotEnv.Load(options: new DotEnvOptions(probeForEnv: true));
string cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");

// Check if the cloudinaryUrl is correctly loaded
if (string.IsNullOrWhiteSpace(cloudinaryUrl))
{
    throw new ArgumentException("CLOUDINARY_URL is not set correctly.");
}

// Instantiate the Cloudinary object
Cloudinary cloudinary = new Cloudinary(cloudinaryUrl);
cloudinary.Api.Secure = true;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(cloudinary); // Register Cloudinary as a singleton service

// Add services to the container.
builder.Services.AddControllersWithViews();

// Custom services
builder.Services.AddScoped<IUserStore<UserModel>, UserStore>();
builder.Services.AddScoped<IPasswordHasher<UserModel>, PasswordHasher<UserModel>>();
builder.Services.AddScoped<UserMethods>(); // Register UserMethods

builder.Services.AddScoped<TasklistMethods>();
builder.Services.AddScoped<TaskMethods>();
builder.Services.AddScoped<ListUserMethods>();
builder.Services.AddScoped<EmailService>();

// Register TimeProvider (if necessary)
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

// Configure Identity without roles
builder.Services.AddIdentityCore<UserModel>(options =>
{
    // Configure identity options if needed
    options.Password.RequireDigit = true; // Require a digit
    options.Password.RequireLowercase = true; // Require lowercase letter
    options.Password.RequireUppercase = false; // Don't require uppercase
    options.Password.RequiredLength = 8; // Minimum length of 8 characters
    options.Password.RequireNonAlphanumeric = false; // Don't require special characters
    options.User.RequireUniqueEmail = true;
})
    .AddUserStore<UserStore>()
    .AddSignInManager<SignInManager<UserModel>>() // Explicitly add SignInManager
    .AddDefaultTokenProviders();

// Configure authentication services
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddCookie(IdentityConstants.ApplicationScheme, options =>
{
    options.LoginPath = "/User/Login";
    options.AccessDeniedPath = "/User/AccessDenied";
})
.AddCookie(IdentityConstants.ExternalScheme)
.AddCookie(IdentityConstants.TwoFactorRememberMeScheme)
.AddCookie(IdentityConstants.TwoFactorUserIdScheme);

// Start session handling on the server
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(600);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// For accessing session data in the views
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Add session middleware

app.UseAuthentication(); // Ensure this is before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();