using Microsoft.EntityFrameworkCore;
using Randomizer.Data;
using Randomizer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<DodeljevanjeRecenzentovService>();
builder.Services.AddScoped<TretjiRecenzentService>();
builder.Services.AddScoped<RecenzentZavrnitveService>();
builder.Services.AddScoped<GrozdiRecenzentZavrnitveService>();

var app = builder.Build();
// Logiranje podatkov o bazi po zagonu aplikacije

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
