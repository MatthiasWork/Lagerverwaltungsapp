using Lagerverwaltungsapp_MatthiasUtrata.Models;
using Lagerverwaltungsapp_MatthiasUtrata.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Jede Seite erfordert eine Anmeldung, außer sie ist mit [AllowAnonymous] markiert
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});
builder.Services.AddDbContext<LagerverwaltungContext>(
        options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Anmeldung per Cookie; nicht angemeldete Benutzer werden zum Login umgeleitet
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
        options.LogoutPath = "/Home/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";
    });

builder.Services.AddScoped<IPasswordHasher<Person>, PasswordHasher<Person>>();
builder.Services.AddScoped<PasswordService>();

var app = builder.Build();

// Ersteinrichtung: Gibt es noch keine Person, wird ein Admin angelegt,
// damit man sich überhaupt anmelden kann (Passwort danach ändern!)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<LagerverwaltungContext>();
    var passwordService = scope.ServiceProvider.GetRequiredService<PasswordService>();

    if (!context.Person.Any())
    {
        var adminRolle = context.Rolle.FirstOrDefault(r => r.Admin)
            ?? new Rolle { Name = "Administrator", Admin = true };

        var admin = new Person
        {
            Vorname = "Admin",
            Nachname = "Admin",
            Username = "admin",
            Email = "admin@lagerverwaltung.local",
            Rolle = adminRolle
        };
        admin.Password = passwordService.HashPassword(admin, "admin");

        context.Person.Add(admin);
        context.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
