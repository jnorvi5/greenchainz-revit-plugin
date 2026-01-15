'use client';

import React, { useState } from 'react';

// Simple Spinner Component
const Spinner = ({ className }: { className?: string }) => (
  <svg
    className={`animate-spin h-5 w-5 ${className || ''}`}
    xmlns="http://www.w3.org/2000/svg"
    fill="none"
    viewBox="0 0 24 24"
    role="status"
    aria-label="loading"
  >
    <circle
      className="opacity-25"
      cx="12"
      cy="12"
      r="10"
      stroke="currentColor"
      strokeWidth="4"
    ></circle>
    <path
      className="opacity-75"
      fill="currentColor"
      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
    ></path>
  </svg>
);

interface PricingTier {
  id: string;
  name: string;
  price: number;
  description: string;
  features: string[];
  priceId?: string; // Stripe Price ID
  buttonText: string;
}

const pricingTiers: PricingTier[] = [
  {
    id: 'free',
    name: 'Free',
    price: 0,
    description: 'Perfect for trying out GreenChainz.',
    features: ['3 Audits per month', 'Basic material browsing', 'Community support'],
    buttonText: 'Get Started',
  },
  {
    id: 'pro',
    name: 'Pro',
    price: 49,
    description: 'For professional architects and suppliers.',
    features: ['Unlimited Audits', 'Advanced analytics', 'Priority support', 'Export to PDF'],
    priceId: 'price_1234567890', // Replace with real Stripe Price ID
    buttonText: 'Subscribe',
  },
  {
    id: 'enterprise',
    name: 'Enterprise',
    price: -1, // Indicates custom
    description: 'Custom solutions for large organizations.',
    features: ['Custom integration', 'Dedicated account manager', 'SLA', 'API Access'],
    buttonText: 'Contact Sales',
  },
];

export default function BillingPage() {
  const [loadingTierId, setLoadingTierId] = useState<string | null>(null);
  const [message, setMessage] = useState('');

  // Example customer ID (in a real app, get this from auth context)
  const customerId = 'cus_123456789';

  const handleManageSubscription = async () => {
    setLoadingTierId('manage');
    try {
      // Artificial delay for UX
      await new Promise(resolve => setTimeout(resolve, 500));

      const res = await fetch('/api/stripe/create-portal', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ customerId }),
      });
      const { url, error } = await res.json();
      if (error) {
        setMessage('Error accessing portal.');
      } else if (url) {
        window.location.href = url;
      }
    } catch (err) {
      console.error(err);
      setMessage('Error accessing portal.');
    } finally {
      // If redirecting, we generally want to keep loading, but since we handle errors:
      setLoadingTierId(null);
    }
  };

  const handleSubscribe = async (tier: PricingTier) => {
    if (tier.price === 0) {
      // Handle free tier logic (e.g., redirect to dashboard)
      setMessage('Free tier selected. Welcome!');
      return;
    }

    if (tier.price === -1) {
      // Handle enterprise logic (e.g., open contact form)
      window.location.href = 'mailto:sales@greenchainz.com';
      return;
    }

    setLoadingTierId(tier.id);
    setMessage('');

    try {
      // Add a small artificial delay to ensure the loading spinner is visible
      // even if the API responds instantly (good for UX perception too)
      await new Promise(resolve => setTimeout(resolve, 500));

      const res = await fetch('/api/stripe/create-checkout', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          priceId: tier.priceId,
        }),
      });

      const { url, error } = await res.json();

      if (error) {
        console.error('Error creating checkout session:', error);
        setMessage('Error initiating checkout. Please try again.');
        setLoadingTierId(null);
        return;
      }

      if (url) {
        window.location.href = url;
      } else {
        setMessage('Failed to get checkout URL.');
        setLoadingTierId(null);
      }
    } catch (err) {
      console.error('Checkout error:', err);
      setMessage('An unexpected error occurred.');
      setLoadingTierId(null);
    }
  };

  return (
    <main id="main-content" className="min-h-screen bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-7xl mx-auto">
        <div className="text-center">
          <h2 className="text-3xl font-extrabold text-gray-900 sm:text-4xl">Simple, Transparent Pricing</h2>
          <p className="mt-4 text-xl text-gray-600">Choose the plan that best fits your needs.</p>
          <div className="mt-4">
             <button
               onClick={handleManageSubscription}
               className="text-indigo-600 hover:text-indigo-500 hover:underline flex items-center justify-center mx-auto focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-indigo-500 rounded px-2 py-1 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
               disabled={!!loadingTierId}
             >
                 {loadingTierId === 'manage' && <Spinner className="mr-2 text-indigo-600" />}
                 Already a subscriber? Manage your subscription
             </button>
          </div>
        </div>

        {message && (
             <div
               className="mt-4 p-4 bg-blue-100 text-blue-700 rounded text-center"
               role="alert"
             >
                 {message}
             </div>
        )}

        <div className="mt-12 space-y-4 sm:mt-16 sm:space-y-0 sm:grid sm:grid-cols-3 sm:gap-6 lg:max-w-4xl lg:mx-auto xl:max-w-none xl:mx-0 xl:grid-cols-3">
          {pricingTiers.map((tier) => {
            const isThisLoading = loadingTierId === tier.id;
            const isAnyLoading = !!loadingTierId;

            return (
            <div key={tier.id} className="border border-gray-200 rounded-lg shadow-sm divide-y divide-gray-200 bg-white flex flex-col hover:shadow-lg hover:-translate-y-1 transition-all duration-300">
              <div className="p-6 flex-1">
                <h3 className="text-lg leading-6 font-medium text-gray-900">{tier.name}</h3>
                <p className="mt-4 text-sm text-gray-500">{tier.description}</p>
                <p className="mt-8">
                  <span className="text-4xl font-extrabold text-gray-900">
                    {tier.price >= 0 ? `$${tier.price}` : 'Custom'}
                  </span>
                  {tier.price >= 0 && <span className="text-base font-medium text-gray-500">/mo</span>}
                </p>

                <button
                  onClick={() => handleSubscribe(tier)}
                  disabled={isAnyLoading}
                  aria-busy={isThisLoading}
                  className={`mt-8 w-full bg-indigo-600 border border-transparent rounded-md py-2 text-sm font-semibold text-white text-center hover:bg-indigo-700 transition-colors flex justify-center items-center focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-indigo-500 ${isAnyLoading ? 'opacity-50 cursor-not-allowed' : ''}`}
                >
                  {isThisLoading ? (
                    <>
                      <Spinner className="mr-2 text-white" />
                      Processing...
                    </>
                  ) : (
                    tier.buttonText
                  )}
                </button>
              </div>
              <div className="pt-6 pb-8 px-6">
                <h4 className="text-sm font-medium text-gray-900 tracking-wide uppercase">What&apos;s included</h4>
                <ul className="mt-6 space-y-4">
                  {tier.features.map((feature) => (
                    <li key={feature} className="flex space-x-3">
                      <svg
                        className="flex-shrink-0 h-5 w-5 text-green-500"
                        xmlns="http://www.w3.org/2000/svg"
                        viewBox="0 0 20 20"
                        fill="currentColor"
                        aria-hidden="true"
                      >
                        <path
                          fillRule="evenodd"
                          d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                          clipRule="evenodd"
                        />
                      </svg>
                      <span className="text-sm text-gray-500">{feature}</span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          )})}
        </div>
      </div>
    </main>
  );
}
