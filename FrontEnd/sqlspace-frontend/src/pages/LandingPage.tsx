import { Link } from 'react-router-dom'
import { Zap, Database, Terminal, ArrowRight, Server, CheckCircle2, Shield, BarChart2, History } from 'lucide-react'

export default function LandingPage() {
  return (
    <div className="min-h-screen bg-[#080809] text-zinc-200 selection:bg-sky-500/30 selection:text-white font-sans overflow-x-hidden">
      <div className="fixed inset-0 pointer-events-none z-0" style={{
        backgroundImage: 'linear-gradient(rgba(255,255,255,0.03) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.03) 1px, transparent 1px)',
        backgroundSize: '50px 50px'
      }} />
      <div className="absolute top-[-10%] left-[20%] w-150 h-150 rounded-full bg-sky-600/10 blur-[120px] pointer-events-none z-0" />
      <div className="absolute top-[20%] right-[-10%] w-100 h-100 rounded-full bg-cyan-600/5 blur-[100px] pointer-events-none z-0" />

      <nav className="fixed top-0 left-0 right-0 z-50 border-b border-white/5 bg-[#080809]/80 backdrop-blur-xl">
        <div className="max-w-7xl mx-auto px-6 h-16 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-lg bg-sky-500 text-white flex items-center justify-center shadow-[0_0_15px_rgba(14,165,233,0.5)]">
              <Zap className="w-4 h-4 fill-current" />
            </div>
            <span className="font-bold text-lg text-white tracking-tight">SqlSpace</span>
          </div>

          <div className="hidden md:flex items-center gap-8 text-sm font-medium text-zinc-400">
            <a href="#features" className="hover:text-white transition-colors">Features</a>
            <a href="#how" className="hover:text-white transition-colors">How it works</a>
            <a href="#databases" className="hover:text-white transition-colors">Databases</a>
          </div>

          <div className="flex items-center gap-4">
            <Link to="/login" className="text-sm font-medium text-zinc-300 hover:text-white transition-colors">
              Sign in
            </Link>
            <Link
              to="/register"
              className="text-sm font-semibold bg-white text-black px-4 py-2 rounded-full hover:bg-zinc-200 transition-transform hover:scale-105 active:scale-95 shadow-[0_0_20px_rgba(255,255,255,0.1)]"
            >
              Get started free
            </Link>
          </div>
        </div>
      </nav>

      {/* Hero */}
      <div className="relative pt-32 pb-20 lg:pt-48 lg:pb-32 px-6 z-10 max-w-7xl mx-auto flex flex-col items-center text-center">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full border border-sky-500/30 bg-sky-500/10 text-sky-300 text-xs font-semibold uppercase tracking-wider mb-8">
          <span className="relative flex h-2 w-2">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-sky-400 opacity-75" />
            <span className="relative inline-flex rounded-full h-2 w-2 bg-sky-500" />
          </span>
          Multi-database support now available
        </div>

        <h1 className="text-5xl lg:text-7xl font-bold text-white tracking-tight leading-[1.1] max-w-4xl">
          Talk to your database. <br />
          <span className="text-transparent bg-clip-text bg-linear-to-r from-sky-400 via-cyan-400 to-teal-400">
            No SQL required.
          </span>
        </h1>

        <p className="mt-6 text-lg lg:text-xl text-zinc-400 max-w-2xl leading-relaxed">
          Type a question in plain English. Get optimized SQL, live results, and auto-generated charts — in seconds. Designed for modern engineering teams.
        </p>

        <div className="mt-10 flex flex-col sm:flex-row items-center gap-4">
          <Link
            to="/register"
            className="h-12 px-8 rounded-full bg-sky-600 text-white font-semibold flex items-center justify-center gap-2 hover:bg-sky-500 hover:shadow-[0_0_30px_rgba(14,165,233,0.4)] transition-all transform hover:-translate-y-0.5 active:translate-y-0"
          >
            Start building for free <ArrowRight className="w-4 h-4" />
          </Link>
          <Link
            to="/login"
            className="h-12 px-8 rounded-full bg-[#18181b] border border-white/10 text-white font-semibold flex items-center justify-center gap-2 hover:bg-white/5 transition-all"
          >
            Sign in
          </Link>
        </div>

        {/* Demo window */}
        <div className="mt-20 w-full max-w-5xl rounded-2xl border border-white/10 bg-[#0d0d0f] shadow-2xl overflow-hidden relative">
          <div className="h-12 border-b border-white/5 bg-[#111113] flex items-center px-4 gap-2">
            <div className="w-3 h-3 rounded-full bg-zinc-700" />
            <div className="w-3 h-3 rounded-full bg-zinc-700" />
            <div className="w-3 h-3 rounded-full bg-zinc-700" />
            <div className="ml-4 px-3 py-1 rounded bg-[#18181b] border border-white/5 text-xs text-zinc-500 flex items-center gap-2 font-mono">
              <Database className="w-3 h-3" /> production_db / public
            </div>
          </div>

          <div className="flex h-100 text-left">
            <div className="w-48 border-r border-white/5 bg-[#111113] p-4 hidden md:block">
              <div className="text-xs font-semibold text-zinc-500 uppercase mb-3">Tables</div>
              <div className="space-y-2 text-sm text-zinc-400">
                <div className="flex items-center gap-2"><Database className="w-3 h-3" /> users</div>
                <div className="flex items-center gap-2 text-sky-400 bg-sky-500/10 py-1 px-2 rounded -mx-2"><Database className="w-3 h-3" /> orders</div>
                <div className="flex items-center gap-2"><Database className="w-3 h-3" /> products</div>
              </div>
            </div>

            <div className="flex-1 p-6 flex flex-col bg-[#080809]">
              <div className="bg-[#111113] border border-sky-500/30 rounded-xl p-4 shadow-inner mb-4">
                <div className="text-zinc-300 font-medium">Show me the top 5 customers by total order amount this year</div>
              </div>

              <div className="flex-1 border border-white/5 rounded-xl bg-[#0d0d0f] overflow-hidden font-mono text-sm">
                <div className="bg-[#18181b] border-b border-white/5 px-4 py-2 text-xs text-zinc-500 flex justify-between items-center">
                  <span>Generated SQL</span>
                  <span className="text-green-400 flex items-center gap-1"><CheckCircle2 className="w-3 h-3" /> 12ms</span>
                </div>
                <div className="p-4 text-zinc-300 whitespace-pre">
                  <span className="text-pink-400">SELECT</span> c.name, <span className="text-cyan-400">SUM</span>(o.amount) <span className="text-pink-400">AS</span> total{'\n'}
                  <span className="text-pink-400">FROM</span> customers c{'\n'}
                  <span className="text-pink-400">JOIN</span> orders o <span className="text-pink-400">ON</span> c.id = o.customer_id{'\n'}
                  <span className="text-pink-400">WHERE</span> o.created_at &gt;= <span className="text-yellow-300">'2024-01-01'</span>{'\n'}
                  <span className="text-pink-400">GROUP BY</span> 1 {'\n'}
                  <span className="text-pink-400">ORDER BY</span> 2 <span className="text-pink-400">DESC LIMIT</span> 5;
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Testimonials */}
      <div className="py-20 border-y border-white/5 bg-[#111113] z-10 relative">
        <div className="max-w-7xl mx-auto px-6">
          <p className="text-center text-sm font-medium text-zinc-500 mb-10 uppercase tracking-wider">What our users say</p>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {[
              {
                initials: 'SR',
                name: 'Sara R.',
                role: 'Data Engineer · fintech startup',
                quote: 'I used to spend 30 minutes crafting joins across 6 tables. Now I just type what I want and SqlSpace figures it out. The schema awareness is genuinely impressive.',
              },
              {
                initials: 'MK',
                name: 'Marcus K.',
                role: 'Backend Engineer · SaaS company',
                quote: 'We gave our product team access and they stopped bugging engineering for one-off queries. It actually understands our table relationships without any hand-holding.',
              },
              {
                initials: 'AL',
                name: 'Aisha L.',
                role: 'Engineering Manager · e-commerce',
                quote: 'The query history alone is worth it. Every generated SQL is saved, shareable, and re-runnable. It replaced three different internal tools we were duct-taping together.',
              },
            ].map((t) => (
              <div key={t.name} className="bg-[#18181b] border border-white/5 rounded-2xl p-6 flex flex-col gap-4 hover:border-white/10 transition-colors">
                <p className="text-zinc-300 text-sm leading-relaxed flex-1">"{t.quote}"</p>
                <div className="flex items-center gap-3 pt-2 border-t border-white/5">
                  <div className="w-9 h-9 rounded-full bg-sky-500/15 border border-sky-500/25 flex items-center justify-center text-xs font-bold text-sky-300 shrink-0">
                    {t.initials}
                  </div>
                  <div>
                    <p className="text-sm font-semibold text-white leading-tight">{t.name}</p>
                    <p className="text-xs text-zinc-500 mt-0.5">{t.role}</p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Features */}
      <div id="features" className="py-32 relative z-10 max-w-7xl mx-auto px-6">
        <div className="text-center mb-16">
          <h2 className="text-3xl lg:text-4xl font-bold text-white mb-4">Everything you need to query faster</h2>
          <p className="text-zinc-400 max-w-2xl mx-auto">Built for developers, data scientists, and product managers who want to spend less time writing boilerplate SQL.</p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[
            { icon: Terminal,  title: 'Natural Language to SQL',  desc: 'Our fine-tuned LLMs understand your schema and translate English to complex, optimized SQL automatically.' },
            { icon: Database,  title: 'Schema-Aware AI',           desc: 'SqlSpace indexes your schema metadata to provide accurate joins, table resolutions, and syntax.' },
            { icon: Zap,       title: 'Live Query Execution',      desc: 'Run generated SQL directly against your database through our secure, encrypted proxy.' },
            { icon: Server,    title: 'Multi-Database Support',    desc: 'PostgreSQL, MySQL, SQL Server, and more. One unified interface for all your data.' },
            { icon: BarChart2, title: 'Insightful Charts',         desc: 'Results are automatically visualized into beautiful, interactive charts without any extra config.' },
            { icon: History,   title: 'Query History',             desc: 'Never lose a query again. Full searchable history of every prompt, generated SQL, and result.' },
          ].map((feature, i) => (
            <div key={i} className="p-6 rounded-2xl bg-[#111113] border border-white/5 hover:border-sky-500/30 transition-colors group">
              <div className="w-10 h-10 rounded-lg bg-sky-500/10 text-sky-400 flex items-center justify-center mb-4 group-hover:scale-110 group-hover:bg-sky-500 group-hover:text-white transition-all">
                <feature.icon className="w-5 h-5" />
              </div>
              <h3 className="text-lg font-semibold text-white mb-2">{feature.title}</h3>
              <p className="text-zinc-400 text-sm leading-relaxed">{feature.desc}</p>
            </div>
          ))}
        </div>
      </div>

      {/* How it works */}
      <div id="how" className="py-20 relative z-10 bg-[#111113] border-y border-white/5">
        <div className="max-w-7xl mx-auto px-6">
          <div className="text-center mb-16">
            <h2 className="text-3xl lg:text-4xl font-bold text-white mb-4">How it works</h2>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {[
              { step: '01', title: 'Connect your database',    desc: 'Securely connect PostgreSQL, MySQL, or any supported database. Your credentials are encrypted end-to-end.' },
              { step: '02', title: 'Describe what you want',   desc: 'Type a plain English question or describe the data you need. No SQL knowledge required.' },
              { step: '03', title: 'Get results instantly',    desc: 'SqlSpace generates optimized SQL, executes it, and returns results with visual charts in seconds.' },
            ].map((step, i) => (
              <div key={i} className="text-center">
                <div className="w-14 h-14 rounded-2xl bg-sky-500/10 border border-sky-500/20 flex items-center justify-center mx-auto mb-4">
                  <span className="text-sky-400 font-bold font-mono text-sm">{step.step}</span>
                </div>
                <h3 className="text-lg font-semibold text-white mb-2">{step.title}</h3>
                <p className="text-zinc-400 text-sm leading-relaxed">{step.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Database grid */}
      <div id="databases" className="py-20 relative z-10 max-w-7xl mx-auto px-6">
        <div className="text-center mb-12">
          <h2 className="text-3xl font-bold text-white mb-4">Compatible with every major database</h2>
          <p className="text-zinc-500 text-sm">All supported and ready to connect — no extra configuration needed.</p>
        </div>
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          {[
            { name: 'PostgreSQL',      note: null },
            { name: 'MySQL',           note: null },
            { name: 'SQL Server',      note: null },
            { name: 'MariaDB',         note: null },
            { name: 'CockroachDB',     note: null },
            { name: 'Supabase',        note: null },
            { name: 'PlanetScale',     note: null },
            { name: 'Amazon Redshift', note: null },
          ].map((db) => (
            <div key={db.name} className="bg-[#111113] border border-white/5 rounded-xl px-4 py-3 flex items-center justify-between gap-2 hover:border-white/10 transition-colors group">
              <span className="text-sm text-zinc-300 group-hover:text-white transition-colors font-medium">{db.name}</span>
              {db.note ? (
                <span className="text-[10px] px-1.5 py-0.5 rounded-full bg-sky-500/10 text-sky-400 border border-sky-500/20 font-medium shrink-0">{db.note}</span>
              ) : (
                <span className="w-1.5 h-1.5 rounded-full bg-green-500 shrink-0" />
              )}
            </div>
          ))}
        </div>
      </div>

      {/* Stats */}
      <div className="py-12 relative z-10 bg-[#111113] border-y border-white/5">
        <div className="max-w-7xl mx-auto px-6">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-8 text-center">
            {[
              { value: '12,400+', label: 'Developers using SqlSpace' },
              { value: '4.2M+',   label: 'Queries generated to date' },
              { value: '94%',     label: 'First-try accuracy rate' },
              { value: '1.2s',    label: 'Average generation time' },
            ].map((stat, i) => (
              <div key={i}>
                <div className="text-4xl font-bold text-white mb-2">{stat.value}</div>
                <div className="text-sm text-zinc-500">{stat.label}</div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* CTA */}
      <div className="py-24 relative z-10 px-6">
        <div className="max-w-3xl mx-auto text-center bg-linear-to-b from-sky-500/10 to-transparent border border-sky-500/20 rounded-3xl p-16">
          <h2 className="text-4xl font-bold text-white mb-4">Ready to query smarter?</h2>
          <p className="text-zinc-400 mb-8">Join thousands of developers who spend less time writing SQL and more time building.</p>
          <Link
            to="/register"
            className="inline-flex items-center gap-2 h-12 px-8 rounded-full bg-sky-600 text-white font-semibold hover:bg-sky-500 hover:shadow-[0_0_30px_rgba(14,165,233,0.4)] transition-all"
          >
            Get started free <ArrowRight className="w-4 h-4" />
          </Link>
        </div>
      </div>

      {/* Footer */}
      <footer className="border-t border-white/10 bg-[#080809] py-12 relative z-10">
        <div className="max-w-7xl mx-auto px-6 flex flex-col md:flex-row justify-between items-center gap-6">
          <div className="flex items-center gap-2">
            <div className="w-6 h-6 rounded flex items-center justify-center bg-sky-500 text-white">
              <Zap className="w-3 h-3" />
            </div>
            <span className="font-semibold text-white">SqlSpace</span>
          </div>
          <div className="flex gap-6 text-sm text-zinc-500">
            <a href="#features" className="hover:text-white transition-colors">Features</a>
            <a href="#how"      className="hover:text-white transition-colors">How it works</a>
            <a href="#databases" className="hover:text-white transition-colors">Databases</a>
            <Link to="/login"   className="hover:text-white transition-colors">Sign in</Link>
          </div>
        </div>
      </footer>
    </div>
  )
}
