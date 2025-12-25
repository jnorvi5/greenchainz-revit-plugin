# GreenChainz Revit Plugin

A sustainable materials and carbon audit plugin for Autodesk Revit 2026.

## Features

### ?? Carbon Analysis
- **Carbon Audit** - Analyze your model's carbon footprint using EC3 real data
- **LEED v4.1 Calculator** - General LEED certification point calculator
- **LEED MRpc132** - Embodied Carbon Pilot Credit (CLF methodology)
- **LEED v5 BD+C** - Full LEED v5 analysis with carbon requirements

### ?? Materials & Procurement
- **Browse Materials** - Search sustainable materials from EC3 database
- **Send RFQ** - Request quotes from certified sustainable suppliers
- **Supplier Matching** - Auto-match materials to EPD-certified suppliers

### ?? Reporting
- **PDF Export** - Generate detailed carbon audit and LEED reports
- **CSV Export** - Export data for further analysis

## Installation

1. Build the solution:
```bash
cd src\GreenChainz.Revit
dotnet build -c Release
```

2. Copy files to Revit Addins folder:
```bash
copy bin\Release\net8.0-windows\*.dll "%APPDATA%\Autodesk\Revit\Addins\2026\"
copy GreenChainz.Revit.addin "%APPDATA%\Autodesk\Revit\Addins\2026\"
```

3. Restart Revit 2026

## API Keys (Optional)

For full functionality, set these environment variables:

```bash
# EC3 Building Transparency API (for real carbon factors)
set EC3_API_KEY=your_key

# Autodesk Platform Services (for SDA material data)
set AUTODESK_CLIENT_ID=your_client_id
set AUTODESK_CLIENT_SECRET=your_secret
```

## Web API (RFQ Backend)

The plugin connects to a Next.js API for RFQ processing:

```bash
cd web
npm install
npm run dev
```

API Endpoints:
- `POST /api/rfq` - Submit RFQ to suppliers
- `GET /api/suppliers` - Get sustainable supplier database

## Tech Stack

- **Revit Plugin**: C#, .NET 8, WPF
- **Web API**: Next.js, TypeScript, Supabase
- **Carbon Data**: EC3 Building Transparency API
- **PDF Generation**: iText7

## Suppliers Database

The plugin includes 15+ certified sustainable suppliers:

| Category | Suppliers |
|----------|-----------|
| Concrete | CarbonCure, Central Concrete, Solidia |
| Steel | Nucor, SSAB, CMC |
| Mass Timber | Structurlam, Nordic Structures |
| Glass | Guardian, Vitro |
| Insulation | Rockwool, Owens Corning |
| Aluminum | Novelis, Hydro |
| Gypsum | USG, CertainTeed |

## License

MIT License

## Support

For issues, open a GitHub issue or contact support@greenchainz.com
