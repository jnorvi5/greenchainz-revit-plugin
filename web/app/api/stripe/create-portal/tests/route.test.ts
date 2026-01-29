import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { NextRequest } from 'next/server';

// Mock Stripe
vi.mock('stripe', () => {
  return {
    default: class MockStripe {
      billingPortal = {
        sessions: {
          create: vi.fn().mockResolvedValue({ url: 'http://test-portal-url.com' }),
        },
      };
    },
  };
});

// Mock Supabase
vi.mock('@supabase/supabase-js', () => ({
  createClient: vi.fn(() => ({
    auth: {
      getUser: vi.fn().mockImplementation((token) => {
        if (token === 'valid-token') {
          return Promise.resolve({ data: { user: { id: 'test-user-id', email: 'test@example.com' } }, error: null });
        }
        return Promise.resolve({ data: { user: null }, error: { message: 'Invalid token' } });
      }),
    },
  })),
}));

describe('Stripe Create Portal API Security', () => {
  const originalEnv = process.env;

  beforeEach(() => {
    vi.resetModules();
    // Setup env vars
    process.env = {
      ...originalEnv,
      STRIPE_SECRET_KEY: 'test-stripe-key',
      NEXT_PUBLIC_SUPABASE_URL: 'https://test.supabase.co',
      NEXT_PUBLIC_SUPABASE_ANON_KEY: 'test-anon-key'
    };
  });

  afterEach(() => {
    process.env = originalEnv;
  });

  it('should return 401 if authorization header is missing', async () => {
    const { POST } = await import('../route');
    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      body: JSON.stringify({ customerId: 'cus_123' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(401);
    const data = await res.json();
    expect(data.error).toBe('Unauthorized: Missing or invalid Authorization header');
  });

  it('should return 401 if token is invalid', async () => {
    const { POST } = await import('../route');
    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer invalid-token'
      },
      body: JSON.stringify({ customerId: 'cus_123' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(401);
    const data = await res.json();
    expect(data.error).toBe('Unauthorized: Invalid token');
  });

  it('should return 200 if token is valid', async () => {
    const { POST } = await import('../route');
    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer valid-token'
      },
      body: JSON.stringify({ customerId: 'cus_123' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(200);
    const data = await res.json();
    expect(data.url).toBe('http://test-portal-url.com');
  });
});
