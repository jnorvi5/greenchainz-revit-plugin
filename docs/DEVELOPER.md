# GreenChainz Revit Plugin - Developer Documentation

## Overview

The GreenChainz Revit plugin integrates sustainable materials marketplace functionality directly into Autodesk Revit 2024. This plugin enables architects and engineers to browse sustainable materials, perform carbon audits, and send RFQs to suppliers without leaving the Revit environment.

### Plugin Architecture

```
GreenChainz.Revit/
├── App.cs                          # Main entry point (IExternalApplication)
│   └── OnStartup()                 # Creates ribbon tab and buttons
│   └── OnShutdown()                # Cleanup on Revit exit
├── Commands/
│   ├── BrowseMaterialsCommand.cs   # Browse sustainable materials
│   ├── CarbonAuditCommand.cs       # Carbon footprint analysis
│   └── SendRFQCommand.cs           # Send quotation requests
├── Properties/
│   └── AssemblyInfo.cs             # Assembly metadata
└── GreenChainz.Revit.addin         # Revit manifest file
```

**Key Components:**
- **App.cs**: Implements `IExternalApplication` and handles plugin initialization
- **Commands**: Implement `IExternalCommand` for user-triggered actions
- **Manifest**: XML file that registers the plugin with Revit

## Prerequisites

Before you begin development, ensure you have the following installed:

1. **Visual Studio 2022**
   - Download: https://visualstudio.microsoft.com/downloads/
   - Required workloads: .NET desktop development

2. **Autodesk Revit 2024**
   - Required for testing and API references
   - Installation path: `C:\Program Files\Autodesk\Revit 2024\`

3. **.NET Framework 4.8**
   - Included with Visual Studio 2022
   - Target framework for Revit 2024 plugins

4. **Git** (for version control)
   - Download: https://git-scm.com/downloads

## Project Structure

```
greenchainz-revit-plugin/
├── GreenChainz.Revit.sln           # Visual Studio solution file
├── src/
│   └── GreenChainz.Revit/
│       ├── GreenChainz.Revit.csproj    # C# project file
│       ├── App.cs                       # Main application entry
│       ├── Commands/                    # Command implementations
│       ├── Properties/                  # Assembly info
│       └── GreenChainz.Revit.addin     # Revit manifest
└── docs/
    ├── DEVELOPER.md                # This file
    ├── INSTALLATION.md             # End-user installation guide
    └── USER_GUIDE.md               # End-user guide
```

## Build Instructions

### Step 1: Clone the Repository

```bash
git clone https://github.com/jnorvi5/greenchainz-revit-plugin.git
cd greenchainz-revit-plugin
```

### Step 2: Open the Solution

1. Open Visual Studio 2022
2. File → Open → Project/Solution
3. Select `GreenChainz.Revit.sln`

### Step 3: Verify References

The project references the following Revit API DLLs:
- `RevitAPI.dll` - Core Revit API
- `RevitAPIUI.dll` - Revit UI API

**Expected location:** `C:\Program Files\Autodesk\Revit 2024\`

If Revit is installed in a different location:
1. Right-click the project → Properties
2. Reference Paths → Update paths to Revit installation
3. Or edit the `.csproj` file directly

### Step 4: Build the Solution

**Option A: Visual Studio GUI**
1. Select **Debug** or **Release** configuration
2. Ensure platform is set to **x64**
3. Build → Build Solution (Ctrl+Shift+B)

**Option B: Command Line**
```bash
# Debug build
msbuild GreenChainz.Revit.sln /p:Configuration=Debug /p:Platform=x64

# Release build
msbuild GreenChainz.Revit.sln /p:Configuration=Release /p:Platform=x64
```

**Expected Output:**
- `src/GreenChainz.Revit/bin/Debug/GreenChainz.Revit.dll`
- `src/GreenChainz.Revit/bin/Debug/GreenChainz.Revit.addin`

## Installation Instructions

### Manual Installation

1. **Build the plugin** (see Build Instructions above)

2. **Locate the Revit Addins folder:**
   ```
   %APPDATA%\Autodesk\Revit\Addins\2024\
   ```
   Full path example: `C:\Users\[YourUsername]\AppData\Roaming\Autodesk\Revit\Addins\2024\`

3. **Copy files:**
   - Copy `GreenChainz.Revit.dll` to the Addins folder
   - Copy `GreenChainz.Revit.addin` to the Addins folder

4. **Restart Revit** to load the plugin

### Automated Installation (PowerShell Script)

```powershell
# Copy DLL and manifest to Revit Addins folder
$addinsPath = "$env:APPDATA\Autodesk\Revit\Addins\2024"
$buildPath = "src\GreenChainz.Revit\bin\Debug"

