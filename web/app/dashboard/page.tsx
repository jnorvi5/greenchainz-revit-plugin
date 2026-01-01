import React from 'react';
import Link from 'next/link';

// Mock data fetcher
const fetchUserData = async () => {
  // In a real app, this would fetch from your API or Supabase
  return new Promise<{ plan: string; credits: number }>((resolve) => {
    setTimeout(() => {
      resolve({
        plan: 'Pro',
        credits: 150,
      });
    }, 1500); // Increased delay to show off the skeleton
  });
};

export default async function DashboardPage() {
  const userData = await fetchUserData();

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
            <h2 className="text-lg font-semibold text-gray-700">Credits Remaining</h2>
            <p className="text-3xl font-bold text-green-600 mt-2">{userData.credits}</p>
            <p className="text-sm text-gray-500 mt-1">Used for audits</p>
          </div>
        </div>
      </div>
    </div>
  );
}
