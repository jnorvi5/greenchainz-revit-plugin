export default function Home() {
  return (
    <main id="main-content" className="min-h-screen bg-white font-sans">
      {/* Nav */}
      <header className="border-b border-gray-100 px-6 py-4 flex items-center justify-between max-w-7xl mx-auto">
        <span className="text-xl font-bold text-green-700">GreenChainz</span>
        <nav className="flex items-center gap-6 text-sm font-medium text-gray-600">
          <a href="#features" className="hover:text-green-700 transition-colors">Features</a>
          <a href="#how-it-works" className="hover:text-green-700 transition-colors">How It Works</a>
          <a
            href="/dashboard"
            className="rounded-full bg-green-700 px-4 py-2 text-white hover:bg-green-800 transition-colors"
          >
            Dashboard
          </a>
        </nav>
      </header>

      {/* Hero */}
      <section className="max-w-4xl mx-auto px-6 py-24 text-center">
        <span className="inline-block rounded-full bg-green-50 px-4 py-1 text-sm font-medium text-green-700 mb-6">
          Revit Plugin · Microsoft AppSource
        </span>
        <h1 className="text-5xl font-extrabold tracking-tight text-gray-900 leading-tight mb-6">
          Sustainable materials,<br />
          <span className="text-green-700">built into your workflow.</span>
        </h1>
        <p className="text-xl text-gray-600 max-w-2xl mx-auto mb-10">
          GreenChainz connects your Revit models to verified low-carbon materials and suppliers —
          so you can design greener buildings without leaving your BIM environment.
        </p>
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <a
            href="https://appsource.microsoft.com"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center justify-center rounded-full bg-green-700 px-8 py-3 text-base font-semibold text-white hover:bg-green-800 transition-colors shadow-sm"
          >
            Get on Microsoft AppSource
          </a>
          <a
            href="/dashboard"
            className="inline-flex items-center justify-center rounded-full border border-gray-200 bg-white px-8 py-3 text-base font-semibold text-gray-700 hover:border-green-600 hover:text-green-700 transition-colors"
          >
            Open Dashboard
          </a>
        </div>
      </section>

      {/* Features */}
      <section id="features" className="bg-gray-50 py-20">
        <div className="max-w-6xl mx-auto px-6">
          <h2 className="text-3xl font-bold text-center text-gray-900 mb-12">
            Everything you need, inside Revit
          </h2>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            <div className="bg-white rounded-2xl p-8 shadow-sm border border-gray-100">
              <div className="w-12 h-12 rounded-xl bg-green-100 flex items-center justify-center mb-5">
                <svg className="w-6 h-6 text-green-700" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2} aria-hidden="true">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                </svg>
              </div>
              <h3 className="text-lg font-semibold text-gray-900 mb-2">Carbon Audit</h3>
              <p className="text-gray-500 text-sm leading-relaxed">
                Scan your Revit model, calculate embodied carbon for every material using EC3 data,
                and sync the report instantly to your GreenChainz dashboard.
              </p>
            </div>
            <div className="bg-white rounded-2xl p-8 shadow-sm border border-gray-100">
              <div className="w-12 h-12 rounded-xl bg-green-100 flex items-center justify-center mb-5">
                <svg className="w-6 h-6 text-green-700" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2} aria-hidden="true">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M4 6h16M4 10h16M4 14h16M4 18h16" />
                </svg>
              </div>
              <h3 className="text-lg font-semibold text-gray-900 mb-2">Browse Materials</h3>
              <p className="text-gray-500 text-sm leading-relaxed">
                Search a curated database of EPD-certified, low-carbon materials — concrete, steel,
                mass timber, glass, and more — and add them directly to your Revit project.
              </p>
            </div>
            <div className="bg-white rounded-2xl p-8 shadow-sm border border-gray-100">
              <div className="w-12 h-12 rounded-xl bg-green-100 flex items-center justify-center mb-5">
                <svg className="w-6 h-6 text-green-700" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2} aria-hidden="true">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" />
                </svg>
              </div>
              <h3 className="text-lg font-semibold text-gray-900 mb-2">Send RFQ</h3>
              <p className="text-gray-500 text-sm leading-relaxed">
                Select elements in Revit and send a Request for Quotation directly to matched
                sustainable suppliers — no spreadsheets, no manual email chains.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* How it works */}
      <section id="how-it-works" className="py-20 max-w-4xl mx-auto px-6">
        <h2 className="text-3xl font-bold text-center text-gray-900 mb-12">How it works</h2>
        <ol className="space-y-8">
          {[
            {
              step: '01',
              title: 'Install from AppSource',
              body: 'Find GreenChainz on the Microsoft AppSource Marketplace and install it to Revit 2024 or 2026 in one click.',
            },
            {
              step: '02',
              title: 'Open your Revit model',
              body: 'The GreenChainz tab appears automatically in the Revit ribbon — no configuration required.',
            },
            {
              step: '03',
              title: 'Run a Carbon Audit',
              body: 'Click "Carbon Audit" to scan your model. GreenChainz calculates embodied carbon using EC3 Building Transparency data and syncs the report to your dashboard.',
            },
            {
              step: '04',
              title: 'Source greener materials',
              body: 'Browse EPD-certified alternatives, select project elements, and send RFQs to verified sustainable suppliers — all from within Revit.',
            },
          ].map(({ step, title, body }) => (
            <li key={step} className="flex gap-6">
              <span className="flex-shrink-0 w-12 h-12 rounded-full bg-green-700 text-white text-sm font-bold flex items-center justify-center">
                {step}
              </span>
              <div>
                <h3 className="font-semibold text-gray-900 mb-1">{title}</h3>
                <p className="text-gray-500 text-sm leading-relaxed">{body}</p>
              </div>
            </li>
          ))}
        </ol>
      </section>

      {/* CTA */}
      <section className="bg-green-700 py-16 text-center px-6">
        <h2 className="text-3xl font-bold text-white mb-4">Ready to build greener?</h2>
        <p className="text-green-100 mb-8 max-w-xl mx-auto">
          GreenChainz is available on the Microsoft AppSource Marketplace.
          Install it for free and start your first carbon audit today.
        </p>
        <a
          href="https://appsource.microsoft.com"
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center justify-center rounded-full bg-white px-8 py-3 text-base font-semibold text-green-700 hover:bg-green-50 transition-colors shadow"
        >
          Get GreenChainz on AppSource
        </a>
      </section>

      {/* Footer */}
      <footer className="border-t border-gray-100 py-8 text-center text-sm text-gray-400">
        © {new Date().getFullYear()} GreenChainz.&nbsp;
        <a href="mailto:sales@greenchainz.com" className="hover:text-gray-600 underline">
          sales@greenchainz.com
        </a>
      </footer>
    </main>
  );
}
