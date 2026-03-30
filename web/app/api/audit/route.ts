import { NextRequest, NextResponse } from 'next/server';
import pool from '@/utils/db';
import crypto from 'crypto';

// Audit API Endpoint - Receives Carbon Audit results from Revit plugin
export async function POST(request: NextRequest) {
  try {
    // 🔒 Security Check: Validate Authorization Token
    const authHeader = request.headers.get('authorization');
    const apiSecret = process.env.GREENCHAINZ_API_SECRET;

    if (!apiSecret) {
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

    // Secure constant-time comparison to prevent timing attacks
    const tokenBuffer = Buffer.from(token);
    const secretBuffer = Buffer.from(apiSecret);

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

    // Save to database
    let savedToDb = false;
    let dbId = null;

    try {
      const result = await pool.query(
        `INSERT INTO audits (project_name, overall_score, summary, data_source, date, materials, recommendations, created_at)
         VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
         RETURNING id`,
        [
          auditData.project_name,
          auditData.overall_score,
          auditData.summary,
          auditData.data_source,
          auditData.date,
          JSON.stringify(auditData.materials),
          JSON.stringify(auditData.recommendations),
          auditData.created_at
        ]
      );

      if (result.rows[0]) {
        savedToDb = true;
        dbId = result.rows[0].id;
      }
    } catch {
      console.log('DB not configured or error connecting, continuing without DB');
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
      { error: 'Failed to process Audit' },
      { status: 500 }
    );
  }
}
