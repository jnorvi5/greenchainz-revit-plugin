import { NextRequest, NextResponse } from 'next/server';
import Stripe from 'stripe';
import { createClient } from '@supabase/supabase-js';

const stripeSecretKey = process.env.STRIPE_SECRET_KEY;
if (!stripeSecretKey) {
  // In a real app, you might want to log this but not crash the whole module scope immediately
  // if this route is imported elsewhere, but for a Next.js API route, this will likely error on build or first request.
  // We'll throw to be safe and strict as requested.
  throw new Error('Missing STRIPE_SECRET_KEY environment variable');
}

const stripe = new Stripe(stripeSecretKey, {
  apiVersion: '2025-12-15.clover' as any,
});

// Initialize Supabase client
// Note: In a real app, use the service role key for admin actions like updating user credits directly
const supabaseUrl = process.env.NEXT_PUBLIC_SUPABASE_URL;
const supabaseServiceKey = process.env.SUPABASE_SERVICE_ROLE_KEY;

if (!supabaseUrl || !supabaseServiceKey) {
  throw new Error('Missing Supabase environment variables (NEXT_PUBLIC_SUPABASE_URL or SUPABASE_SERVICE_ROLE_KEY)');
}

const supabase = createClient(supabaseUrl, supabaseServiceKey);

export async function POST(req: NextRequest) {
  const payload = await req.text();
  const signature = req.headers.get('stripe-signature');
  const webhookSecret = process.env.STRIPE_WEBHOOK_SECRET;

  if (!webhookSecret) {
    console.error('Missing STRIPE_WEBHOOK_SECRET environment variable');
    return NextResponse.json({ error: 'Server configuration error' }, { status: 500 });
  }

  let event: Stripe.Event;

  try {
    if (!signature) throw new Error('Missing stripe-signature header');
    event = stripe.webhooks.constructEvent(
      payload,
      signature,
      webhookSecret
    );
  } catch (err: any) {
    console.error(`Webhook signature verification failed: ${err.message}`);
    return NextResponse.json({ error: err.message }, { status: 400 });
  }

  // Handle the event
  switch (event.type) {
    case 'checkout.session.completed':
      const session = event.data.object as Stripe.Checkout.Session;
      await handleCheckoutSessionCompleted(session);
      break;
    // Add other event types as needed
    default:
      console.log(`Unhandled event type ${event.type}`);
  }

  return NextResponse.json({ received: true });
}

async function handleCheckoutSessionCompleted(session: Stripe.Checkout.Session) {
  console.log('Payment received for session:', session.id);

  // Example: Retrieve user based on customer email or client_reference_id
  // And update their credits or subscription status in Supabase

  const customerEmail = session.customer_details?.email;
  if (!customerEmail) return;

  // Logic to update user in Supabase
  // For demonstration:
  // 1. Find user by email
  // 2. Update their credits or plan

  // NOTE: This assumes a 'users' table and 'profiles' table exist in Supabase
  // In a real scenario, you would match the Supabase user ID with the Stripe customer email

  try {
      const { data: user, error: userError } = await supabase
        .from('users')
        .select('id')
        .eq('email', customerEmail)
        .single();

      if (userError) {
          console.error('Error fetching user:', userError);
          return;
      }

      if (user) {
        // Determine credits based on what was purchased (this logic depends on your price IDs)
        // For now, we assume a generic update for the 'Pro' plan which gives unlimited credits (or a large number)

        const { error: updateError } = await supabase
          .from('profiles')
          .update({
              subscription_status: 'active',
              plan: 'pro',
              credits: 1000 // Example: Reset or add credits
          })
          .eq('id', user.id);

        if (updateError) {
            console.error('Error updating profile:', updateError);
        } else {
            console.log(`Successfully updated subscription for user ${user.id}`);
            // Send confirmation email (implementation omitted)
        }
      } else {
          console.warn(`User with email ${customerEmail} not found in Supabase.`);
      }
  } catch (err) {
      console.error('Unexpected error in webhook handler:', err);
  }
}