Copy-Item "$buildPath\GreenChainz.Revit.dll" -Destination $addinsPath
Copy-Item "$buildPath\GreenChainz.Revit.addin" -Destination $addinsPath

Write-Host "Plugin installed successfully!"
```

## Testing Instructions

### Test 1: Plugin Loads Successfully

1. Launch Autodesk Revit 2024
2. Check for any error dialogs during startup
3. Look for the **GreenChainz** tab in the ribbon
4. Verify the **Sustainable Materials** panel appears

**Expected Result:** GreenChainz tab with 3 buttons visible

### Test 2: Browse Materials Command

1. Click the **Browse Materials** button
2. Verify a MessageBox appears with: "Browse Materials feature coming soon!"

**Expected Result:** MessageBox displayed, no errors

### Test 3: Carbon Audit Command

1. Open or create a Revit project
2. Click the **Carbon Audit** button
3. Verify a MessageBox appears showing the model name and "Carbon Audit feature coming soon!"

**Expected Result:** MessageBox with current model name displayed

### Test 4: Send RFQ Command

1. Click the **Send RFQ** button
2. Verify a MessageBox appears with: "Send RFQ feature coming soon!"

**Expected Result:** MessageBox displayed, no errors

### Checking for Errors

If the plugin fails to load:

1. **Check Revit Journals:**
   - Location: `%APPDATA%\Autodesk\Revit\Autodesk Revit 2024\Journals\`
   - Look for error messages related to GreenChainz

2. **Verify Manifest:**
   - Ensure `GreenChainz.Revit.addin` is in the Addins folder
   - Check XML syntax is valid

3. **Check DLL Dependencies:**
   - Ensure DLL is compiled for x64
   - Verify .NET Framework 4.8 is targeted

## Development Workflow

### Hot Reload (Manual)

Revit loads plugins at startup, so changes require a restart:

1. Close Revit
2. Build the solution in Visual Studio
3. Copy new DLL to Addins folder (use script or build event)
4. Restart Revit
5. Test changes

### Debugging

**Option 1: Attach to Process**
1. Start Revit 2024
2. In Visual Studio: Debug → Attach to Process
3. Select `Revit.exe`
4. Set breakpoints in your code
5. Execute commands in Revit

**Option 2: Start External Program**
1. Right-click project → Properties
2. Debug tab
3. Start external program: `C:\Program Files\Autodesk\Revit 2024\Revit.exe`
4. Press F5 to start debugging

### Post-Build Event (Auto-Copy)

Add to `.csproj` to automatically copy DLL and manifest after build:

```xml
<Target Name="AfterBuild">
  <Copy SourceFiles="$(TargetPath)" DestinationFolder="$(APPDATA)\Autodesk\Revit\Addins\2024\" />
  <Copy SourceFiles="$(ProjectDir)GreenChainz.Revit.addin" DestinationFolder="$(APPDATA)\Autodesk\Revit\Addins\2024\" />
</Target>
```

## Next Steps: Implementing Real Functionality

This foundation provides the basic structure. To implement real features:

### 1. Browse Materials Command

**TODO:**
- Create WPF window for materials browser
- Integrate with GreenChainz API (`https://greenchainz.com/api`)
- Add search and filter functionality
- Display material specifications, certifications, pricing
- Enable material selection and insertion into Revit model

**Key APIs:**
- `FilteredElementCollector` - Query existing materials
- `Material.Create()` - Add new materials to model
- HTTP client for API calls

### 2. Carbon Audit Command

**TODO:**
- Iterate through model elements and materials
- Calculate material volumes/quantities
- Query embodied carbon data from GreenChainz API
- Generate carbon footprint report
- Display results in WPF window with charts

**Key APIs:**
- `FilteredElementCollector` - Get all elements
- `Element.GetMaterialIds()` - Get materials per element
- `Parameter` - Extract quantities and properties

### 3. Send RFQ Command

**TODO:**
- Extract material specifications from model
- Create WPF form for RFQ details
- Generate RFQ document
- Send to GreenChainz API
- Display confirmation

**Key APIs:**
- `FilteredElementCollector` - Get materials
- `Material` properties - Extract specifications
- HTTP POST to API endpoint

## API Integration Guidelines

### Base URL
```
https://greenchainz.com/api
```

