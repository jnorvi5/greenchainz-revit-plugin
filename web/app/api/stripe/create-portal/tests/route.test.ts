import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { POST } from '../route';
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
      customers = {
        retrieve: mockRetrieveCustomer,
      };
      billingPortal = {
        sessions: {
          create: mockCreateSession,
        },
      };
    },
  };
});

describe('Stripe Portal API Security', () => {
  const originalEnv = process.env;

  beforeEach(() => {
    vi.resetModules();
    process.env = {
        ...originalEnv,
        STRIPE_SECRET_KEY: 'sk_test_123',
        NEXT_PUBLIC_SUPABASE_URL: 'https://example.supabase.co',
        NEXT_PUBLIC_SUPABASE_ANON_KEY: 'public-key'
    };
    mockGetUser.mockReset();
    mockRetrieveCustomer.mockReset();
    mockCreateSession.mockReset();
  });

  afterEach(() => {
    process.env = originalEnv;
  });

  it('should return 401 if authorization header is missing', async () => {
    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      body: JSON.stringify({ customerId: 'cus_123' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(401);
  });

  it('should return 401 if token is invalid', async () => {
    mockGetUser.mockResolvedValue({ data: { user: null }, error: new Error('Invalid token') });

    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: { Authorization: 'Bearer invalid-token' },
      body: JSON.stringify({ customerId: 'cus_123' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(401);
  });

  it('should return 403 if user email does not match customer email (IDOR)', async () => {
    // Mock authenticated user
    mockGetUser.mockResolvedValue({
      data: { user: { email: 'attacker@example.com' } },
      error: null
    });

    // Mock Stripe customer belonging to victim
    mockRetrieveCustomer.mockResolvedValue({
      id: 'cus_victim',
      email: 'victim@example.com',
      // Stripe customer object usually has email
    });

    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: { Authorization: 'Bearer valid-token' },
      body: JSON.stringify({ customerId: 'cus_victim' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(403);
    const data = await res.json();
    expect(data.error).toBe('Unauthorized access to customer');
  });

  it('should return 200 and session URL if authenticated and email matches', async () => {
    // Mock authenticated user
    mockGetUser.mockResolvedValue({
      data: { user: { email: 'user@example.com' } },
      error: null
    });

    // Mock Stripe customer
    mockRetrieveCustomer.mockResolvedValue({
      id: 'cus_user',
      email: 'user@example.com'
    });

    // Mock Session creation
    mockCreateSession.mockResolvedValue({
      url: 'https://billing.stripe.com/session/123'
    });

    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: { Authorization: 'Bearer valid-token' },
      body: JSON.stringify({ customerId: 'cus_user' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(200);
    const data = await res.json();
    expect(data.url).toBe('https://billing.stripe.com/session/123');
  });
});
