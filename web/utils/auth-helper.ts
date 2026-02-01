import { NextRequest, NextResponse } from 'next/server';
import crypto from 'crypto';

export function validateApiSecret(request: NextRequest): NextResponse | null {
  const authHeader = request.headers.get('authorization');
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
  const tokenBuffer = Buffer.from(token);
  const secretBuffer = Buffer.from(EXPECTED_AUTH_TOKEN);

  // Use constant-time comparison to prevent timing attacks
  if (tokenBuffer.length !== secretBuffer.length || !crypto.timingSafeEqual(tokenBuffer, secretBuffer)) {
    return NextResponse.json(
      { error: 'Unauthorized: Invalid token' },
      { status: 401 }
    );
  }

  return null;
}
