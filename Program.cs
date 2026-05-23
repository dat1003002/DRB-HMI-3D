using DRB_HMI_3D.Data;
using DRB_HMI_3D.Hubs;
using DRB_HMI_3D.Repository;
using DRB_HMI_3D.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.UseCompatibilityLevel(120)
    ));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddRazorPages();

builder.Services.AddSignalR();

builder.Services.AddSingleton<HmiRealtimeStore>();
builder.Services.AddHostedService<KepwareSubscriptionWorker>();

builder.Services.AddScoped<IWorkshopRepository, WorkshopRepository>();
builder.Services.AddScoped<IWorkshopService, WorkshopService>();
builder.Services.AddScoped<IPressGroupRepository, PressGroupRepository>();
builder.Services.AddScoped<IPressGroupService, PressGroupService>();
builder.Services.AddScoped<IPressItemTagRepository, PressItemTagRepository>();
builder.Services.AddScoped<IPressItemTagService, PressItemTagService>();

var app = builder.Build();

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
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.MapHub<HmiRealtimeHub>("/hubs/hmi-realtime");

app.Run();