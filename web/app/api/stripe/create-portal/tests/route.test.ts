import { describe, it, expect, vi, beforeEach, afterEach, Mock } from 'vitest';
import { NextRequest } from 'next/server';

// Mock Supabase
const mockGetUser = vi.fn();
vi.mock('@supabase/supabase-js', () => ({
  createClient: vi.fn(() => ({
    auth: {
      getUser: mockGetUser,
    },
  })),
}));

// Mock Stripe
const mockRetrieveCustomer = vi.fn();
const mockCreateSession = vi.fn();

vi.mock('stripe', () => {
  return {
    default: class MockStripe {
      billingPortal = {
        sessions: {
          create: mockCreateSession,
        },
      };
      customers = {
        retrieve: mockRetrieveCustomer,
      };
    },
  };
});

describe('Stripe Create Portal API Security', () => {
  const originalEnv = process.env;

  beforeEach(() => {
    vi.resetModules();
    process.env = {
      ...originalEnv,
      STRIPE_SECRET_KEY: 'sk_test_123',
      NEXT_PUBLIC_SUPABASE_URL: 'https://test.supabase.co',
      NEXT_PUBLIC_SUPABASE_ANON_KEY: 'test-key'
    };
    mockGetUser.mockReset();
    mockRetrieveCustomer.mockReset();
    mockCreateSession.mockReset();
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
    expect(data.error).toMatch(/unauthorized|missing/i);
  });

  it('should return 401 if token is invalid', async () => {
    const { POST } = await import('../route');
    mockGetUser.mockResolvedValue({ data: { user: null }, error: { message: 'Invalid token' } });

    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: { Authorization: 'Bearer invalid-token' },
      body: JSON.stringify({ customerId: 'cus_123' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(401);
  });

  it('should return 403 if customer email does not match user email', async () => {
    const { POST } = await import('../route');
    mockGetUser.mockResolvedValue({
      data: { user: { email: 'user@example.com' } },
      error: null
    });
    mockRetrieveCustomer.mockResolvedValue({
      id: 'cus_123',
      email: 'other@example.com',
      deleted: false
    });

    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: { Authorization: 'Bearer valid-token' },
      body: JSON.stringify({ customerId: 'cus_123' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(403);
    const data = await res.json();
    expect(data.error).toMatch(/unauthorized|access denied/i);
  });

  it('should return 403 if customer is deleted (no email)', async () => {
    const { POST } = await import('../route');
    mockGetUser.mockResolvedValue({
      data: { user: { email: 'user@example.com' } },
      error: null
    });
    mockRetrieveCustomer.mockResolvedValue({
        id: 'cus_123',
        deleted: true
    });

    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: { Authorization: 'Bearer valid-token' },
      body: JSON.stringify({ customerId: 'cus_123' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(403);
  });

  it('should return 200 and session url if authenticated and emails match', async () => {
    const { POST } = await import('../route');
    mockGetUser.mockResolvedValue({
      data: { user: { email: 'user@example.com' } },
      error: null
    });
    mockRetrieveCustomer.mockResolvedValue({
      id: 'cus_123',
      email: 'user@example.com',
      deleted: false
    });
    mockCreateSession.mockResolvedValue({ url: 'https://stripe.com/session' });

    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: { Authorization: 'Bearer valid-token' },
      body: JSON.stringify({ customerId: 'cus_123' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(200);
    const data = await res.json();
    expect(data.url).toBe('https://stripe.com/session');
  });
});
