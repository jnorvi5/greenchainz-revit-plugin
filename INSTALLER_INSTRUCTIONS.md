# GreenChainz Installer Instructions

## Prerequisites

1.  **Visual Studio 2019/2022** with .NET Desktop Development workload.
2.  **WiX Toolset v3.11**: Download and install from [wixtoolset.org](https://wixtoolset.org/releases/v3.11.2/releases/).
3.  **WiX Toolset Visual Studio Extension**: Install the extension for your version of Visual Studio (available in VS Marketplace).

## Building the Installer

1.  Open `GreenChainz.Revit.sln` in Visual Studio.
2.  Ensure the configuration is set to **Release** and Platform is **x64**.
3.  Right-click on the `GreenChainz.Revit.Installer` project in Solution Explorer and select **Build**.
4.  The output MSI file will be located at:
    `src/GreenChainz.Revit.Installer/bin/Release/GreenChainzSetup.msi`

## Installing

1.  Run `GreenChainzSetup.msi`.
2.  Follow the on-screen instructions.
3.  The plugin will be installed to:
    - `.addin` file: `%APPDATA%\Autodesk\Revit\Addins\2024\`
    - DLLs: `%APPDATA%\Autodesk\Revit\Addins\2024\GreenChainz\`
4.  A shortcut "GreenChainz for Revit 2024" will be created on the desktop (if Revit 2024 is installed).

## Uninstallation

1.  Go to **Add or Remove Programs** in Windows Settings.
2.  Find "GreenChainz for Revit 2024".
3.  Click **Uninstall**.
