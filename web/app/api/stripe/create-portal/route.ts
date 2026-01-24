import { NextRequest, NextResponse } from 'next/server';

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export async function POST(req: NextRequest) {
  // 🛡️ Sentinel: Endpoint disabled due to IDOR vulnerability.
  // Requires robust session-based authentication before re-enabling.
  return NextResponse.json(
    { error: 'Billing service is temporarily disabled for maintenance.' },
    { status: 503 }
  );
}
