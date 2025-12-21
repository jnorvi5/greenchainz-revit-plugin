# GreenChainz API Client

This document provides information about the GreenChainz API client implementation for the Revit plugin.

## Overview

The API client provides a robust interface for communicating with the GreenChainz REST API, including:
- Material browsing and search
- Carbon audit submission and analysis
- Request for Quotation (RFQ) creation

## Architecture

### Core Components

#### Models (Request DTOs)
- `MaterialUsage` - Represents material usage data from Revit elements
- `AuditRequest` - Request for carbon audit analysis
- `RfqItem` - Individual item in an RFQ
- `RfqRequest` - Complete RFQ submission

#### Models (Response DTOs)
- `Material` - Sustainable material information
- `MaterialsResponse` - Paginated list of materials
- `MaterialAuditResult` - Carbon analysis for a specific material
- `AuditResponse` - Complete audit results with recommendations
- `RfqResponse` - RFQ submission confirmation

#### Services
- `ApiClient` - Main HTTP client for API communication
- `ApiException` - Custom exception for API errors
- `ApiConfig` - Configuration constants and token management

## Usage

### Basic Setup

```csharp
using GreenChainz.Revit.Services;
using GreenChainz.Revit.Models;

// Initialize the API client
string authToken = ApiConfig.LoadAuthToken(); // Or provide your token
var apiClient = new ApiClient(ApiConfig.BASE_URL, authToken);
```

### Browse Materials

```csharp
// Get all materials
var materials = await apiClient.GetMaterialsAsync();

// Filter by category
var concreteMateirals = await apiClient.GetMaterialsAsync(category: "Concrete");

// Search with text
var lowCarbonMaterials = await apiClient.GetMaterialsAsync(search: "low carbon");

// Combine filters
var results = await apiClient.GetMaterialsAsync(
    category: "Concrete", 
    search: "low carbon"
);
```

### Submit Carbon Audit

```csharp
// Create audit request
var auditRequest = new AuditRequest
{
    ProjectName = doc.Title,
    ProjectId = doc.ProjectInformation.UniqueId,
    Materials = extractedMaterials, // List<MaterialUsage>
    AuditDate = DateTime.UtcNow,
    RevitVersion = doc.Application.VersionNumber
};

// Submit for analysis
var auditResult = await apiClient.SubmitAuditAsync(auditRequest);

// Process results
Console.WriteLine($"Total Embodied Carbon: {auditResult.TotalEmbodiedCarbon} kgCO2e");
foreach (var result in auditResult.Results)
{
    Console.WriteLine($"{result.MaterialName}: {result.EmbodiedCarbon} kgCO2e");
}
```

### Submit RFQ

```csharp
// Create RFQ request
var rfqRequest = new RfqRequest
{
    ProjectName = "New Office Building",
    ContactEmail = "architect@example.com",
    ContactName = "John Doe",
    Items = new List<RfqItem>
    {
        new RfqItem
        {
            MaterialName = "Low Carbon Concrete",
            MaterialCategory = "Concrete",
            Quantity = 500,
            Unit = "m3",
            Specifications = "Must meet LEED Gold standards"
        }
    },
    Notes = "Required by Q2 2024",
    RequiredDate = new DateTime(2024, 6, 30)
};

// Submit RFQ
var rfqResponse = await apiClient.SubmitRfqAsync(rfqRequest);

Console.WriteLine($"RFQ ID: {rfqResponse.RfqId}");
Console.WriteLine($"Suppliers Notified: {rfqResponse.SuppliersNotified}");
```

### Error Handling

```csharp
try
{
    var materials = await apiClient.GetMaterialsAsync();
}
catch (ApiException ex)
{
    // Handle API-specific errors
    Console.WriteLine($"API Error ({ex.StatusCode}): {ex.Message}");
    
    if (ex.StatusCode == 401)
    {
        // Handle authentication error
        MessageBox.Show("Authentication failed. Please check your API token.");
    }
    else if (ex.StatusCode == 500)
    {
        // Handle server error
        MessageBox.Show("Server error. Please try again later.");
    }
}
catch (Exception ex)
{
    // Handle general errors
    Console.WriteLine($"Error: {ex.Message}");
}
```

### Best Practices

1. **Dispose the client after use:**
```csharp
using (var apiClient = new ApiClient(ApiConfig.BASE_URL, token))
{
    // Use the client
    var materials = await apiClient.GetMaterialsAsync();
}
```

2. **Handle authentication errors gracefully:**
```csharp
if (string.IsNullOrEmpty(authToken))
{
    MessageBox.Show("Please configure your GreenChainz API token.");
    return;
}
```

3. **Validate data before submitting:**
```csharp
if (auditRequest.Materials == null || auditRequest.Materials.Count == 0)
{
    MessageBox.Show("No materials found to audit.");
    return;
}
```

4. **Use async/await properly:**
```csharp
// In a command class
public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
{
    try
    {
        // Run async operation synchronously in Revit context
        var task = PerformAuditAsync(commandData);
        task.Wait();
        return Result.Succeeded;
    }
    catch (Exception ex)
    {
        message = ex.Message;
        return Result.Failed;
    }
}

private async Task PerformAuditAsync(ExternalCommandData commandData)
{
    using (var apiClient = new ApiClient(ApiConfig.BASE_URL, token))
    {
        var result = await apiClient.SubmitAuditAsync(request);
        // Process result
    }
}
```

## Configuration

### Setting the Authentication Token

The API client looks for the authentication token in the following order:

1. **Environment Variable:**
```
GREENCHAINZ_AUTH_TOKEN=your-jwt-token-here
```

2. **App Configuration (app.config):**
```xml
<appSettings>
    <add key="GreenChainzAuthToken" value="your-jwt-token-here" />
</appSettings>
```

3. **Direct Instantiation:**
```csharp
var apiClient = new ApiClient(ApiConfig.BASE_URL, "your-jwt-token-here");
```

## Testing

The project includes comprehensive unit tests using NUnit. Tests use mocked HTTP responses to validate:
- Successful API calls
- Error handling
- Request serialization
- Query parameter construction
- Authentication header inclusion

To run tests (requires Windows/.NET Framework 4.8):
```
dotnet test tests/GreenChainz.Revit.Tests/GreenChainz.Revit.Tests.csproj
```

## API Endpoints

- `GET /materials` - Browse materials with optional filters
- `POST /audit/extract-materials` - Submit carbon audit
- `POST /rfqs` - Submit Request for Quotation

## Dependencies

- **Newtonsoft.Json** (13.0.3) - JSON serialization/deserialization
- **System.Net.Http** - HTTP client functionality

## Error Codes

| Status Code | Description | ApiException Message |
|-------------|-------------|---------------------|
| 401 | Unauthorized | "Authentication failed. Please check your API token." |
| 403 | Forbidden | "Access forbidden. You don't have permission to access this resource." |
| 404 | Not Found | "The requested resource was not found." |
| 400 | Bad Request | "Bad request: [response body]" |
| 500 | Internal Server Error | "Server error occurred: [response body]" |
| 503 | Service Unavailable | "The API service is temporarily unavailable. Please try again later." |
| 429 | Too Many Requests | "Rate limit exceeded. Please wait before making more requests." |

## Future Enhancements

Potential improvements for future versions:
- Retry logic for transient failures
- Token refresh mechanism
- Request/response caching
- Logging framework integration
- Rate limiting handling
- Bulk operations support
- WebSocket support for real-time updates
