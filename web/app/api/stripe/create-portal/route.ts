import { NextRequest, NextResponse } from 'next/server';
import Stripe from 'stripe';
import { createClient } from '@supabase/supabase-js';

export async function POST(req: NextRequest) {
  try {
    // Initialize Stripe
    const stripeSecretKey = process.env.STRIPE_SECRET_KEY;
    if (!stripeSecretKey) {
      console.error('STRIPE_SECRET_KEY is not set');
      return NextResponse.json({ error: 'Server configuration error' }, { status: 500 });
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const stripe = new Stripe(stripeSecretKey, { apiVersion: '2025-01-27.acacia' as any });

    // 1. Security: Authenticate User via Supabase
    const authHeader = req.headers.get('authorization');
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return NextResponse.json({ error: 'Unauthorized: Missing token' }, { status: 401 });
    }
    const token = authHeader.split(' ')[1];

    const supabaseUrl = process.env.NEXT_PUBLIC_SUPABASE_URL;
    const supabaseAnonKey = process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY;

    if (!supabaseUrl || !supabaseAnonKey) {
      console.error('Supabase configuration missing');
      return NextResponse.json({ error: 'Server configuration error' }, { status: 500 });
    }

    const supabase = createClient(supabaseUrl, supabaseAnonKey);
    const { data: { user }, error: authError } = await supabase.auth.getUser(token);

    if (authError || !user || !user.email) {
      return NextResponse.json({ error: 'Unauthorized: Invalid token' }, { status: 401 });
    }

    // 2. Validate Input
    const { customerId } = await req.json();

    if (!customerId) {
        return NextResponse.json({ error: 'Customer ID is required' }, { status: 400 });
    }

    // 3. Security: Prevent IDOR (Insecure Direct Object Reference)
    // Ensure the Stripe customer email matches the authenticated user's email
    try {
        const customer = await stripe.customers.retrieve(customerId);

        if ((customer as Stripe.DeletedCustomer).deleted) {
             return NextResponse.json({ error: 'Customer not found' }, { status: 404 });
        }

        const stripeCustomer = customer as Stripe.Customer;

        if (stripeCustomer.email !== user.email) {
            console.warn(`Potential IDOR attempt: User ${user.email} tried to access customer ${customerId} (${stripeCustomer.email})`);
            return NextResponse.json({ error: 'Unauthorized access to customer' }, { status: 403 });
        }

        // 4. Create Portal Session
        const session = await stripe.billingPortal.sessions.create({
          customer: customerId,
          return_url: `${req.headers.get('origin')}/billing`,
        });

        return NextResponse.json({ url: session.url });

    } catch (stripeError: unknown) {
        // Handle Stripe specific errors or general fetch errors
        console.error('Stripe Error:', stripeError);
        return NextResponse.json({ error: 'Failed to verify customer' }, { status: 500 });
    }

  } catch (err: unknown) {
    console.error('Create Portal Error:', err);
    return NextResponse.json({ error: 'Internal Server Error' }, { status: 500 });
  }
}
