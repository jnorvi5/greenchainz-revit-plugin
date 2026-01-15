import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { POST } from '../route';
import { NextRequest } from 'next/server';

// Mock Supabase
vi.mock('@supabase/supabase-js', () => ({
  createClient: vi.fn(() => ({
    from: vi.fn(() => ({
      insert: vi.fn(() => ({
        select: vi.fn(() => ({
          single: vi.fn(() => ({ data: { id: 'mock-id' }, error: null })),
        })),
      })),
    })),
  })),
}));

describe('Audit API Endpoint Security', () => {
  const originalEnv = process.env;

  beforeEach(() => {
    vi.resetModules();
    process.env = { ...originalEnv, GREENCHAINZ_API_SECRET: 'test-secret' };
  });

  afterEach(() => {
    process.env = originalEnv;
  });

  it('should return 401 if authorization header is missing', async () => {
    const req = new NextRequest('http://localhost:3000/api/audit', {
      method: 'POST',
      body: JSON.stringify({ ProjectName: 'Test Project' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(401);
    const data = await res.json();
    expect(data.error).toContain('Missing or invalid Authorization header');
  });

  it('should return 401 if token is incorrect', async () => {
    const req = new NextRequest('http://localhost:3000/api/audit', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer wrong-token',
      },
      body: JSON.stringify({ ProjectName: 'Test Project' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(401);
    const data = await res.json();
    expect(data.error).toContain('Invalid token');
  });

  it('should return 500 if server secret is not configured', async () => {
    process.env.GREENCHAINZ_API_SECRET = ''; // Unset secret

    const req = new NextRequest('http://localhost:3000/api/audit', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer test-secret',
      },
      body: JSON.stringify({ ProjectName: 'Test Project' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(500);
    const data = await res.json();
    expect(data.error).toContain('Server misconfiguration');
  });

  it('should return 200 if token is correct', async () => {
    const req = new NextRequest('http://localhost:3000/api/audit', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer test-secret',
      },
      body: JSON.stringify({ ProjectName: 'Test Project' }),
    });

    const res = await POST(req);
    expect(res.status).toBe(200);
    const data = await res.json();
    expect(data.success).toBe(true);
  });
});
