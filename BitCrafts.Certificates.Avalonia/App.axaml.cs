// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (C) 2025 Younes Benmoussa <benzsoftware@pm.me>
// 
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// You should have received a copy of the GNU Affero General Public License along with this program. If not, see <https://www.gnu.org/licenses/>. 

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using BitCrafts.Certificates.Avalonia.ViewModels;
using BitCrafts.Certificates.Avalonia.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using BitCrafts.Certificates.Services;
using BitCrafts.Certificates.Data;
using BitCrafts.Certificates.Data.Repositories;

namespace BitCrafts.Certificates.Avalonia;

public partial class App : global::Avalonia.Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Configure services
            ServiceProvider = Program.ConfigureServices();

            // Initialize data directory and schema
            InitializeDatabase();

            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            
            // Check if setup is needed
            if (RequiresSetup())
            {
                desktop.MainWindow = new SetupWindow
                {
                    DataContext = new SetupViewModel(ServiceProvider, () => ShowMainWindow(desktop)),
                };
            }
            else
            {
                ShowMainWindow(desktop);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private bool RequiresSetup()
    {
        if (ServiceProvider == null) return true;

        try
        {
            var settings = ServiceProvider.GetRequiredService<ISettingsRepository>();
            var domain = settings.GetAsync("BITCRAFTS_DOMAIN").Result;
            return string.IsNullOrWhiteSpace(domain);
        }
        catch
        {
            return true;
        }
    }

    private void ShowMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (ServiceProvider == null) return;

        var mainWindow = new MainWindow
        {
            DataContext = new MainWindowViewModel(ServiceProvider),
        };

        if (desktop.MainWindow is SetupWindow setupWindow)
        {
            setupWindow.Close();
        }

        desktop.MainWindow = mainWindow;
        mainWindow.Show();
    }

    private void InitializeDatabase()
    {
        if (ServiceProvider == null) return;

        var dataDir = ServiceProvider.GetRequiredService<IDataDirectory>();
        dataDir.EnsureLayout();

        var schema = ServiceProvider.GetRequiredService<ISchemaBootstrapper>();
        schema.EnsureInitializedAsync().Wait();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}