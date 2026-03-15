import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { Database, Sparkles, Zap, Shield, ArrowRight } from 'lucide-react'
import { Button } from '@/components/ui/button'

const FEATURES = [
  {
    icon: <Sparkles className="w-5 h-5 text-violet-400" />,
    title: 'Natural Language to SQL',
    desc: 'Ask questions in plain English. SqlSpace converts them to optimized SQL instantly.',
  },
  {
    icon: <Zap className="w-5 h-5 text-cyan-400" />,
    title: 'Multi-database Support',
    desc: 'Connect to PostgreSQL, MySQL, SQLite, SQL Server, and Oracle from one place.',
  },
  {
    icon: <Shield className="w-5 h-5 text-green-400" />,
    title: 'Access Control',
    desc: 'Fine-grained permissions so your team only sees what they need to see.',
  },
]

const FADE_UP = {
  hidden: { opacity: 0, y: 24 },
  show: (i: number) => ({ opacity: 1, y: 0, transition: { delay: i * 0.1, duration: 0.5 } }),
}

export default function LandingPage() {
  return (
    <div className="grid-bg min-h-screen text-white flex flex-col">
      {/* Nav */}
      <nav className="flex items-center justify-between px-6 py-4 border-b border-(--border-subtle) backdrop-blur-sm sticky top-0 z-20">
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded-xl bg-violet-600/20 border border-violet-500/30 flex items-center justify-center">
            <Database className="w-4 h-4 text-violet-400" />
          </div>
          <span className="font-semibold">SqlSpace</span>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="sm" className="text-(--text-secondary) hover:text-white" asChild>
            <Link to="/login">Sign in</Link>
          </Button>
          <Button size="sm" className="bg-violet-600 hover:bg-violet-500 text-white" asChild>
            <Link to="/register">Get started</Link>
          </Button>
        </div>
      </nav>

      {/* Hero */}
      <section className="flex-1 flex flex-col items-center justify-center text-center px-6 py-24 relative">
        {/* Glow blobs */}
        <div className="absolute inset-0 overflow-hidden pointer-events-none">
          <div className="absolute -top-32 left-1/2 -translate-x-1/2 w-96 h-96 bg-violet-600/10 rounded-full blur-3xl" />
          <div className="absolute top-32 right-1/4 w-64 h-64 bg-cyan-600/8 rounded-full blur-3xl" />
        </div>

        <motion.div
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ duration: 0.5 }}
          className="inline-flex items-center gap-2 px-3 py-1.5 rounded-full border border-violet-500/30 bg-violet-600/10 text-violet-300 text-xs mb-6"
        >
          <Sparkles className="w-3 h-3" />
          AI-powered SQL workspace
        </motion.div>

        <motion.h1
          initial={{ opacity: 0, y: 24 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1, duration: 0.5 }}
          className="text-5xl sm:text-6xl font-bold tracking-tight max-w-3xl leading-tight"
        >
          Query your data{' '}
          <span className="bg-linear-to-r from-violet-400 to-cyan-400 bg-clip-text text-transparent">
            in plain English
          </span>
        </motion.h1>

        <motion.p
          initial={{ opacity: 0, y: 24 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2, duration: 0.5 }}
          className="mt-6 text-lg text-(--text-secondary) max-w-xl"
        >
          SqlSpace turns natural language into optimized SQL — no memorizing syntax, no context
          switching, just answers.
        </motion.p>

        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.3, duration: 0.5 }}
          className="mt-8 flex items-center gap-3"
        >
          <Button size="lg" className="bg-violet-600 hover:bg-violet-500 text-white px-6" asChild>
            <Link to="/register">
              Start for free
              <ArrowRight className="w-4 h-4 ml-2" />
            </Link>
          </Button>
          <Button size="lg" variant="outline" className="border-(--border-strong) text-(--text-secondary) hover:text-white" asChild>
            <Link to="/login">Sign in</Link>
          </Button>
        </motion.div>

        {/* Demo prompt preview */}
        <motion.div
          initial={{ opacity: 0, y: 32 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.45, duration: 0.6 }}
          className="mt-16 w-full max-w-2xl"
        >
          <div className="gradient-border rounded-xl p-0.5">
            <div className="bg-(--bg-surface) rounded-xl p-4 text-left">
              <div className="flex items-center gap-2 mb-3">
                <Sparkles className="w-4 h-4 text-violet-400" />
                <span className="text-sm text-(--text-muted)">Natural language prompt</span>
              </div>
              <p className="text-sm text-white">
                Show me the top 10 customers by total revenue in the last 30 days
              </p>
              <div className="mt-3 pt-3 border-t border-(--border-subtle)">
                <p className="text-xs text-(--text-muted) mb-2 font-mono">Generated SQL</p>
                <pre className="text-xs font-mono text-cyan-300 overflow-x-auto">
{`SELECT c.name, SUM(o.total) AS revenue
FROM orders o
JOIN customers c ON o.customer_id = c.id
WHERE o.created_at >= NOW() - INTERVAL '30 days'
GROUP BY c.id, c.name
ORDER BY revenue DESC
LIMIT 10;`}
                </pre>
              </div>
            </div>
          </div>
        </motion.div>
      </section>

      {/* Features */}
      <section className="px-6 pb-24 max-w-5xl mx-auto w-full">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {FEATURES.map((f, i) => (
            <motion.div
              key={f.title}
              custom={i}
              variants={FADE_UP}
              initial="hidden"
              whileInView="show"
              viewport={{ once: true }}
              className="bg-(--bg-surface) border border-(--border-default) rounded-xl p-6"
            >
              <div className="w-10 h-10 rounded-lg bg-(--bg-elevated) border border-(--border-default) flex items-center justify-center mb-4">
                {f.icon}
              </div>
              <h3 className="text-sm font-semibold text-white mb-2">{f.title}</h3>
              <p className="text-sm text-(--text-muted)">{f.desc}</p>
            </motion.div>
          ))}
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t border-(--border-subtle) px-6 py-6 flex items-center justify-between text-xs text-(--text-muted)">
        <span>© 2026 SqlSpace</span>
        <div className="flex items-center gap-4">
          <a href="https://github.com" target="_blank" rel="noreferrer" className="hover:text-white">
            GitHub
          </a>
          <Link to="/login" className="hover:text-white">Sign in</Link>
        </div>
      </footer>
    </div>
  )
}
