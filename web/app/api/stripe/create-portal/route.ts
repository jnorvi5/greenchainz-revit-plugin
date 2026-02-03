import { NextRequest, NextResponse } from 'next/server';
import Stripe from 'stripe';
import { createClient, SupabaseClient } from '@supabase/supabase-js';

// Lazy initialization to support testing and runtime configuration changes
let stripe: Stripe | null = null;
let supabase: SupabaseClient | null = null;

function getStripe() {
  if (stripe) return stripe;
  const key = process.env.STRIPE_SECRET_KEY;
  if (key) {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    stripe = new Stripe(key, { apiVersion: '2025-01-27.acacia' as any });
  }
  return stripe;
}

function getSupabase() {
  if (supabase) return supabase;
  const url = process.env.NEXT_PUBLIC_SUPABASE_URL;
  const key = process.env.SUPABASE_SERVICE_ROLE_KEY;
  if (url && key) {
    supabase = createClient(url, key);
  }
  return supabase;
}

export async function POST(req: NextRequest) {
  const stripeClient = getStripe();
  const supabaseClient = getSupabase();

  if (!stripeClient) {
    console.error('STRIPE_SECRET_KEY is not set');
    return NextResponse.json({ error: 'Server configuration error' }, { status: 500 });
  }

  if (!supabaseClient) {
    console.error('Supabase credentials not set');
    return NextResponse.json({ error: 'Server configuration error' }, { status: 500 });
  }

  try {
    // 1. Auth Check: Verify Bearer Token
    const authHeader = req.headers.get('authorization');
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
        return NextResponse.json({ error: 'Unauthorized: Missing token' }, { status: 401 });
    }
    const token = authHeader.split(' ')[1];

    // Validate the token and get the user
    const { data: { user }, error: authError } = await supabaseClient.auth.getUser(token);

    if (authError || !user || !user.email) {
        return NextResponse.json({ error: 'Unauthorized: Invalid token' }, { status: 401 });
    }

    // 2. Lookup Customer by Email (Prevent IDOR)
    // Instead of trusting client-provided customerId, we find the customer associated with the authenticated email.
    const customers = await stripeClient.customers.list({
        email: user.email,
        limit: 1
    });

    if (customers.data.length === 0) {
        return NextResponse.json({ error: 'No billing account found for this user' }, { status: 404 });
    }

    const customerId = customers.data[0].id;

    // 3. Create Portal Session with the verified customer ID
    const session = await stripeClient.billingPortal.sessions.create({
      customer: customerId,
      return_url: `${req.headers.get('origin')}/billing`,
    });

    return NextResponse.json({ url: session.url });
  } catch (err: unknown) {
    // Log detailed error for debugging but hide from client
    console.error('Stripe Portal Error:', err);
    return NextResponse.json({ error: 'Failed to create portal session' }, { status: 500 });
  }
}
