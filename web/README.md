# GreenChainz Revit Plugin — Web API

Next.js backend API for the GreenChainz Revit plugin. Handles audit results, RFQ processing, and supplier matching.

## Getting Started

```bash
npm install
npm run dev
```

Open [http://localhost:3000](http://localhost:3000).

## Environment Variables

Create a `.env` file:

```env
DATABASE_URL=postgresql://user:password@your-server.postgres.database.azure.com:5432/greenchainz?sslmode=require
GREENCHAINZ_API_SECRET=your-secret-here
```

## Deployment

Deployed via Azure Container Apps as part of the GreenChainz infrastructure.

## Testing

```bash
npm test
```
