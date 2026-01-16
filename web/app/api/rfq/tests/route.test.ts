import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { POST } from '../route';
import { NextRequest } from 'next/server';

// Mock Supabase
vi.mock('@supabase/supabase-js', () => ({
  createClient: vi.fn(() => ({
    from: vi.fn(() => ({
      insert: vi.fn(() => ({ error: null })),
      select: vi.fn(() => ({
        single: vi.fn(() => ({ data: { id: 'mock-id' }, error: null })),
      })),
    })),
  })),
}));

describe('RFQ API Endpoint Security', () => {
  const originalEnv = process.env;

  beforeEach(() => {
    vi.resetModules();
    process.env = { ...originalEnv, GREENCHAINZ_API_SECRET: 'test-secret' };
  });

  afterEach(() => {
    process.env = originalEnv;
  });

  it('should return 401 if authorization header is missing', async () => {
    const req = new NextRequest('http://localhost:3000/api/rfq', {
      method: 'POST',
      body: JSON.stringify({ projectName: 'Test Project', materials: [{ name: 'Steel' }] }),
    });

    const res = await POST(req);
    expect(res.status).toBe(401);
    const data = await res.json();
    expect(data.error).toContain('Unauthorized');
  });

  it('should return 401 if token is incorrect', async () => {
    const req = new NextRequest('http://localhost:3000/api/rfq', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer wrong-token',
      },
      body: JSON.stringify({ projectName: 'Test Project', materials: [{ name: 'Steel' }] }),
    });

    const res = await POST(req);
    expect(res.status).toBe(401);
  });

  it('should return 400 if materials array exceeds limit (DoS protection)', async () => {
    const manyMaterials = Array(101).fill({ name: 'Steel' });
    const req = new NextRequest('http://localhost:3000/api/rfq', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer test-secret',
      },
      body: JSON.stringify({ projectName: 'Test Project', materials: manyMaterials }),
    });

    const res = await POST(req);
    expect(res.status).toBe(400);
    const data = await res.json();
    expect(data.error).toContain('Too many materials');
  });

  it('should return 200 if token is correct and payload is valid', async () => {
    const req = new NextRequest('http://localhost:3000/api/rfq', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer test-secret',
      },
      body: JSON.stringify({ projectName: 'Test Project', materials: [{ name: 'Steel' }] }),
    });

    const res = await POST(req);
    expect(res.status).toBe(200);
    const data = await res.json();
    expect(data.success).toBe(true);
  });

  it('should not expose error details on failure', async () => {
    // Malformed JSON to trigger parse error or validation error
    const req = {
      json: async () => { throw new Error('Simulated JSON parse error'); },
      headers: new Headers({ 'Authorization': 'Bearer test-secret' })
      headers: { get: () => 'Bearer test-secret' },
      json: async () => { throw new Error('Simulated JSON parse error'); }
    } as unknown as NextRequest;

    const res = await POST(req);
    expect(res.status).toBe(500);
    const data = await res.json();

    expect(data.error).toBe('Failed to process RFQ');
    expect(data.details).toBeUndefined(); // Crucial security check
  });
});
