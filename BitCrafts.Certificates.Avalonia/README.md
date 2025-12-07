# BitCrafts Certificates - Avalonia UI

A cross-platform desktop application for managing certificates built with Avalonia UI and MVVM pattern.

## Overview

This is a desktop UI application for the BitCrafts Certificate Authority that allows you to manage certificates through a native desktop application. It's built using:

- **Avalonia UI** - Cross-platform .NET UI framework
- **MVVM Pattern** - Model-View-ViewModel architecture
- **CommunityToolkit.Mvvm** - MVVM helpers and commands
- **Clean Architecture** - Separation of concerns across layers

## Features

- **Certificate List View**: Browse all certificates (server and client)
  - Filter by certificate type (All, Server, Client)
  - View certificate details
  - Revoke certificates
  - Delete certificates
  - Refresh list

- **Create Certificate View**: Create new certificates
  - Create server certificates with FQDN and IP addresses
  - Create client certificates with username and email
  - Real-time validation
  - Success/error messages

## Architecture

The Avalonia UI follows the MVVM (Model-View-ViewModel) pattern:

```
├── ViewModels/
│   ├── ViewModelBase.cs              # Base class for all ViewModels
│   ├── MainWindowViewModel.cs        # Main window orchestration
│   ├── CertificateListViewModel.cs   # Certificate list logic
│   └── CreateCertificateViewModel.cs # Certificate creation logic
├── Views/
│   ├── MainWindow.axaml              # Main application window
│   ├── CertificateListView.axaml    # Certificate list UI
│   └── CreateCertificateView.axaml  # Certificate creation UI
├── Models/                           # (empty - uses DTOs from Application layer)
├── App.axaml.cs                      # Application startup and DI configuration
└── Program.cs                        # Entry point and service configuration
```

### Dependency Injection

The application uses Microsoft.Extensions.DependencyInjection for dependency injection. All services from the main BitCrafts.Certificates project are registered and available to ViewModels.

Services configured:
- Data directory and database services
- PKI services (certificate generation)
- Application services (certificate and deployment operations)
- Audit logging
- Repository adapters

### ViewModels

**MainWindowViewModel**
- Orchestrates navigation between views
- Manages the current view

**CertificateListViewModel**
- Loads and displays certificates
- Handles filtering by type
- Manages certificate revocation and deletion
- Uses `ICertificateApplicationService` from the Application layer

**CreateCertificateViewModel**
- Handles form input for creating certificates
- Validates input
- Creates server or client certificates
- Uses `ICertificateApplicationService` from the Application layer

### Views

**MainWindow**
- Left navigation panel for switching between views
- Content area that displays the current view (DataTemplate-based)

**CertificateListView**
- DataGrid for displaying certificates
- Filter buttons for certificate types
- Detail panel showing selected certificate
- Action buttons (Revoke, Delete)

**CreateCertificateView**
- Radio buttons for certificate type selection
- Conditional forms based on certificate type
- Input validation
- Status messages

## Running the Application

### Prerequisites

- .NET 8.0 SDK
- Linux, macOS, or Windows

### Build

```bash
dotnet build BitCrafts.Certificates.Avalonia
```

### Run

```bash
dotnet run --project BitCrafts.Certificates.Avalonia
```

Or run the executable directly after building:

```bash
./BitCrafts.Certificates.Avalonia/bin/Debug/net8.0/BitCrafts.Certificates.Avalonia
```

### Configuration

The application uses the same environment variables as the web application:

- `BITCRAFTS_DATA_DIR` - Directory for certificate storage
- `BITCRAFTS_DB_PATH` - Path to SQLite database
- `BITCRAFTS_DOMAIN` - Domain for the Certificate Authority

If not set, defaults will be used.

## Targeting AlmaLinux

The application is designed to run on AlmaLinux and other Linux distributions. Avalonia UI provides native Linux support through:

- **X11** - Traditional Linux windowing system
- **Wayland** - Modern Linux display protocol
- **Framebuffer** - Direct rendering for headless scenarios

### Running on AlmaLinux

1. Install .NET 8.0 Runtime:
```bash
sudo dnf install dotnet-runtime-8.0
```

2. Install X11 dependencies (if using GUI):
```bash
sudo dnf install libX11 libICE libSM
```

3. Run the application:
```bash
./BitCrafts.Certificates.Avalonia
```

### Deployment Options

**Option 1: Self-Contained Deployment**
```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

**Option 2: Framework-Dependent Deployment**
```bash
dotnet publish -c Release -r linux-x64 --no-self-contained
```

The self-contained deployment includes the .NET runtime and is larger but doesn't require .NET to be installed on the target system.

## MVVM Pattern Benefits

1. **Separation of Concerns**: UI logic is separate from business logic
2. **Testability**: ViewModels can be unit tested without UI
3. **Maintainability**: Changes to UI don't affect business logic
4. **Data Binding**: Automatic synchronization between View and ViewModel
5. **Commands**: Declarative way to handle user actions

## Extending the Application

### Adding a New View

1. Create ViewModel in `ViewModels/`:
```csharp
public partial class MyNewViewModel : ViewModelBase
{
    // Properties and commands
}
```

2. Create View in `Views/`:
```xml
<UserControl x:DataType="vm:MyNewViewModel">
    <!-- UI elements -->
</UserControl>
```

3. Add DataTemplate to MainWindow.axaml:
```xml
<DataTemplate DataType="vm:MyNewViewModel">
    <views:MyNewView />
</DataTemplate>
```

4. Add navigation in MainWindowViewModel

### Adding New Features

- Add properties to ViewModels with `[ObservableProperty]` attribute
- Add commands with `[RelayCommand]` attribute
- Inject services through constructor
- Update Views to bind to new properties

## Troubleshooting

**Application won't start**
- Check that .NET 8.0 runtime is installed
- Verify environment variables are set correctly
- Check that data directory is writable

**UI doesn't display on Linux**
- Ensure X11 or Wayland is running
- Check that required libraries are installed
- Try setting `AVALONIA_SCREEN_SCALE_FACTOR=1`

**Database errors**
- Ensure BITCRAFTS_DATA_DIR exists and is writable
- Check that the database schema is initialized
- Verify SQLite is accessible

## License

This project is licensed under AGPLv3 (Affero GPL v3). See `../LICENSE` for the full text.
