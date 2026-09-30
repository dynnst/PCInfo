using Microsoft.Extensions.DependencyInjection;
using PCNetworkInspector.Services;
using PCNetworkInspector.ViewModels;
using PCNetworkInspector.Views;
using System.Windows;

namespace PCNetworkInspector
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Services
            services.AddTransient<IWmiService, WmiService>();
            services.AddTransient<IComputerInfoService, ComputerInfoService>();
            services.AddTransient<ICpuInfoService, CpuInfoService>();
            services.AddTransient<IMemoryInfoService, MemoryInfoService>();
            services.AddTransient<IDiskInfoService, DiskInfoService>();
            services.AddTransient<IGpuInfoService, GpuInfoService>();
            services.AddTransient<INetworkInfoService, NetworkInfoService>();
            services.AddTransient<IWindowsInfoService, WindowsInfoService>();
            services.AddTransient<ISoftwareInfoService, SoftwareInfoService>();
            services.AddTransient<IBiosInfoService, BiosInfoService>();
            services.AddTransient<IMotherboardInfoService, MotherboardInfoService>();
            services.AddTransient<IExportService, ExportService>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<QueryViewModel>();
            services.AddTransient<BulkQueryViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<SettingsViewModel>();

            // Views
            services.AddSingleton<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider.Dispose();
            base.OnExit(e);
        }
    }
}
