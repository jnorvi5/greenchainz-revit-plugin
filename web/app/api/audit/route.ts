import { NextRequest, NextResponse } from 'next/server';
import { createClient } from '@supabase/supabase-js';
import crypto from 'crypto';

// Initialize Supabase client
const supabaseUrl = process.env.NEXT_PUBLIC_SUPABASE_URL || '';
const supabaseKey = process.env.SUPABASE_SERVICE_ROLE_KEY || process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY || '';
const supabase = supabaseUrl && supabaseKey ? createClient(supabaseUrl, supabaseKey) : null;

// Audit API Endpoint - Receives Carbon Audit results from Revit plugin
export async function POST(request: NextRequest) {
  try {
    // 🔒 Security Check: Validate Authorization Token
    const authHeader = request.headers.get('authorization');
    // Read the secret from env inside the function to support mocking in tests
    const EXPECTED_AUTH_TOKEN = process.env.GREENCHAINZ_API_SECRET;

    if (!EXPECTED_AUTH_TOKEN) {
      console.error('SERVER CONFIG ERROR: GREENCHAINZ_API_SECRET is not set.');
      return NextResponse.json(
        { error: 'Server configuration error' },
        { status: 500 }
      );
    }

    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return NextResponse.json(
        { error: 'Unauthorized: Missing or invalid Authorization header' },
        { status: 401 }
      );
    }

    const token = authHeader.split(' ')[1];

    // Use constant-time comparison to prevent timing attacks
    // Secure constant-time comparison
    const tokenBuffer = Buffer.from(token);
    const secretBuffer = Buffer.from(EXPECTED_AUTH_TOKEN);

    if (tokenBuffer.length !== secretBuffer.length || !crypto.timingSafeEqual(tokenBuffer, secretBuffer)) {
      return NextResponse.json(
        { error: 'Unauthorized: Invalid token' },
        { status: 401 }
      );
    }

    const body = await request.json();

    // Validate request based on AuditResult model in plugin
    if (!body.ProjectName) {
      return NextResponse.json(
        { error: 'Missing required field: ProjectName' },
        { status: 400 }
      );
    }

    const auditData = {
      project_name: body.ProjectName,
      overall_score: body.OverallScore,
      summary: body.Summary,
      data_source: body.DataSource,
      date: body.Date || new Date().toISOString(),
      materials: body.Materials, // JSONB
      recommendations: body.Recommendations, // JSONB
      created_at: new Date().toISOString()
    };

    // Save to Supabase if available
    let savedToDb = false;
    let dbId = null;

    if (supabase) {
      try {
        const { data, error } = await supabase
          .from('audits')
          .insert(auditData)
          .select('id')
          .single();

        if (!error && data) {
          savedToDb = true;
          dbId = data.id;
        } else if (error) {
          console.error('Supabase DB Error:', error);
        }
      } catch {
        console.log('Supabase not configured or error connecting, continuing without DB');
      }
    }

    return NextResponse.json({
      success: true,
      message: savedToDb ? 'Audit saved successfully.' : 'Audit received (not saved to DB).',
      id: dbId,
      // Echo back for confirmation
      audit: {
          ProjectName: body.ProjectName,
          OverallScore: body.OverallScore,
          Date: auditData.date
      }
    });

  } catch (error) {
    console.error('Audit API Error:', error);
    return NextResponse.json(
      { error: 'Failed to process Audit' }, // Do not leak error details
      { status: 500 }
    );
  }
}
