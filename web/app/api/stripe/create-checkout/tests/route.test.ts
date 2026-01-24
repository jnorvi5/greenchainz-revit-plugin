import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { NextRequest } from 'next/server';

describe('Create Checkout API Security', () => {
  const originalEnv = process.env;

  beforeEach(() => {
    vi.resetModules();
    process.env = { ...originalEnv };
  });

  afterEach(() => {
    process.env = originalEnv;
  });

  it('should return 503 Service Unavailable', async () => {
    const { POST } = await import('../route');
    const req = new NextRequest('http://localhost:3000/api/stripe/create-checkout', {
      method: 'POST',
      body: JSON.stringify({ priceId: 'price_123' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(503);
    const data = await res.json();
    expect(data.error).toContain('temporarily disabled');
  });
});
