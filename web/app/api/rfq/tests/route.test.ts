import { describe, it, expect, vi } from 'vitest';
import { POST } from '../route';
import { NextRequest } from 'next/server';

// Mock Supabase
vi.mock('@supabase/supabase-js', () => ({
  createClient: vi.fn(() => ({
    from: vi.fn(() => ({
      insert: vi.fn(() => ({ error: null })),
    })),
  })),
}));

describe('RFQ API Endpoint Security', () => {
  it('should not expose error details on failure', async () => {
    // Malformed JSON to trigger parse error or validation error
    const req = {
      json: async () => { throw new Error('Simulated JSON parse error'); }
    } as unknown as NextRequest;

    const res = await POST(req);
    expect(res.status).toBe(500);
    const data = await res.json();

    expect(data.error).toBe('Failed to process RFQ');
    expect(data.details).toBeUndefined(); // Crucial security check
  });
});
