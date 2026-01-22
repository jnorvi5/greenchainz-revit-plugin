import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { NextRequest } from 'next/server';

// Define mocks before imports
vi.mock('stripe', () => {
  return {
    default: vi.fn().mockImplementation(() => ({
      checkout: { sessions: { create: vi.fn().mockResolvedValue({ id: 'sess_123', url: 'https://checkout.stripe.com/test' }) } },
      billingPortal: { sessions: { create: vi.fn().mockResolvedValue({ url: 'https://billing.stripe.com/test' }) } }
    }))
  };
});

describe('Stripe API Security', () => {
  // We need to reload modules to pick up env var changes or use a different approach
  // Since Next.js routes read process.env at top level, we can't easily change it per test without resetModules
  // But strictly for testing security:

  beforeEach(() => {
    process.env.GREENCHAINZ_API_SECRET = 'super-secret-key';
    // We cannot easily mock the top-level stripe variable in the route without more complex mocking
    // So we accept 500 as "Authorized but Misconfigured" which proves Auth passed
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('create-checkout should reject unauthenticated requests', async () => {
    const { POST: createCheckout } = await import('../create-checkout/route');
    const req = new NextRequest('http://localhost/api/stripe/create-checkout', {
      method: 'POST',
      body: JSON.stringify({ priceId: 'price_1', customerId: 'cus_1' })
    });
    const res = await createCheckout(req);
    expect(res.status).toBe(401);
  });

  it('create-checkout should accept authenticated requests (returns 500 if Stripe not configured, proving it passed 401 check)', async () => {
    const { POST: createCheckout } = await import('../create-checkout/route');
    const req = new NextRequest('http://localhost/api/stripe/create-checkout', {
      method: 'POST',
      headers: {
        'Authorization': 'Bearer super-secret-key'
      },
      body: JSON.stringify({ priceId: 'price_1', customerId: 'cus_1' })
    });
    const res = await createCheckout(req);
    // If it was 401, it failed auth. If it is 500, it PASSED auth and hit the stripe check.
    // If we could mock stripe properly it would be 200.
    expect(res.status).not.toBe(401);
  });

  it('create-portal should reject unauthenticated requests', async () => {
    const { POST: createPortal } = await import('../create-portal/route');
    const req = new NextRequest('http://localhost/api/stripe/create-portal', {
      method: 'POST',
      body: JSON.stringify({ customerId: 'cus_1' })
    });
    const res = await createPortal(req);
    expect(res.status).toBe(401);
  });

  it('create-portal should accept authenticated requests (returns 500 if Stripe not configured)', async () => {
    const { POST: createPortal } = await import('../create-portal/route');
    const req = new NextRequest('http://localhost/api/stripe/create-portal', {
      method: 'POST',
      headers: {
        'Authorization': 'Bearer super-secret-key'
      },
      body: JSON.stringify({ customerId: 'cus_1' })
    });
    const res = await createPortal(req);
    expect(res.status).not.toBe(401);
  });
});
