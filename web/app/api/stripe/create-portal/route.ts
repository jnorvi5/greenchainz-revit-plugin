import { NextRequest, NextResponse } from 'next/server';
import Stripe from 'stripe';
import { createClient } from '@supabase/supabase-js';

const stripeSecretKey = process.env.STRIPE_SECRET_KEY;
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const stripe = stripeSecretKey
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  ? new Stripe(stripeSecretKey, { apiVersion: '2025-01-27.acacia' as any })
  : null;

// Initialize Supabase with Service Role Key for admin access to profiles
const supabaseUrl = process.env.NEXT_PUBLIC_SUPABASE_URL || '';
const supabaseKey = process.env.SUPABASE_SERVICE_ROLE_KEY || process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY || '';
const supabase = supabaseUrl && supabaseKey ? createClient(supabaseUrl, supabaseKey) : null;

export async function POST(req: NextRequest) {
  if (!stripe || !supabase) {
    console.error('Stripe or Supabase not configured');
    return NextResponse.json({ error: 'Server configuration error' }, { status: 500 });
  }

  try {
    // 1. Security Check: Authenticate User via Bearer Token
    const authHeader = req.headers.get('authorization');
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
       return NextResponse.json({ error: 'Unauthorized: Missing token' }, { status: 401 });
    }

    const token = authHeader.split(' ')[1];

    // Verify the token and get the user
    const { data: { user }, error: authError } = await supabase.auth.getUser(token);

    if (authError || !user) {
        console.warn('Invalid token provided to create-portal');
        return NextResponse.json({ error: 'Unauthorized: Invalid token' }, { status: 401 });
    }

    // 2. Security Check: Get Customer ID for Authenticated User
    // This prevents IDOR by ensuring we only access the authenticated user's data
    const { data: profile, error: profileError } = await supabase
        .from('profiles')
        .select('stripe_customer_id')
        .eq('id', user.id)
        .single();

    if (profileError || !profile) {
        console.error(`Profile fetch error for user ${user.id}:`, profileError);
        return NextResponse.json({ error: 'User profile not found' }, { status: 404 });
    }

    if (!profile.stripe_customer_id) {
        return NextResponse.json({ error: 'No billing account found for this user' }, { status: 400 });
    }

    // 3. Create Portal Session
    const session = await stripe.billingPortal.sessions.create({
      customer: profile.stripe_customer_id,
      return_url: `${req.headers.get('origin')}/billing`,
    });

    return NextResponse.json({ url: session.url });
  } catch (err: unknown) {
    const errorMessage = err instanceof Error ? err.message : 'Unknown error';
    console.error('Create Portal Error:', errorMessage);
    // Secure error handling: Don't leak stack traces
    return NextResponse.json({ error: 'Failed to create portal session' }, { status: 500 });
  }
}
