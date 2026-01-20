# GreenChainz AI Agent

Carbon Intelligence Agent for Revit BIM Models.

## Quick Start

### Run Locally (Development)
```bash
cd agent
pip install -r requirements.txt
uvicorn main:app --reload
```

### Run with Docker
```bash
cd agent

# Build
docker build -t greenchainz-agent .

# Run
docker run --rm -p 8000:8000 greenchainz-agent
```

### Test
```bash
# Health check
curl http://localhost:8000/health

# Score materials
curl -X POST http://localhost:8000/agent/infer \
  -H "Content-Type: application/json" \
  -d '{
    "task": "score_materials",
    "materials": [
      {"id": 1, "name": "Concrete 4000psi", "volume_m3": 100},
      {"id": 2, "name": "Structural Steel", "volume_m3": 10}
    ]
  }'
```

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check |
| `/agent/infer` | POST | Main inference (score + recommend) |
| `/agent/score` | POST | Score materials only |
| `/agent/recommend` | POST | Get swap recommendations |
| `/agent/ifc-mapping` | POST | Map to IFC property sets |

## Response Actions

The agent returns a list of actions to apply in Revit:

| Action Type | Description |
|-------------|-------------|
| `set_parameter` | Set a parameter value on an element |
| `flag_issue` | Flag a high-carbon issue |
| `recommend_swap` | Recommend a material swap |
| `note` | Add a note/comment |

## Parameters Set

| Parameter | Description |
|-----------|-------------|
| `GC_CarbonTag` | Carbon level: LowCarbon, MediumCarbon, HighCarbon |
| `GC_GWP` | Global Warming Potential value |
| `GC_Category` | Material category (Concrete, Steel, etc.) |
| `GC_IfcCategory` | IFC material type for export |
| `GC_Pset` | IFC Property Set name |
