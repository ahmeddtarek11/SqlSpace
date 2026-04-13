import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { motion } from 'framer-motion'
import { Database, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { authApi } from '@/api/auth'

const schema = z.object({
  username: z.string().min(3, 'At least 3 characters'),
  email: z.string().email('Valid email required'),
  password: z.string().min(8, 'At least 8 characters'),
})

type FormData = z.infer<typeof schema>

export default function RegisterPage() {
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({ resolver: zodResolver(schema) })

  const onSubmit = async (data: FormData) => {
    setError(null)
    try {
      await authApi.register(data)
      navigate('/login', { replace: true })
    } catch {
      setError('Registration failed. Username or email may already be taken.')
    }
  }

  return (
    <div
      className="min-h-screen flex items-center justify-center p-4 relative"
      style={{ background: 'var(--bg-base)' }}
    >
      {/* Subtle gradient glow */}
      <div
        className="absolute top-1/3 left-1/2 -translate-x-1/2 w-[500px] h-[500px] rounded-full pointer-events-none"
        style={{
          background: 'radial-gradient(circle, var(--accent-glow) 0%, transparent 70%)',
          filter: 'blur(80px)',
        }}
      />

      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.35, ease: [0.25, 0.46, 0.45, 0.94] }}
        className="w-full max-w-[400px] relative z-10"
      >
        <div className="flex flex-col items-center mb-8">
          <div
            className="w-11 h-11 flex items-center justify-center mb-5"
            style={{
              borderRadius: 'var(--radius-lg)',
              background: 'var(--accent)',
              boxShadow: '0 0 20px var(--accent-glow)',
            }}
          >
            <Database className="w-5 h-5 text-white" strokeWidth={2} />
          </div>
          <h1 className="text-[22px] font-bold" style={{ color: 'var(--text-primary)', letterSpacing: '-0.02em' }}>
            Create account
          </h1>
          <p className="text-[13px] mt-1" style={{ color: 'var(--text-tertiary)' }}>
            Join SqlSpace
          </p>
        </div>

        <div
          className="p-8"
          style={{
            borderRadius: 'var(--radius-xl)',
            background: 'var(--bg-surface)',
            border: '1px solid var(--border-default)',
            boxShadow: '0 24px 64px rgba(0,0,0,0.3), 0 0 1px rgba(0,0,0,0.2)',
          }}
        >
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            <div className="space-y-2">
              <Label htmlFor="username" className="text-[12px] font-semibold" style={{ color: 'var(--text-secondary)' }}>
                Username
              </Label>
              <Input
                id="username"
                placeholder="your_username"
                className="h-10 text-[14px] focus-visible:ring-1 transition-all"
                style={{
                  borderRadius: 'var(--radius-md)',
                  background: 'var(--bg-elevated)',
                  border: '1px solid var(--border-default)',
                  color: 'var(--text-primary)',
                }}
                {...register('username')}
              />
              {errors.username && <p className="text-[11px]" style={{ color: 'var(--danger)' }}>{errors.username.message}</p>}
            </div>

            <div className="space-y-2">
              <Label htmlFor="email" className="text-[12px] font-semibold" style={{ color: 'var(--text-secondary)' }}>
                Email
              </Label>
              <Input
                id="email"
                type="email"
                placeholder="you@example.com"
                className="h-10 text-[14px] focus-visible:ring-1 transition-all"
                style={{
                  borderRadius: 'var(--radius-md)',
                  background: 'var(--bg-elevated)',
                  border: '1px solid var(--border-default)',
                  color: 'var(--text-primary)',
                }}
                {...register('email')}
              />
              {errors.email && <p className="text-[11px]" style={{ color: 'var(--danger)' }}>{errors.email.message}</p>}
            </div>

            <div className="space-y-2">
              <Label htmlFor="password" className="text-[12px] font-semibold" style={{ color: 'var(--text-secondary)' }}>
                Password
              </Label>
              <Input
                id="password"
                type="password"
                placeholder="••••••••"
                className="h-10 text-[14px] focus-visible:ring-1 transition-all"
                style={{
                  borderRadius: 'var(--radius-md)',
                  background: 'var(--bg-elevated)',
                  border: '1px solid var(--border-default)',
                  color: 'var(--text-primary)',
                }}
                {...register('password')}
              />
              {errors.password && <p className="text-[11px]" style={{ color: 'var(--danger)' }}>{errors.password.message}</p>}
            </div>

            {error && <p className="text-[13px] text-center" style={{ color: 'var(--danger)' }}>{error}</p>}

            <Button
              type="submit"
              disabled={isSubmitting}
              className="w-full h-10 text-[13px] font-semibold text-white active:scale-[0.98] transition-all"
              style={{
                borderRadius: 'var(--radius-md)',
                background: 'var(--accent)',
                boxShadow: '0 0 16px var(--accent-glow)',
              }}
            >
              {isSubmitting ? <Loader2 className="w-4 h-4 animate-spin" /> : 'Create account'}
            </Button>
          </form>

          <p className="text-center text-[13px] mt-6" style={{ color: 'var(--text-muted)' }}>
            Already have an account?{' '}
            <Link
              to="/login"
              className="font-medium transition-colors"
              style={{ color: 'var(--accent)' }}
              onMouseEnter={(e) => { e.currentTarget.style.color = 'var(--accent-hover)' }}
              onMouseLeave={(e) => { e.currentTarget.style.color = 'var(--accent)' }}
            >
              Sign in
            </Link>
          </p>
        </div>
      </motion.div>
    </div>
  )
}
