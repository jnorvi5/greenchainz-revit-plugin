import React from 'react';
import Link from 'next/link';

interface PricingTier {
  id: string;
  name: string;
  price: string;
  description: string;
  features: string[];
}

const pricingTiers: PricingTier[] = [
  {
    id: 'free',
    name: 'Free',
    price: '$0',
    description: 'Perfect for trying out GreenChainz.',
    features: ['3 Audits per month', 'Basic material browsing', 'Community support'],
  },
  {
    id: 'pro',
    name: 'Pro',
    price: '$49/mo',
    description: 'For professional architects and suppliers.',
    features: ['Unlimited Audits', 'Advanced analytics', 'Priority support', 'Export to PDF'],
  },
  {
    id: 'enterprise',
    name: 'Enterprise',
    price: 'Custom',
    description: 'Custom solutions for large organizations.',
    features: ['Custom integration', 'Dedicated account manager', 'SLA', 'API Access'],
  },
];

export default function BillingPage() {
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
<<<<<<< HEAD
          <div className="mt-4">
             <button
               onClick={handleManageSubscription}
               className="text-indigo-600 hover:text-indigo-500 hover:underline focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 rounded px-2 py-1 transition-colors flex items-center justify-center mx-auto"
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
            );
          })}
=======
          <div className="mt-6 inline-flex items-center gap-2 rounded-lg bg-blue-50 border border-blue-200 px-5 py-3">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              className="h-5 w-5 text-blue-600 flex-shrink-0"
              viewBox="0 0 20 20"
              fill="currentColor"
              aria-hidden="true"
            >
              <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
            </svg>
            <p className="text-sm text-blue-700">
              GreenChainz is available on the{' '}
              <a
                href="https://appsource.microsoft.com"
                target="_blank"
                rel="noopener noreferrer"
                className="font-semibold underline hover:text-blue-900"
              >
                Microsoft AppSource Marketplace
              </a>
              . Purchase and manage your subscription there.
            </p>
          </div>
        </div>

        <div className="mt-12 space-y-4 sm:mt-16 sm:space-y-0 sm:grid sm:grid-cols-3 sm:gap-6 lg:max-w-4xl lg:mx-auto xl:max-w-none xl:mx-0 xl:grid-cols-3">
          {pricingTiers.map((tier) => (
            <div
              key={tier.id}
              className="border border-gray-200 rounded-lg shadow-sm divide-y divide-gray-200 bg-white flex flex-col"
              aria-labelledby={`tier-name-${tier.id}`}
            >
              <div className="p-6 flex-1">
                <h3 id={`tier-name-${tier.id}`} className="text-lg leading-6 font-medium text-gray-900">{tier.name}</h3>
                <p id={`tier-desc-${tier.id}`} className="mt-4 text-sm text-gray-500">{tier.description}</p>
                <p className="mt-8 text-4xl font-extrabold text-gray-900">{tier.price}</p>
                <a
                  href="https://appsource.microsoft.com"
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-describedby={`tier-desc-${tier.id}`}
                  className="mt-8 w-full bg-indigo-600 border border-transparent rounded-md py-2 text-sm font-semibold text-white text-center hover:bg-indigo-700 transition-colors flex justify-center items-center focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-indigo-500"
                >
                  Get on Microsoft AppSource
                </a>
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
          ))}
>>>>>>> 039e306a47b2bc6544e95c271ca02a818ce678bf
        </div>

        <p className="mt-10 text-center text-sm text-gray-500">
          Questions about pricing?{' '}
          <a
            href="mailto:sales@greenchainz.com"
            className="font-medium text-indigo-600 hover:text-indigo-800 underline"
          >
            Contact sales
          </a>
        </p>
      </div>
    </main>
  );
}