### Authentication
- Use API key-based authentication
- Store API key securely (user-level settings)
- Include in request headers: `Authorization: Bearer {API_KEY}`

### Common Endpoints (Example)
```
GET  /api/materials           # List materials
GET  /api/materials/{id}      # Get material details
POST /api/carbon-audit        # Submit carbon audit
POST /api/rfq                 # Submit RFQ
```

### Best Practices
- Implement async/await for API calls
- Add timeout handling (30 seconds)
- Cache frequently accessed data
- Handle network errors gracefully
- Show loading indicators in UI

### HTTP Client Example
```csharp
using System.Net.Http;
using System.Threading.Tasks;

public class GreenChainzApiClient
{
    private static readonly HttpClient client = new HttpClient();
    private const string BaseUrl = "https://greenchainz.com/api";
    
    public async Task<string> GetMaterialsAsync()
    {
        client.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_API_KEY");
        var response = await client.GetAsync($"{BaseUrl}/materials");
        response.EnsureSuccessStatusCode();
        return await response.ReadAsStringAsync();
    }
}
```

## Troubleshooting

### Plugin Not Loading

**Symptom:** GreenChainz tab doesn't appear in Revit ribbon

**Solutions:**
1. Check Revit journal files for errors:
   - `%APPDATA%\Autodesk\Revit\Autodesk Revit 2024\Journals\`
2. Verify manifest file is in Addins folder:
   - `%APPDATA%\Autodesk\Revit\Addins\2024\GreenChainz.Revit.addin`
3. Ensure DLL is in the same folder as manifest
4. Check manifest XML syntax is valid
5. Verify GUID in manifest matches AssemblyInfo.cs

### Build Errors - Missing References

**Symptom:** Cannot find RevitAPI or RevitAPIUI

**Solutions:**
1. Verify Revit 2024 is installed
2. Check reference paths in project properties
3. Update hint paths in `.csproj` to match Revit installation
4. Ensure platform is set to x64 (not AnyCPU)

### Runtime Errors

**Symptom:** Exceptions when clicking buttons

**Solutions:**
1. Enable debugging (Attach to Process)
2. Check exception message and stack trace
3. Verify all namespaces are imported
4. Ensure Transaction attribute is set correctly
5. Check for null references in document/element access

### Visual Studio Not Debugging

**Symptom:** Breakpoints not hit

**Solutions:**
1. Ensure Debug configuration is selected (not Release)
2. Generate debug symbols (.pdb files)
3. Verify Revit.exe is the correct process
4. Check "Enable Just My Code" is disabled in Debug options

## Resources and Links

### Official Documentation
- [Revit API Documentation](https://www.revitapidocs.com/)
- [Autodesk Developer Network](https://www.autodesk.com/developer-network/platform-technologies/revit)
- [Revit API Forums](https://forums.autodesk.com/t5/revit-api-forum/bd-p/160)

### Learning Resources
- [The Building Coder Blog](https://thebuildingcoder.typepad.com/)
- [Revit API Primer](https://github.com/DynamoDS/RevitAPIDocs)
- [YouTube: Revit API Tutorials](https://www.youtube.com/results?search_query=revit+api+tutorial)

### Tools
- [RevitLookup](https://github.com/jeremytammik/RevitLookup) - Revit database exploration tool
- [ReSharper](https://www.jetbrains.com/resharper/) - Code analysis and refactoring

### GreenChainz
- Website: https://greenchainz.com
- API Documentation: (To be added)
- Support: founder@greenchainz.com

## Contributing

When implementing new features:

1. **Follow the existing code structure**
   - Commands in `Commands/` folder
   - Each command implements `IExternalCommand`
   - Use `[Transaction(TransactionMode.Manual)]` attribute

2. **Handle exceptions properly**
   - Use try-catch blocks
   - Return meaningful error messages
   - Use `TaskDialog` for user-facing errors

3. **Code style**
   - Follow C# naming conventions
   - Add XML documentation comments
   - Keep methods focused and testable

4. **Testing**
   - Test in Revit 2024
   - Verify with sample projects
   - Check error handling paths

5. **Documentation**
   - Update this file when adding features
   - Add code comments for complex logic
   - Update USER_GUIDE.md for end users

## Version History

- **v1.0.0** (2025) - Initial foundation release
  - Basic plugin structure
  - Ribbon UI with 3 placeholder buttons
  - Build system and installation process

---

**For questions or support, contact:** founder@greenchainz.com
