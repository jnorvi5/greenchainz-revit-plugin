import { NextRequest, NextResponse } from 'next/server';
import Stripe from 'stripe';
import { createClient } from '@supabase/supabase-js';

export async function POST(req: NextRequest) {
  const stripeSecretKey = process.env.STRIPE_SECRET_KEY;
  if (!stripeSecretKey) {
    console.error('STRIPE_SECRET_KEY is not set');
    return NextResponse.json({ error: 'Server configuration error' }, { status: 500 });
  }

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const stripe = new Stripe(stripeSecretKey, { apiVersion: '2025-01-27.acacia' as any });

  try {
    // 1. Auth Check
    const authHeader = req.headers.get('authorization');
    if (!authHeader) {
      return NextResponse.json({ error: 'Unauthorized: Missing Authorization header' }, { status: 401 });
    }

    const token = authHeader.replace('Bearer ', '');
    const supabaseUrl = process.env.NEXT_PUBLIC_SUPABASE_URL;
    const supabaseAnonKey = process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY;

    if (!supabaseUrl || !supabaseAnonKey) {
        console.error('Supabase not configured');
        return NextResponse.json({ error: 'Server configuration error' }, { status: 500 });
    }

    const supabase = createClient(supabaseUrl, supabaseAnonKey);
    const { data: { user }, error: authError } = await supabase.auth.getUser(token);

    if (authError || !user || !user.email) {
       return NextResponse.json({ error: 'Unauthorized: Invalid token' }, { status: 401 });
    }

    const { customerId } = await req.json();

    if (!customerId) {
        return NextResponse.json({ error: 'Customer ID is required' }, { status: 400 });
    }

    // 2. IDOR Check - Verify customer belongs to user
    const customer = await stripe.customers.retrieve(customerId);

    if (customer.deleted) {
         return NextResponse.json({ error: 'Unauthorized: Customer deleted' }, { status: 403 });
    }

    // Ensure strict email matching
    const customerEmail = (customer as Stripe.Customer).email;
    if (!customerEmail || customerEmail.toLowerCase() !== user.email.toLowerCase()) {
         return NextResponse.json({ error: 'Unauthorized: Access denied' }, { status: 403 });
    }

    const session = await stripe.billingPortal.sessions.create({
      customer: customerId,
      return_url: `${req.headers.get('origin')}/billing`,
    });

    return NextResponse.json({ url: session.url });
  } catch (err: unknown) {
    const errorMessage = err instanceof Error ? err.message : 'Unknown error';
    // Don't leak stack trace, but log it
    console.error('Create Portal Error:', err);
    return NextResponse.json({ error: errorMessage }, { status: 500 });
  }
}
