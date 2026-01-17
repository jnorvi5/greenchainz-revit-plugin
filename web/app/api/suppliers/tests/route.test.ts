import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { POST } from '../route';
import { NextRequest } from 'next/server';

describe('Suppliers API Endpoint Security', () => {
  const originalEnv = process.env;

  beforeEach(() => {
    vi.resetModules();
    process.env = { ...originalEnv, GREENCHAINZ_API_SECRET: 'test-secret' };
  });

  afterEach(() => {
    process.env = originalEnv;
  });

  it('should return 401 if authorization header is missing', async () => {
    const req = new NextRequest('http://localhost:3000/api/suppliers', {
      method: 'POST',
      body: JSON.stringify({ supplierIds: ['sup-1'] }),
    });

    const res = await POST(req);
    expect(res.status).toBe(401);
    const data = await res.json();
    expect(data.error).toContain('Missing or invalid Authorization header');
  });

  it('should return 401 if token is incorrect', async () => {
    const req = new NextRequest('http://localhost:3000/api/suppliers', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer wrong-token',
      },
      body: JSON.stringify({ supplierIds: ['sup-1'] }),
    });

    const res = await POST(req);
    expect(res.status).toBe(401);
    const data = await res.json();
    expect(data.error).toContain('Invalid token');
  });

  it('should return 500 if server secret is not configured', async () => {
    process.env.GREENCHAINZ_API_SECRET = ''; // Unset secret

    const req = new NextRequest('http://localhost:3000/api/suppliers', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer test-secret',
      },
      body: JSON.stringify({ supplierIds: ['sup-1'] }),
    });

    const res = await POST(req);
    expect(res.status).toBe(500);
    const data = await res.json();
    expect(data.error).toContain('Server configuration error');
  });

  it('should return 200 if token is correct and payload is valid', async () => {
    const req = new NextRequest('http://localhost:3000/api/suppliers', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer test-secret',
      },
      body: JSON.stringify({ supplierIds: ['sup-1'] }),
    });

    const res = await POST(req);
    expect(res.status).toBe(200);
    const data = await res.json();
    expect(data.success).toBe(true);
  });

  it('should return 400 if supplierIds is missing or empty', async () => {
    const req = new NextRequest('http://localhost:3000/api/suppliers', {
      method: 'POST',
      headers: {
        Authorization: 'Bearer test-secret',
      },
      body: JSON.stringify({ supplierIds: [] }),
    });

    const res = await POST(req);
    expect(res.status).toBe(400);
    const data = await res.json();
    expect(data.error).toContain('No suppliers selected');
  });

  it('should not expose error details on unexpected failure', async () => {
    // Force an internal error by mocking request.json to fail
    const req = {
        headers: new Headers({
            'Authorization': 'Bearer test-secret'
        }),
      json: async () => { throw new Error('Simulated internal error'); }
    } as unknown as NextRequest;

    const res = await POST(req);
    expect(res.status).toBe(500);
    const data = await res.json();

    expect(data.error).toBe('Failed to send RFQ to suppliers');
    // Ensure no stack trace or internal error message is leaked
  });
});
