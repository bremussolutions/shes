﻿using BSolutions.SHES.App.Activation;
using BSolutions.SHES.App.ComponentModels;
using BSolutions.SHES.App.Contracts.Services;
using BSolutions.SHES.App.Core.Contracts.Services;
using BSolutions.SHES.App.Core.Services;
using BSolutions.SHES.App.Helpers;
using BSolutions.SHES.App.Models;
using BSolutions.SHES.App.Services;
using BSolutions.SHES.App.ViewModels;
using BSolutions.SHES.App.Views;
using BSolutions.SHES.Data;
using BSolutions.SHES.Data.Repositories.Devices;
using BSolutions.SHES.Data.Repositories.ProjectItems;
using BSolutions.SHES.Data.Repositories.Projects;
using BSolutions.SHES.Models.Observables;
using BSolutions.SHES.Services.Devices;
using BSolutions.SHES.Services.Knx;
using BSolutions.SHES.Services.ProjectItems;
using BSolutions.SHES.Services.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Globalization;
using System.IO;
using System.Threading;

// To learn more about WinUI3, see: https://docs.microsoft.com/windows/apps/winui/winui3/.
namespace BSolutions.SHES.App
{
    public partial class App : Application
    {
        // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Default Activation Handler
                services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

                // Database
                services.AddDbContext<ShesDbContext>(options =>
                {
                    // Create database directory
                    string userDocumentPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                    string shesDatabasePath = Path.Combine(userDocumentPath, "SHES");
                    Directory.CreateDirectory(shesDatabasePath);

                    options.UseSqlite($"Data Source={Path.Combine(shesDatabasePath, "shes.db")};");
                    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                });

                // Repositories
                services.AddSingleton<IProjectRepository, ProjectRepository>();
                services.AddSingleton<IProjectItemRepository, ProjectItemRepository>();
                services.AddSingleton<IDeviceRepository, DeviceRepository>();

                // Services
                services.AddSingleton<IProjectService, ProjectService>();
                services.AddSingleton<IProjectItemService, ProjectItemService>();
                services.AddSingleton<IDeviceService, DeviceService>();
                services.AddSingleton<IKnxImportService, KnxImportService>();

                services.AddSingleton<ILocalSettingsService, LocalSettingsServicePackaged>();
                services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
                services.AddTransient<INavigationViewService, NavigationViewService>();

                services.AddSingleton<IActivationService, ActivationService>();
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<INavigationService, NavigationService>();

                // Core Services
                services.AddSingleton<ISampleDataService, SampleDataService>();
                services.AddSingleton<IFileService, FileService>();

                // Views and ViewModels
                services.AddTransient<ShellPage>();
                services.AddTransient<ShellViewModel>();

                services.AddTransient<MainPage>();
                services.AddTransient<MainViewModel>();

                services.AddTransient<BuildingStructurePage>();
                services.AddTransient<BuildingStructureViewModel>();

                services.AddTransient<ProjectListComponentModel>();
                services.AddTransient<ProjectItemTreeComponentModel>();
                services.AddTransient<CabinetDetailsComponentModel>();

                services.AddTransient<SettingsViewModel>();
                services.AddTransient<SettingsPage>();
                services.AddTransient<ContentGridDetailViewModel>();
                services.AddTransient<ContentGridDetailPage>();
                services.AddTransient<ContentGridViewModel>();
                services.AddTransient<ContentGridPage>();
                services.AddTransient<ListDetailsViewModel>();
                services.AddTransient<ListDetailsPage>();
                services.AddTransient<DataGridViewModel>();
                services.AddTransient<DataGridPage>();

                services.AddTransient<ResourceLoader>();

                // Configuration
                services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));
            })
            .Build();

        public static T GetService<T>()
            where T : class
            => _host.Services.GetService(typeof(T)) as T;

        public static Window MainWindow { get; set; } = new Window() { Title = "AppDisplayName".GetLocalized() };

        public App()
        {
            InitializeComponent();
            UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // TODO: Log and handle exceptions as appropriate.
            // For more details, see https://docs.microsoft.com/windows/winui/api/microsoft.ui.xaml.unhandledexceptioneventargs.
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);
            var activationService = App.GetService<IActivationService>();
            await activationService.ActivateAsync(args);
        }
    }
}
