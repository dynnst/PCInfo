using Microsoft.EntityFrameworkCore;
using PCNetworkInspector.Data;
using PCNetworkInspector.Hubs;
using PCNetworkInspector.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Preserve C# PascalCase or camelCase
    });

builder.Services.AddSignalR();
builder.Services.AddHttpClient();

// Database: SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=PCNetworkInventory;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;";

builder.Services.AddDbContext<InventoryDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Register Domain & Hardware Services
builder.Services.AddTransient<IWmiService, WmiService>();
builder.Services.AddTransient<ICpuInfoService, CpuInfoService>();
builder.Services.AddTransient<IMemoryInfoService, MemoryInfoService>();
builder.Services.AddTransient<IDiskInfoService, DiskInfoService>();
builder.Services.AddTransient<IGpuInfoService, GpuInfoService>();
builder.Services.AddTransient<IMotherboardInfoService, MotherboardInfoService>();
builder.Services.AddTransient<IBiosInfoService, BiosInfoService>();
builder.Services.AddTransient<IWindowsInfoService, WindowsInfoService>();
builder.Services.AddTransient<INetworkInfoService, NetworkInfoService>();
builder.Services.AddTransient<ISoftwareInfoService, SoftwareInfoService>();
builder.Services.AddTransient<IPeripheralInfoService, PeripheralInfoService>();
builder.Services.AddTransient<IComputerInfoService, ComputerInfoService>();
builder.Services.AddTransient<IHikvisionService, HikvisionService>();
builder.Services.AddSingleton<ISmartScannerService, SmartScannerService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddTransient<IExportService, ExportService>();

var app = builder.Build();

// Ensure SQL Server database and tables are created automatically
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı oluşturulurken hata meydana geldi. Bağlantı dizesini kontrol edin.");
    }
}

// Configure HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ScannerHub>("/scannerHub");

app.Run();
