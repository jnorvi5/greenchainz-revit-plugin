import { NextRequest, NextResponse } from 'next/server';
import Stripe from 'stripe';

const stripeSecretKey = process.env.STRIPE_SECRET_KEY;
const stripe = stripeSecretKey
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  ? new Stripe(stripeSecretKey, { apiVersion: '2025-12-15.clover' as any })
  : null;

export async function POST(req: NextRequest) {
  if (!stripe) {
    console.error('STRIPE_SECRET_KEY is not set');
    return NextResponse.json({ error: 'Server configuration error' }, { status: 500 });
  }

  try {
    const { customerId } = await req.json();

    if (!customerId) {
        return NextResponse.json({ error: 'Customer ID is required' }, { status: 400 });
    }

    const session = await stripe.billingPortal.sessions.create({
      customer: customerId,
      return_url: `${req.headers.get('origin')}/billing`,
    });

    return NextResponse.json({ url: session.url });
  } catch (err: unknown) {
    const errorMessage = err instanceof Error ? err.message : 'Unknown error';
    return NextResponse.json({ error: errorMessage }, { status: 500 });
  }
}
