'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';

// Mock data fetcher
const fetchUserData = async () => {
  // In a real app, this would fetch from your API or Supabase
  return new Promise<{ plan: string; credits: number; maxCredits: number }>((resolve) => {
    setTimeout(() => {
      resolve({
        plan: 'Pro',
        credits: 150,
        maxCredits: 500, // Added maxCredits for progress bar context
      });
    }, 1500); // Increased delay to show off the skeleton
  });
};

function DashboardSkeleton() {
  return (
    <div className="min-h-screen bg-gray-100 p-8">
      <div className="max-w-4xl mx-auto">
        {/* Title Skeleton */}
        <div className="h-8 w-48 bg-gray-200 rounded animate-pulse mb-6"></div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* Card 1 Skeleton */}
          <div className="bg-white p-6 rounded-lg shadow">
            <div className="h-5 w-32 bg-gray-200 rounded animate-pulse mb-3"></div>
            <div className="h-10 w-24 bg-indigo-100 rounded animate-pulse mt-2"></div>
            <div className="h-4 w-28 bg-gray-100 rounded animate-pulse mt-5"></div>
          </div>

          {/* Card 2 Skeleton */}
          <div className="bg-white p-6 rounded-lg shadow">
            <div className="h-5 w-40 bg-gray-200 rounded animate-pulse mb-3"></div>
            <div className="h-10 w-24 bg-green-100 rounded animate-pulse mt-2"></div>
            {/* Progress bar skeleton */}
            <div className="w-full bg-gray-200 rounded-full h-2.5 mt-4">
              <div className="bg-gray-300 h-2.5 rounded-full animate-pulse" style={{ width: '45%' }}></div>
            </div>
            <div className="h-4 w-32 bg-gray-100 rounded animate-pulse mt-2"></div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function DashboardPage() {
  const [userData, setUserData] = useState<{ plan: string; credits: number; maxCredits: number } | null>(null);

  useEffect(() => {
    fetchUserData().then(setUserData);
  }, []);

  if (!userData) {
    return <DashboardSkeleton />;
  }

  // Calculate percentage for progress bar
  const percentage = Math.min(100, Math.max(0, (userData.credits / userData.maxCredits) * 100));

  return (
    <div className="min-h-screen bg-gray-100 p-8">
      <div className="max-w-4xl mx-auto">
        <h1 className="text-2xl font-bold mb-6">Dashboard</h1>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div className="bg-white p-6 rounded-lg shadow">
            <h2 className="text-lg font-semibold text-gray-700">Current Plan</h2>
            <p className="text-3xl font-bold text-indigo-600 mt-2">{userData.plan}</p>
            <Link href="/billing" className="text-sm text-gray-500 mt-4 block hover:underline hover:text-indigo-600 transition-colors">
              Change Plan
            </Link>
          </div>

          <div className="bg-white p-6 rounded-lg shadow">
            <div className="flex justify-between items-baseline">
              <h2 className="text-lg font-semibold text-gray-700">Credits Remaining</h2>
              <span className="text-sm text-gray-500">{userData.credits} / {userData.maxCredits}</span>
            </div>

            <p className="text-3xl font-bold text-green-600 mt-2">{userData.credits}</p>

            {/* Accessible Progress Bar */}
            <div
              className="w-full bg-gray-200 rounded-full h-2.5 mt-4 mb-1"
              role="progressbar"
              aria-valuenow={userData.credits}
              aria-valuemin={0}
              aria-valuemax={userData.maxCredits}
              aria-label="Credits usage"
            >
              <div
                className="bg-green-600 h-2.5 rounded-full transition-all duration-500 ease-out"
                style={{ width: `${percentage}%` }}
              ></div>
            </div>

            <p className="text-sm text-gray-500">Used for audits</p>
          </div>
        </div>
      </div>
    </div>
  );
}
