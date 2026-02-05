import { NextRequest, NextResponse } from 'next/server';
import crypto from 'crypto';

/**
 * Validates the request against the GREENCHAINZ_API_SECRET.
 * Returns null if valid, or a NextResponse with the appropriate error.
 */
export function validateApiSecret(request: NextRequest): NextResponse | null {
  const authHeader = request.headers.get('authorization');
  // Read from process.env each time to allow mocking in tests
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

  // Create buffers for constant-time comparison
  const tokenBuffer = Buffer.from(token);
  const secretBuffer = Buffer.from(apiSecret);

  // Use constant-time comparison to prevent timing attacks
  if (tokenBuffer.length !== secretBuffer.length || !crypto.timingSafeEqual(tokenBuffer, secretBuffer)) {
    return NextResponse.json(
      { error: 'Unauthorized: Invalid token' },
      { status: 401 }
    );
  }

  return null;
}
