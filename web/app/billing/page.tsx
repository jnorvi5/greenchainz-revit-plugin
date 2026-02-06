'use client';

import React, { useState } from 'react';
import Link from 'next/link';

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
  const [message, setMessage] = useState<{ type: 'success' | 'error' | 'info'; text: string } | null>(null);

  // Example customer ID (in a real app, get this from auth context)
  const customerId = 'cus_123456789';

  const handleManageSubscription = async () => {
    setLoadingTierId('manage');
    try {
      const res = await fetch('/api/stripe/create-portal', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ customerId }),
      });
      const { url, error } = await res.json();
      if (error) {
        setMessage({ type: 'error', text: 'Error accessing portal.' });
      } else if (url) {
        window.location.href = url;
      }
    } catch (err) {
      console.error(err);
      setMessage({ type: 'error', text: 'Error accessing portal.' });
    } finally {
      setLoadingTierId(null);
    }
  };

  const handleSubscribe = async (tier: PricingTier) => {
    if (tier.price === 0) {
      // Handle free tier logic (e.g., redirect to dashboard)
      setMessage({ type: 'success', text: 'Free tier selected. Welcome!' });
      return;
    }

    if (tier.price === -1) {
      // Handle enterprise logic (e.g., open contact form)
      window.location.href = 'mailto:sales@greenchainz.com';
      return;
    }

    setLoadingTierId(tier.id);
    setMessage(null);

    try {
      // Add a small artificial delay to ensure the loading spinner is visible
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
        setMessage({ type: 'error', text: 'Error initiating checkout. Please try again.' });
        setLoadingTierId(null);
        return;
      }

      if (url) {
        window.location.href = url;
      } else {
        setMessage({ type: 'error', text: 'Failed to get checkout URL.' });
      }
    } catch (err) {
      console.error('Checkout error:', err);
      setMessage({ type: 'error', text: 'An unexpected error occurred.' });
    } finally {
      setLoadingTierId(null);
    }
  };

  return (
    <main id="main-content" className="min-h-screen bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-7xl mx-auto">
        <Link
          href="/dashboard"
          className="inline-flex items-center text-sm font-medium text-gray-500 hover:text-indigo-600 hover:underline transition-colors mb-6 focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-indigo-500 rounded"
          aria-label="Back to Dashboard"
          title="Return to your dashboard"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            className="h-4 w-4 mr-1"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            aria-hidden="true"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
          </svg>
          Back to Dashboard
        </Link>

        <div className="text-center">
          <h2 className="text-3xl font-extrabold text-gray-900 sm:text-4xl">Simple, Transparent Pricing</h2>
          <p className="mt-4 text-xl text-gray-600">Choose the plan that best fits your needs.</p>
          <div className="mt-4">
             <button
               onClick={handleManageSubscription}
               className="mx-auto flex items-center justify-center rounded-md px-4 py-2 text-sm font-medium text-indigo-600 transition-colors hover:bg-indigo-50 hover:text-indigo-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
               disabled={!!loadingTierId}
               aria-busy={loadingTierId === 'manage'}
               title="Manage your existing subscription and billing details"
             >
                 {loadingTierId === 'manage' && <Spinner className="mr-2 text-indigo-600" />}
                 Already a subscriber? Manage your subscription
             </button>
          </div>
        </div>

        {message && (
             <div
               className={`mt-4 p-4 rounded text-center flex items-center justify-center gap-2 ${
                 message.type === 'error' ? 'bg-red-50 text-red-700' :
                 message.type === 'success' ? 'bg-green-50 text-green-700' :
                 'bg-blue-50 text-blue-700'
               }`}
               role="alert"
             >
               {message.type === 'error' && (
                 <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                   <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                 </svg>
               )}
               {message.type === 'success' && (
                 <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                   <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                 </svg>
               )}
               {message.type === 'info' && (
                 <svg xmlns="http://www.w3.org/2000/svg" className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                   <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                 </svg>
               )}
               {message.text}
             </div>
        )}

        <div className="mt-12 space-y-4 sm:mt-16 sm:space-y-0 sm:grid sm:grid-cols-3 sm:gap-6 lg:max-w-4xl lg:mx-auto xl:max-w-none xl:mx-0 xl:grid-cols-3">
          {pricingTiers.map((tier) => {
            const isThisLoading = loadingTierId === tier.id;
            const isAnyLoading = !!loadingTierId;

            return (
            <div
              key={tier.id}
              className="border border-gray-200 rounded-lg shadow-sm divide-y divide-gray-200 bg-white flex flex-col hover:shadow-lg hover:-translate-y-1 transition-all duration-300"
              aria-labelledby={`tier-name-${tier.id}`}
            >
              <div className="p-6 flex-1">
                <h3 id={`tier-name-${tier.id}`} className="text-lg leading-6 font-medium text-gray-900">{tier.name}</h3>
                <p id={`tier-desc-${tier.id}`} className="mt-4 text-sm text-gray-500">{tier.description}</p>
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
                  aria-describedby={`tier-desc-${tier.id}`}
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
            );
          })}
        </div>
      </div>
    </main>
  );
}
