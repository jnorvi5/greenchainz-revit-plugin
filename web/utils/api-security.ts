import { NextRequest } from 'next/server';
import crypto from 'crypto';

/**
 * Validates that the request contains the correct Bearer token matching the server's API secret.
 * This is used for server-to-server authentication.
 *
 * @param request The NextRequest object
 * @returns boolean True if authorized, false otherwise
 */
export function validateApiSecret(request: NextRequest): boolean {
  const authHeader = request.headers.get('authorization');
  // Read from env dynamically to support runtime config changes/tests
  const expectedSecret = process.env.GREENCHAINZ_API_SECRET;

  if (!expectedSecret) {
    console.error('SERVER CONFIG ERROR: GREENCHAINZ_API_SECRET is not set.');
    return false;
  }

  if (!authHeader || !authHeader.startsWith('Bearer ')) {
    return false;
  }

  const token = authHeader.split(' ')[1];

  if (!token) return false;

  const tokenBuffer = Buffer.from(token);
  const secretBuffer = Buffer.from(expectedSecret);

  // Use constant-time comparison to prevent timing attacks
  if (tokenBuffer.length !== secretBuffer.length) {
    return false;
  }

  return crypto.timingSafeEqual(tokenBuffer, secretBuffer);
}
