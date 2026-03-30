import { NextResponse } from 'next/server';
import pool from '@/utils/db';

export async function GET() {
  const checks: Record<string, string> = { status: 'ok' };

  try {
    const result = await pool.query('SELECT 1');
    checks.database = result.rows.length > 0 ? 'connected' : 'error';
  } catch {
    checks.database = 'unavailable';
  }

  const allHealthy = checks.database === 'connected';

  return NextResponse.json(checks, { status: allHealthy ? 200 : 503 });
}
