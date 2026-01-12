export default function Loading() {
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
            <div className="h-4 w-32 bg-gray-100 rounded animate-pulse mt-5"></div>
          </div>
        </div>
      </div>
    </div>
  );
}
