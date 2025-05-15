using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureBackup.Services;
using SecureBackup.ViewModels;

namespace SecureBackup
{
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = ServiceProvider.GetRequiredService<MainViewModel>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register configuration
            services.AddSingleton(Configuration);

            // Register services
            services.AddSingleton<FileScanner>();
            services.AddSingleton<EncryptionService>();
            services.AddSingleton<AwsService>();
            services.AddSingleton<FileSystemWatcherService>();
            services.AddSingleton<ConfigurationService>();

            // Register view models
            services.AddSingleton<MainViewModel>();
            services.AddTransient<FileSelectionViewModel>();
            services.AddTransient<ConfigurationViewModel>();

            // Register main window
            services.AddSingleton<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            
            // Dispose of any resources that need cleanup
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
