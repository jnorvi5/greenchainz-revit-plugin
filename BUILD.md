# GreenChainz Revit Plugin - Build Instructions

## Prerequisites

1. **Revit 2026** must be installed (provides API references)
2. **.NET 8.0 SDK** 
3. **Visual Studio 2022** (recommended) or `dotnet` CLI

## Building the Plugin

### Option 1: Visual Studio
1. Open `GreenChainz.Revit.sln` in Visual Studio 2022
2. Ensure the configuration is set to `Debug | x64`
3. Build the solution (Ctrl+Shift+B)

### Option 2: Command Line
```bash
cd src\GreenChainz.Revit
dotnet build -c Debug
```

## Deployment

After building successfully, run the deployment script:

```bash
cd src\GreenChainz.Revit
deploy.bat
```

This copies the plugin to: `%APPDATA%\Autodesk\Revit\Addins\2026\`

## Configuration (Optional)

To use live Autodesk SDA data instead of mock data, set environment variables:

```cmd
set AUTODESK_CLIENT_ID=your_client_id
set AUTODESK_CLIENT_SECRET=your_client_secret
```

Get credentials from the [Autodesk Developer Portal](https://aps.autodesk.com/).

## Plugin Features

| Feature | Command | Description |
|---------|---------|-------------|
| **Browse Materials** | `MaterialBrowserCmd` | Opens dockable panel to search sustainable materials |
| **Carbon Audit** | `CarbonAuditCommand` | Analyzes model for carbon footprint |
| **Send RFQ** | `SendRFQCommand` | Creates Request for Quote from selected elements |
| **About** | `AboutCommand` | Shows plugin version info |

## Project Structure

```
src/GreenChainz.Revit/
??? App.cs                    # Plugin entry point
??? Commands/                 # Revit ribbon commands
??? Models/                   # Data models
??? Services/                 # API connectors & business logic
?   ??? AutodeskAuthService.cs    # OAuth2 for Autodesk APIs
?   ??? SdaConnectorService.cs    # SDA Carbon API connector
?   ??? MaterialService.cs        # Material data provider
?   ??? AuditService.cs           # Carbon audit scanning
?   ??? IAutodeskToolConnector.cs # Interface for multi-tool support
??? UI/                       # WPF windows and panels
??? Utils/                    # Helpers and handlers
```

## Troubleshooting

### Build Error: "Could not locate assembly RevitAPI"
Ensure Revit 2026 is installed, or update the HintPath in the .csproj file to match your Revit installation path.

### Plugin doesn't appear in Revit
1. Check that `GreenChainz.Revit.addin` is in the addins folder
2. Verify the DLL path in the .addin file matches the actual location
3. Restart Revit completely

### Materials not loading
The plugin falls back to mock data if SDA credentials are not configured or the API is unavailable.

## Quick Start

```bash
# 1. Build
cd src\GreenChainz.Revit
dotnet build -c Debug

# 2. Deploy
deploy.bat

# 3. Start Revit 2026 and look for the GreenChainz tab!
