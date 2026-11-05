using Caudal.Application;
using Caudal.BlazorApp;
using Caudal.BlazorApp.Modules.Account;
using Caudal.Infraestructure.PostgreSQL;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddPostgreSQL();
builder.Services.AddApplication(options => options.UsePostgreSQL(builder.Configuration));
builder.Services.AddAccountModule();

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
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/500", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}", createScopeForStatusCodePages: true);

// Un POST que falla se re-ejecuta con su método, y Blazor lo rechazaría por no traer un formulario.
app.Use((context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Error"))
    {
        context.Request.Method = HttpMethods.Get;
    }

    return next();
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
