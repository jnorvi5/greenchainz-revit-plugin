"""
GreenChainz AI Agent - Carbon Intelligence for Revit

This agent analyzes materials from Revit models and provides:
- Carbon scoring and tagging
- Material swap recommendations
- LEED compliance suggestions
- IFC property mapping

Run locally: uvicorn main:app --reload
Run in Docker: docker run -p 8000:8000 greenchainz-agent
"""

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import List, Optional, Dict, Any
import os
from dotenv import load_dotenv

load_dotenv()

app = FastAPI(
    title="GreenChainz AI Agent",
    description="Carbon Intelligence Agent for Revit BIM Models",
    version="1.0.0"
)

# CORS for local development
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ============================================
# Data Models
# ============================================

class Material(BaseModel):
    id: int
    name: str
    category: Optional[str] = None
    carbon_score: Optional[float] = None
    volume_m3: Optional[float] = None
    ifc_guid: Optional[str] = None
    ifc_category: Optional[str] = None

class AgentRequest(BaseModel):
    task: str  # "score_materials", "recommend_swaps", "leed_analysis", "ifc_mapping"
    materials: List[Material]
    project_zip: Optional[str] = None
    project_name: Optional[str] = None

class AgentAction(BaseModel):
    type: str  # "set_parameter", "flag_issue", "recommend_swap", "note"
    elementId: int
    parameterName: Optional[str] = None
    parameterValue: Optional[str] = None
    note: Optional[str] = None
    severity: Optional[str] = None  # "critical", "warning", "info"
    recommendation: Optional[Dict[str, Any]] = None

class AgentResponse(BaseModel):
    success: bool
    actions: List[AgentAction]
    message: str
    summary: Optional[Dict[str, Any]] = None

# ============================================
# Carbon Database (Baseline CLF v2021)
# ============================================

CARBON_BASELINES = {
    "concrete": {"gwp": 340, "unit": "kgCO2e/m3", "threshold_high": 400, "threshold_critical": 500},
    "steel": {"gwp": 1850, "unit": "kgCO2e/ton", "threshold_high": 2000, "threshold_critical": 2500},
    "aluminum": {"gwp": 8000, "unit": "kgCO2e/ton", "threshold_high": 9000, "threshold_critical": 12000},
    "wood": {"gwp": 110, "unit": "kgCO2e/m3", "threshold_high": 150, "threshold_critical": 200},
    "glass": {"gwp": 1500, "unit": "kgCO2e/m3", "threshold_high": 1800, "threshold_critical": 2200},
    "insulation": {"gwp": 50, "unit": "kgCO2e/m3", "threshold_high": 80, "threshold_critical": 120},
    "gypsum": {"gwp": 200, "unit": "kgCO2e/m3", "threshold_high": 250, "threshold_critical": 350},
    "brick": {"gwp": 200, "unit": "kgCO2e/m3", "threshold_high": 250, "threshold_critical": 350},
}

LOW_CARBON_ALTERNATIVES = {
    "concrete": [
        {"name": "CarbonCure Ready-Mix", "gwp": 238, "supplier": "CarbonCure Technologies", "savings": 30},
        {"name": "Solidia Low-Carbon Cement", "gwp": 180, "supplier": "Solidia Technologies", "savings": 47},
    ],
    "steel": [
        {"name": "Nucor EAF Steel", "gwp": 690, "supplier": "Nucor Corporation", "savings": 63},
        {"name": "SSAB Fossil-Free Steel", "gwp": 50, "supplier": "SSAB", "savings": 97},
    ],
    "aluminum": [
        {"name": "Novelis Recycled Aluminum", "gwp": 2000, "supplier": "Novelis", "savings": 75},
    ],
    "wood": [
        {"name": "Structurlam CLT (Carbon Negative)", "gwp": -500, "supplier": "Structurlam", "savings": 555},
    ],
}

# ============================================
# Helper Functions
# ============================================

def detect_category(name: str) -> str:
    """Detect material category from name"""
    name_lower = name.lower()
    
    if any(x in name_lower for x in ["concrete", "cement", "cmu"]):
        return "concrete"
    if any(x in name_lower for x in ["steel", "metal", "iron", "rebar"]):
        return "steel"
    if any(x in name_lower for x in ["aluminum", "aluminium"]):
        return "aluminum"
    if any(x in name_lower for x in ["wood", "timber", "lumber", "clt", "glulam"]):
        return "wood"
    if any(x in name_lower for x in ["glass", "glazing"]):
        return "glass"
    if any(x in name_lower for x in ["insulation", "rockwool", "fiberglass"]):
        return "insulation"
    if any(x in name_lower for x in ["gypsum", "drywall", "sheetrock"]):
        return "gypsum"
    if any(x in name_lower for x in ["brick", "masonry"]):
        return "brick"
    
    return "other"

def get_carbon_score(material: Material) -> tuple:
    """Calculate carbon score and severity"""
    category = detect_category(material.name)
    baseline = CARBON_BASELINES.get(category, {"gwp": 100, "threshold_high": 150, "threshold_critical": 250})
    
    # Use provided score or baseline
    gwp = material.carbon_score if material.carbon_score else baseline["gwp"]
    
    # Determine severity
    if gwp >= baseline.get("threshold_critical", 999):
        severity = "critical"
        tag = "HighCarbon"
    elif gwp >= baseline.get("threshold_high", 999):
        severity = "warning"
        tag = "MediumCarbon"
    else:
        severity = "info"
        tag = "LowCarbon"
    
    return category, gwp, severity, tag

