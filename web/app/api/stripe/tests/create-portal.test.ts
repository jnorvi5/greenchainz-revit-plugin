import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { NextRequest } from 'next/server';

// Mock Stripe
const { mockStripe } = vi.hoisted(() => {
  return {
    mockStripe: {
      billingPortal: {
        sessions: {
          create: vi.fn(),
        },
      },
    }
  }
});

vi.mock('stripe', () => {
  return {
    default: class {
      billingPortal = mockStripe.billingPortal;
      constructor() {}
    }
  };
});

// Mock Supabase
const mockGetUser = vi.fn();
const mockFrom = vi.fn();

vi.mock('@supabase/supabase-js', () => ({
  createClient: vi.fn(() => ({
    auth: {
      getUser: mockGetUser,
    },
    from: mockFrom,
  })),
}));

describe('Stripe Create Portal Endpoint Security', () => {
  const originalEnv = process.env;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  let POST: any;

  beforeEach(async () => {
    vi.resetModules();
    vi.clearAllMocks();

    // Set env vars BEFORE importing the module
    process.env = {
        ...originalEnv,
        STRIPE_SECRET_KEY: 'sk_test_123',
        NEXT_PUBLIC_SUPABASE_URL: 'https://example.supabase.co',
        SUPABASE_SERVICE_ROLE_KEY: 'service-key'
    };

    // Dynamically import the module so top-level code runs with new env vars
    const routeModule = await import('../create-portal/route');
    POST = routeModule.POST;

    // Default mock behavior
    mockStripe.billingPortal.sessions.create.mockResolvedValue({ url: 'https://billing.stripe.com/session/123' });
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
    mockGetUser.mockResolvedValue({ data: { user: null }, error: new Error('Invalid token') });

    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer invalid-token',
      },
      body: JSON.stringify({}),
    });

    const res = await POST(req);
    expect(res.status).toBe(401);
  });

  it('should return 404 if user profile is not found', async () => {
    mockGetUser.mockResolvedValue({ data: { user: { id: 'user_123' } }, error: null });

    // Mock Supabase chain for profile lookup: from().select().eq().single()
    const mockSingle = vi.fn().mockResolvedValue({ data: null, error: { message: 'Not found' } });
    const mockEq = vi.fn(() => ({ single: mockSingle }));
    const mockSelect = vi.fn(() => ({ eq: mockEq }));
    mockFrom.mockReturnValue({ select: mockSelect });

    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer valid-token',
      },
      body: JSON.stringify({}),
    });

    const res = await POST(req);
    expect(res.status).toBe(404);
    expect(mockFrom).toHaveBeenCalledWith('profiles');
    expect(mockEq).toHaveBeenCalledWith('id', 'user_123');
  });

  it('should return 400 if user has no stripe_customer_id', async () => {
    mockGetUser.mockResolvedValue({ data: { user: { id: 'user_123' } }, error: null });

    // Profile found but no customer ID
    const mockSingle = vi.fn().mockResolvedValue({ data: { stripe_customer_id: null }, error: null });
    const mockEq = vi.fn(() => ({ single: mockSingle }));
    const mockSelect = vi.fn(() => ({ eq: mockEq }));
    mockFrom.mockReturnValue({ select: mockSelect });

    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer valid-token',
      },
      body: JSON.stringify({}),
    });

    const res = await POST(req);
    expect(res.status).toBe(400);
    const data = await res.json();
    expect(data.error).toContain('No billing account found');
  });

  it('should create portal session with correct customer ID when authorized', async () => {
    mockGetUser.mockResolvedValue({ data: { user: { id: 'user_123' } }, error: null });

    // Profile found with customer ID
    const mockSingle = vi.fn().mockResolvedValue({ data: { stripe_customer_id: 'cus_999' }, error: null });
    const mockEq = vi.fn(() => ({ single: mockSingle }));
    const mockSelect = vi.fn(() => ({ eq: mockEq }));
    mockFrom.mockReturnValue({ select: mockSelect });

    const req = new NextRequest('http://localhost:3000/api/stripe/create-portal', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer valid-token',
        Origin: 'http://localhost:3000'
      },
      body: JSON.stringify({}),
    });

    const res = await POST(req);
    expect(res.status).toBe(200);
    const data = await res.json();
    expect(data.url).toBe('https://billing.stripe.com/session/123');

    // Verify Stripe called with correct customer ID from DB, NOT from request
    expect(mockStripe.billingPortal.sessions.create).toHaveBeenCalledWith({
      customer: 'cus_999',
      return_url: 'http://localhost:3000/billing'
    });
  });
});
