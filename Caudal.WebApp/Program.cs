using Caudal.Application;
using Caudal.Infraestructure.PostgreSQL;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddPostgreSQL();
builder.Services.AddApplication(options => options.UsePostgreSQL(builder.Configuration));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(option =>
{
    option.LoginPath = "/account/login";
    option.Cookie.Name = "caudal.auth";
    option.Cookie.HttpOnly = true;
    option.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    option.Cookie.SameSite = SameSiteMode.Lax;
    option.ExpireTimeSpan = TimeSpan.FromHours(8);
    option.SlidingExpiration = true;
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/500");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Platform}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
