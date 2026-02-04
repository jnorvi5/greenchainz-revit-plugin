import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { NextRequest } from 'next/server';

// Mock Supabase
vi.mock('@supabase/supabase-js', () => ({
  createClient: vi.fn(() => ({
    from: vi.fn(() => ({
      insert: vi.fn(() => ({ error: null })),
      select: vi.fn(() => ({
        eq: vi.fn(() => ({
            single: vi.fn(() => ({ data: { id: 'mock-id', project_name: 'Secret Project' }, error: null })),
        })),
      })),
    })),
  })),
}));

describe('RFQ API GET Endpoint Security', () => {
  const originalEnv = process.env;

  beforeEach(() => {
    vi.resetModules();
    process.env = {
      ...originalEnv,
      GREENCHAINZ_API_SECRET: 'test-secret',
      NEXT_PUBLIC_SUPABASE_URL: 'https://mock.supabase.co',
      SUPABASE_SERVICE_ROLE_KEY: 'mock-key'
    };
  });

  afterEach(() => {
    process.env = originalEnv;
  });

  it('should return 401 if authorization header is missing on GET', async () => {
    const { GET } = await import('../route');
    const req = new NextRequest('http://localhost:3000/api/rfq?id=123', {
      method: 'GET',
    });

    const res = await GET(req);
    // Security check: Should be rejected
    expect(res.status).toBe(401);
    const data = await res.json();
    expect(data.error).toContain('Unauthorized');
  });

  it('should return 401 if token is incorrect on GET', async () => {
    const { GET } = await import('../route');
    const req = new NextRequest('http://localhost:3000/api/rfq?id=123', {
      method: 'GET',
      headers: {
        Authorization: 'Bearer wrong-token',
      },
    });

    const res = await GET(req);
    // Security check: Should be rejected
    expect(res.status).toBe(401);
  });

  it('should return 200 if token is correct on GET', async () => {
    const { GET } = await import('../route');
    const req = new NextRequest('http://localhost:3000/api/rfq?id=123', {
      method: 'GET',
      headers: {
        Authorization: 'Bearer test-secret',
      },
    });

    const res = await GET(req);
    expect(res.status).toBe(200);
    const data = await res.json();
    expect(data.id).toBe('mock-id');
  });
});
