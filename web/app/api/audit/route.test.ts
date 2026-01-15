import { describe, it, expect, vi, beforeEach } from 'vitest';
import { POST } from './route';
import { NextRequest } from 'next/server';

// Mock Supabase
const mockSupabase = {
  from: vi.fn().mockReturnThis(),
  insert: vi.fn().mockReturnThis(),
  select: vi.fn().mockReturnThis(),
  single: vi.fn().mockResolvedValue({ data: { id: 123 }, error: null }),
};

vi.mock('@supabase/supabase-js', () => ({
  createClient: vi.fn(() => mockSupabase),
}));

// Mock process.env
const ORIGINAL_ENV = process.env;

describe('Audit API POST', () => {
  beforeEach(() => {
    vi.resetModules();
    process.env = { ...ORIGINAL_ENV };
    process.env.GREENCHAINZ_API_SECRET = 'secret123';
    process.env.NEXT_PUBLIC_SUPABASE_URL = 'https://example.com';
    process.env.SUPABASE_SERVICE_ROLE_KEY = 'key';
  });

  it('should return 401 if Authorization header is missing', async () => {
    const request = new NextRequest('http://localhost/api/audit', {
      method: 'POST',
      body: JSON.stringify({ ProjectName: 'Test Project' }),
    });

    const response = await POST(request);

    // In current vulnerable state, this will be 200. After fix, 401.
    // We expect 401 for a secure app.
    expect(response.status).toBe(401);
  });

  it('should return 401 if Authorization header is incorrect', async () => {
    const request = new NextRequest('http://localhost/api/audit', {
      method: 'POST',
      headers: {
        'Authorization': 'Bearer wrong_secret',
      },
      body: JSON.stringify({ ProjectName: 'Test Project' }),
    });

    const response = await POST(request);
    expect(response.status).toBe(401);
  });

  it('should return 200 if Authorization header is correct', async () => {
    const request = new NextRequest('http://localhost/api/audit', {
      method: 'POST',
      headers: {
        'Authorization': 'Bearer secret123',
      },
      body: JSON.stringify({ ProjectName: 'Test Project' }),
    });

    const response = await POST(request);
    expect(response.status).toBe(200);
  });
});
