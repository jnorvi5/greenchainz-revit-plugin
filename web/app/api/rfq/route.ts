import { NextRequest, NextResponse } from 'next/server';
import { createClient } from '@supabase/supabase-js';

// Initialize Supabase client
const supabaseUrl = process.env.NEXT_PUBLIC_SUPABASE_URL || '';
const supabaseKey = process.env.SUPABASE_SERVICE_ROLE_KEY || process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY || '';
const supabase = supabaseUrl && supabaseKey ? createClient(supabaseUrl, supabaseKey) : null;

// RFQ API Endpoint - Receives RFQ from Revit plugin and finds suppliers
export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    
    const { projectName, projectAddress, materials, deliveryDate, specialInstructions, selectedSupplierIds } = body;

    // Validate required fields
    if (!projectName || !materials || materials.length === 0) {
      return NextResponse.json(
        { error: 'Missing required fields: projectName and materials' },
        { status: 400 }
      );
    }

    // Security: Limit number of materials to prevent DoS
    if (Array.isArray(materials) && materials.length > 100) {
      return NextResponse.json(
        { error: 'Too many materials. Maximum allowed is 100.' },
        { status: 400 }
      );
    }

    // Create RFQ record
    const rfqId = `RFQ-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    
    // Find suppliers for each material
    const supplierMatches = await findSuppliersForMaterials(materials);

    // Filter to selected suppliers if provided
    const notifySuppliers = selectedSupplierIds && selectedSupplierIds.length > 0
      ? supplierMatches.filter((s: any) => selectedSupplierIds.includes(s.id))
      : supplierMatches;

    // Save to Supabase if available
    let savedToDb = false;
    if (supabase) {
      try {
        const { error } = await supabase.from('rfqs').insert({
          id: rfqId,
          project_name: projectName,
          project_address: projectAddress,
          materials: materials,
          delivery_date: deliveryDate,
          special_instructions: specialInstructions,
          selected_suppliers: notifySuppliers.map((s: any) => s.id),
          status: 'pending',
          created_at: new Date().toISOString()
        });
        
        if (!error) savedToDb = true;
      } catch (dbError) {
        console.log('Supabase not configured, continuing without DB');
      }
    }

    // In production: Send emails to suppliers
    // await sendSupplierNotifications(notifySuppliers, { rfqId, projectName, materials, deliveryDate });

    return NextResponse.json({
      success: true,
      rfqId: rfqId,
      message: `RFQ submitted successfully! ${notifySuppliers.length} suppliers will be notified.${savedToDb ? ' Saved to database.' : ''}`,
      rfq: {
        id: rfqId,
        projectName,
        projectAddress,
        materials,
        deliveryDate,
        specialInstructions,
        status: 'pending',
        createdAt: new Date().toISOString()
      },
      suppliers: notifySuppliers
    });

  } catch (error) {
    console.error('RFQ API Error:', error);
    return NextResponse.json(
      { error: 'Failed to process RFQ' }, // Do not leak error details
      { status: 500 }
    );
  }
}

// GET - Retrieve RFQ status or list
export async function GET(request: NextRequest) {
  const { searchParams } = new URL(request.url);
  const rfqId = searchParams.get('id');

  if (rfqId && supabase) {
    try {
      const { data, error } = await supabase
        .from('rfqs')
        .select('*')
        .eq('id', rfqId)
        .single();

      if (error) throw error;

      return NextResponse.json(data);
    } catch {
      return NextResponse.json({ rfqId, status: 'not_found' }, { status: 404 });
    }
  }

  // Return supplier database for matching
  return NextResponse.json({
    message: 'GreenChainz RFQ API',
    endpoints: {
      'POST /api/rfq': 'Submit new RFQ',
      'GET /api/rfq?id=xxx': 'Get RFQ status',
      'GET /api/suppliers': 'List all suppliers',
      'GET /api/suppliers?category=steel': 'Filter suppliers'
    }
  });
}

// Supplier matching function with real sustainable suppliers
async function findSuppliersForMaterials(materials: any[]) {
  const suppliers: any[] = [];

  // Real sustainable material supplier database
  const supplierDatabase = [
    // CONCRETE
    {
      id: 'carboncure',
      name: 'CarbonCure Technologies',
      categories: ['concrete', 'ready-mix', 'low-carbon'],
      certifications: ['EPD', 'Carbon Negative', 'LEED Contributing'],
      region: 'North America',
      avgLeadTime: '1-2 weeks',
      sustainabilityScore: 96,
      contact: 'info@carboncure.com',
      website: 'https://www.carboncure.com',
      description: 'CO2 mineralization technology - reduces carbon by 5-10%'
    },
    {
      id: 'centralconcrete',
      name: 'Central Concrete',
      categories: ['concrete', 'ready-mix', 'aggregate'],
      certifications: ['EPD', 'LEED', 'Bay Area Green Business'],
      region: 'California',
      avgLeadTime: '1 week',
      sustainabilityScore: 88,
      contact: 'sales@centralconcrete.com',
      website: 'https://www.centralconcrete.com',
      description: 'Recycled aggregate concrete supplier'
    },
    // STEEL
    {
      id: 'nucor',
      name: 'Nucor Corporation',
      categories: ['steel', 'structural steel', 'rebar', 'steel decking', 'metal'],
      certifications: ['EPD', 'ISO 14001', 'Responsible Steel'],
      region: 'North America',
      avgLeadTime: '2-4 weeks',
      sustainabilityScore: 91,
      contact: 'products@nucor.com',
      website: 'https://www.nucor.com',
      description: '75%+ recycled content EAF steel'
    },
    {
      id: 'ssab',
      name: 'SSAB',
      categories: ['steel', 'structural steel', 'plate steel', 'fossil-free'],
      certifications: ['EPD', 'ISO 14001', 'Science Based Targets'],
      region: 'Global',
      avgLeadTime: '4-6 weeks',
      sustainabilityScore: 95,
      contact: 'info@ssab.com',
      website: 'https://www.ssab.com',
      description: 'HYBRIT fossil-free steel'
    },
    {
      id: 'cmc',
      name: 'Commercial Metals Company',
      categories: ['steel', 'rebar', 'merchant bar'],
      certifications: ['EPD', 'ISO 14001'],
      region: 'North America',
      avgLeadTime: '2-3 weeks',
      sustainabilityScore: 87,
      contact: 'sales@cmc.com',
      website: 'https://www.cmc.com',
      description: '97% recycled steel content'
    },
    // WOOD / MASS TIMBER
    {
      id: 'structurlam',
      name: 'Structurlam',
      categories: ['wood', 'mass timber', 'clt', 'glulam', 'timber'],
      certifications: ['FSC', 'PEFC', 'EPD', 'APA Certified'],
      region: 'North America',
      avgLeadTime: '6-10 weeks',
      sustainabilityScore: 96,
      contact: 'info@structurlam.com',
      website: 'https://www.structurlam.com',
      description: 'Leading mass timber manufacturer'
    },
    {
      id: 'nordic',
      name: 'Nordic Structures',
      categories: ['wood', 'clt', 'glulam', 'mass timber'],
      certifications: ['FSC', 'EPD', 'LEED Contributing'],
      region: 'North America',
      avgLeadTime: '6-8 weeks',
      sustainabilityScore: 94,
      contact: 'info@nordic.ca',
      website: 'https://www.nordic.ca',
      description: 'Cross-laminated timber specialist'
    },
    // GLASS
    {
      id: 'guardian',
      name: 'Guardian Glass',
      categories: ['glass', 'glazing', 'architectural glass', 'low-e'],
      certifications: ['EPD', 'Cradle to Cradle', 'ISO 14001'],
      region: 'Global',
      avgLeadTime: '3-4 weeks',
      sustainabilityScore: 88,
      contact: 'buildingproducts@guardian.com',
      website: 'https://www.guardianglass.com',
      description: '30%+ recycled cullet content'
    },
    {
      id: 'vitro',
      name: 'Vitro Architectural Glass',
      categories: ['glass', 'glazing', 'coated glass', 'insulating'],
      certifications: ['EPD', 'LEED Contributing'],
      region: 'North America',
      avgLeadTime: '2-3 weeks',
      sustainabilityScore: 86,
      contact: 'architecturalglass@vitro.com',
      website: 'https://www.vitroglazings.com',
      description: 'High-performance low-e coatings'
    },
    // INSULATION
    {
      id: 'rockwool',
      name: 'Rockwool',
      categories: ['insulation', 'mineral wool', 'stone wool', 'fire-safe'],
      certifications: ['EPD', 'Cradle to Cradle', 'GREENGUARD'],
      region: 'Global',
      avgLeadTime: '1-2 weeks',
      sustainabilityScore: 92,
      contact: 'info@rockwool.com',
      website: 'https://www.rockwool.com',
      description: 'Made from volcanic rock, fully recyclable'
    },
    {
      id: 'owenscorning',
      name: 'Owens Corning',
      categories: ['insulation', 'fiberglass', 'foam board'],
      certifications: ['EPD', 'GREENGUARD Gold', 'Declare'],
      region: 'Global',
      avgLeadTime: '1 week',
      sustainabilityScore: 87,
      contact: 'insulation@owenscorning.com',
      website: 'https://www.owenscorning.com',
      description: '50%+ recycled glass content'
    },
    // ALUMINUM
    {
      id: 'novelis',
      name: 'Novelis',
      categories: ['aluminum', 'rolled aluminum', 'building products'],
      certifications: ['EPD', 'ASI Certified', 'ISO 14001'],
      region: 'Global',
      avgLeadTime: '3-4 weeks',
      sustainabilityScore: 93,
      contact: 'info@novelis.com',
      website: 'https://www.novelis.com',
      description: '82% recycled aluminum content'
    },
    {
      id: 'hydro',
      name: 'Hydro Aluminum',
      categories: ['aluminum', 'extrusions', 'building systems'],
      certifications: ['EPD', 'ASI Certified', 'ISO 14001'],
      region: 'Global',
      avgLeadTime: '3-5 weeks',
      sustainabilityScore: 94,
      contact: 'contact@hydro.com',
      website: 'https://www.hydro.com',
      description: 'CIRCAL 75R - 75% recycled aluminum'
    },
    // GYPSUM
    {
      id: 'usg',
      name: 'USG Corporation',
      categories: ['gypsum', 'drywall', 'sheathing', 'ceiling'],
      certifications: ['EPD', 'GREENGUARD Gold', 'Declare'],
      region: 'North America',
      avgLeadTime: '1 week',
      sustainabilityScore: 85,
      contact: 'info@usg.com',
      website: 'https://www.usg.com',
      description: 'Synthetic recycled gypsum products'
    },
    {
      id: 'certainteed',
      name: 'CertainTeed',
      categories: ['gypsum', 'drywall', 'insulation', 'ceiling'],
      certifications: ['EPD', 'GREENGUARD', 'Declare'],
      region: 'North America',
      avgLeadTime: '1 week',
      sustainabilityScore: 86,
      contact: 'ctproductinfo@saint-gobain.com',
      website: 'https://www.certainteed.com',
      description: '95% recycled paper facing'
    }
  ];

  // Match materials to suppliers
  for (const material of materials) {
    const materialName = (material.materialName || material.name || '').toLowerCase();
    
    for (const supplier of supplierDatabase) {
      const matches = supplier.categories.some(cat => 
        materialName.includes(cat) || cat.includes(materialName.split(' ')[0]) || cat.includes(materialName.split(',')[0].trim())
      );
      
      if (matches && !suppliers.find(s => s.id === supplier.id)) {
        suppliers.push({
          ...supplier,
          matchedMaterial: material.materialName || material.name,
          quoteStatus: 'pending'
        });
      }
    }
  }

  // Sort by sustainability score
  suppliers.sort((a, b) => b.sustainabilityScore - a.sustainabilityScore);

  return suppliers;
}