# ============================================
# API Endpoints
# ============================================

@app.get("/health")
def health():
    """Health check endpoint"""
    return {"status": "ok", "service": "greenchainz-agent", "version": "1.0.0"}

@app.get("/")
def root():
    """API info"""
    return {
        "service": "GreenChainz AI Agent",
        "endpoints": {
            "/health": "Health check",
            "/agent/infer": "POST - Main inference endpoint",
            "/agent/score": "POST - Score materials only",
            "/agent/recommend": "POST - Get swap recommendations",
        }
    }

@app.post("/agent/infer", response_model=AgentResponse)
def agent_infer(req: AgentRequest):
    """
    Main agent inference endpoint.
    Analyzes materials and returns actions to apply in Revit.
    """
    actions: List[AgentAction] = []
    total_carbon = 0
    high_carbon_count = 0
    categories_found = set()
    
    for m in req.materials:
        category, gwp, severity, tag = get_carbon_score(m)
        categories_found.add(category)
        
        # Calculate total carbon if volume provided
        volume = m.volume_m3 or 1.0
        material_carbon = gwp * volume
        total_carbon += material_carbon
        
        if severity in ["critical", "warning"]:
            high_carbon_count += 1
        
        # Action 1: Set carbon tag parameter
        actions.append(AgentAction(
            type="set_parameter",
            elementId=m.id,
            parameterName="GC_CarbonTag",
            parameterValue=tag,
            severity=severity
        ))
        
        # Action 2: Set GWP value
        actions.append(AgentAction(
            type="set_parameter",
            elementId=m.id,
            parameterName="GC_GWP",
            parameterValue=str(round(gwp, 2)),
        ))
        
        # Action 3: Set category
        actions.append(AgentAction(
            type="set_parameter",
            elementId=m.id,
            parameterName="GC_Category",
            parameterValue=category.title(),
        ))
        
        # Action 4: Flag high-carbon materials
        if severity == "critical":
            alternatives = LOW_CARBON_ALTERNATIVES.get(category, [])
            best_alt = alternatives[0] if alternatives else None
            
            actions.append(AgentAction(
                type="flag_issue",
                elementId=m.id,
                note=f"HIGH CARBON: {m.name} ({gwp} kgCO2e). Consider low-carbon alternatives.",
                severity="critical",
                recommendation=best_alt
            ))
        
        # Action 5: Recommend swaps for high-carbon materials
        if severity in ["critical", "warning"] and req.task in ["recommend_swaps", "score_materials"]:
            alternatives = LOW_CARBON_ALTERNATIVES.get(category, [])
            if alternatives:
                best = alternatives[0]
                actions.append(AgentAction(
                    type="recommend_swap",
                    elementId=m.id,
                    note=f"Swap to {best['name']} from {best['supplier']} for {best['savings']}% carbon savings",
                    recommendation=best
                ))
    
    # Summary
    summary = {
        "total_materials": len(req.materials),
        "total_carbon_kgco2e": round(total_carbon, 0),
        "high_carbon_materials": high_carbon_count,
        "categories": list(categories_found),
        "actions_generated": len(actions)
    }
    
    message = f"Analyzed {len(req.materials)} materials. Found {high_carbon_count} high-carbon materials. Total embodied carbon: {total_carbon:,.0f} kgCO2e."
    
    return AgentResponse(
        success=True,
        actions=actions,
        message=message,
        summary=summary
    )

@app.post("/agent/score")
def agent_score(req: AgentRequest):
    """Score materials only (no swap recommendations)"""
    req.task = "score_only"
    return agent_infer(req)

@app.post("/agent/recommend")
def agent_recommend(req: AgentRequest):
    """Get swap recommendations for high-carbon materials"""
    req.task = "recommend_swaps"
    response = agent_infer(req)
    
    # Filter to only recommendation actions
    response.actions = [a for a in response.actions if a.type == "recommend_swap"]
    return response

@app.post("/agent/ifc-mapping")
def agent_ifc_mapping(req: AgentRequest):
    """Map materials to IFC property sets"""
    actions: List[AgentAction] = []
    
    for m in req.materials:
        category, gwp, severity, tag = get_carbon_score(m)
        
        # IFC Category mapping
        ifc_material_type = {
            "concrete": "IfcConcrete",
            "steel": "IfcSteel",
            "aluminum": "IfcAluminium",
            "wood": "IfcWood",
            "glass": "IfcGlass",
            "insulation": "IfcInsulation",
            "gypsum": "IfcGypsum",
            "brick": "IfcCite",
        }.get(category, "IfcMaterial")
        
        actions.append(AgentAction(
            type="set_parameter",
            elementId=m.id,
            parameterName="GC_IfcCategory",
            parameterValue=ifc_material_type,
        ))
        
        actions.append(AgentAction(
            type="set_parameter",
            elementId=m.id,
            parameterName="GC_Pset",
            parameterValue="Pset_EnvironmentalImpactIndicators",
        ))
    
    return AgentResponse(
        success=True,
        actions=actions,
        message=f"Mapped {len(req.materials)} materials to IFC property sets",
        summary={"materials_mapped": len(req.materials)}
    )


if __name__ == "__main__":
    import uvicorn
    port = int(os.getenv("PORT", 8000))
    uvicorn.run("main:app", host="0.0.0.0", port=port, reload=True)
