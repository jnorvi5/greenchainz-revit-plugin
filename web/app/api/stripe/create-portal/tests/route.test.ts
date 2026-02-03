import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { NextRequest } from 'next/server';

// Mock Supabase
vi.mock('@supabase/supabase-js', () => ({
  createClient: vi.fn(() => ({
    auth: {
      getUser: vi.fn((token) => {
        if (token === 'valid-token') {
          return Promise.resolve({ data: { user: { email: 'test@example.com' } }, error: null });
        }
        return Promise.resolve({ data: { user: null }, error: new Error('Invalid token') });
      })
    }
  })),
}));

// Mock Stripe
const mockStripeCustomersList = vi.fn();
const mockStripePortalCreate = vi.fn();

vi.mock('stripe', () => {
  return {
    default: class MockStripe {
      customers = {
        list: mockStripeCustomersList
      };
      billingPortal = {
        sessions: {
          create: mockStripePortalCreate
        }
      };
    }
  };
});

describe('Stripe Portal API Security', () => {
  const originalEnv = process.env;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  let POST: any;

  beforeEach(async () => {
    vi.clearAllMocks();
    vi.resetModules(); // Reset module cache to ensure fresh state
    process.env = {
      ...originalEnv,
      STRIPE_SECRET_KEY: 'sk_test_123',
      NEXT_PUBLIC_SUPABASE_URL: 'https://test.supabase.co',
      SUPABASE_SERVICE_ROLE_KEY: 'service-role-key'
    };

    // Re-import the module to pick up the new env vars and reset lazy clients
    const route = await import('../route');
    POST = route.POST;
  });

  afterEach(() => {
    process.env = originalEnv;
  });

  it('should return 401 if authorization header is missing', async () => {
    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      body: JSON.stringify({}),
    });

    const res = await POST(req);
    expect(res.status).toBe(401);
    const data = await res.json();
    expect(data.error).toContain('Unauthorized');
  });

  it('should return 401 if token is invalid', async () => {
    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: { 'Authorization': 'Bearer invalid-token' },
      body: JSON.stringify({}),
    });

    const res = await POST(req);
    expect(res.status).toBe(401);
  });

  it('should return 404 if no customer found for email', async () => {
    mockStripeCustomersList.mockResolvedValueOnce({ data: [] });

    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: { 'Authorization': 'Bearer valid-token' },
      body: JSON.stringify({}),
    });

    const res = await POST(req);
    expect(res.status).toBe(404);
    expect(mockStripeCustomersList).toHaveBeenCalledWith(expect.objectContaining({
      email: 'test@example.com'
    }));
  });

  it('should create portal session if customer found', async () => {
    mockStripeCustomersList.mockResolvedValueOnce({ data: [{ id: 'cus_123' }] });
    mockStripePortalCreate.mockResolvedValueOnce({ url: 'https://billing.stripe.com/session/123' });

    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: {
        'Authorization': 'Bearer valid-token',
        'Origin': 'http://localhost:3000'
      },
      body: JSON.stringify({}),
    });

    const res = await POST(req);
    expect(res.status).toBe(200);
    const data = await res.json();
    expect(data.url).toBe('https://billing.stripe.com/session/123');

    // Verify it used the ID from list, not anything else
    expect(mockStripePortalCreate).toHaveBeenCalledWith(expect.objectContaining({
      customer: 'cus_123'
    }));
  });
});
