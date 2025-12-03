using Tablitsya3.Components;
using Tablitsya3.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<ProductionPlanningService>();
builder.Services.AddSingleton<DataStorageService>();
builder.Services.AddSingleton<WorkingDaysService>();
builder.Services.AddSingleton<LoggingService>();
builder.Services.AddSingleton<DataSeedService>();

var app = builder.Build();

// Seed initial data on first run
using (var scope = app.Services.CreateScope())
{
    var seedService = scope.ServiceProvider.GetRequiredService<DataSeedService>();
    await seedService.SeedInitialDataIfEmpty();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
