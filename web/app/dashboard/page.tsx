'use client';

import React, { useState, useEffect } from 'react';
import Link from 'next/link';
import DashboardSkeleton from './loading';

// Mock data fetcher
const fetchUserData = async () => {
  // In a real app, this would fetch from your API or Supabase
  return new Promise<{ plan: string; credits: number; maxCredits: number }>((resolve) => {
    setTimeout(() => {
      resolve({
        plan: 'Pro',
        credits: 150,
        maxCredits: 500,
      });
    }, 1500);
  });
};

export default function DashboardPage() {
  const [userData, setUserData] = useState<{ plan: string; credits: number; maxCredits: number } | null>(null);

  useEffect(() => {
    fetchUserData().then(setUserData);
  }, []);

  if (!userData) {
    return <DashboardSkeleton />;
  }

  // Calculate usage
  const usedCredits = userData.maxCredits - userData.credits;
  const percentage = Math.min(100, Math.max(0, (userData.credits / userData.maxCredits) * 100));

  // Determine color based on availability
  // < 25%: Red (Critical)
  // < 50%: Orange (Warning)
  // >= 50%: Green (Good)
  let progressColor = 'bg-green-600';
  if (percentage < 25) {
    progressColor = 'bg-red-600';
  } else if (percentage < 50) {
    progressColor = 'bg-orange-500';
  }

  return (
    // Added animate-in for smoother transition from loading skeleton
    <main id="main-content" className="min-h-screen bg-gray-100 p-8">
      <div className="max-w-4xl mx-auto">
        <h1 className="text-2xl font-bold mb-6 text-gray-900">Dashboard</h1>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* Plan Card */}
          <div className="bg-white p-6 rounded-lg shadow">
            <h2 className="text-lg font-semibold text-gray-700">Current Plan</h2>
            <p className="text-3xl font-bold text-indigo-600 mt-2">{userData.plan}</p>
            <Link
              href="/billing"
              className="mt-4 inline-flex items-center text-sm font-medium text-indigo-600 hover:text-indigo-800 transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500 focus-visible:ring-offset-2 rounded-md"
              aria-label="Change current plan"
            >
              Change Plan <span aria-hidden="true" className="ml-1">→</span>
            </Link>
          </div>

          {/* Credits Card */}
          <div className="bg-white p-6 rounded-lg shadow">
            <div className="flex justify-between items-baseline">
              <h2 className="text-lg font-semibold text-gray-700">Credits Balance</h2>
              <span className="text-sm text-gray-500">
                <span className="sr-only">Total credits: </span>
                {userData.maxCredits}
              </span>
            </div>

            <div className="flex items-baseline gap-2 mt-2">
              <p className="text-3xl font-bold text-green-600">
                {userData.credits}
              </p>
              <span className="text-sm text-gray-500">available</span>
            </div>

            {/* Accessible Progress Bar */}
            <div
              className="mt-4 mb-1"
              aria-describedby="usage-text"
            >
              <div className="flex justify-between text-xs text-gray-400 mb-1">
                <span>0</span>
                <span>{userData.maxCredits}</span>
              </div>
              <div
                className="w-full bg-gray-200 rounded-full h-2.5"
                role="progressbar"
                aria-valuenow={userData.credits}
                aria-valuemin={0}
                aria-valuemax={userData.maxCredits}
                aria-label="Credits balance"
              >
                <div
                  className={`${progressColor} h-2.5 rounded-full transition-all duration-500 ease-out`}
                  style={{ width: `${percentage}%` }}
                ></div>
              </div>
            </div>

            <p id="usage-text" className="text-sm text-gray-500 mt-2">
              You have used {usedCredits} credits this period
            </p>
          </div>
        </div>
      </div>
    </main>
  );
}
