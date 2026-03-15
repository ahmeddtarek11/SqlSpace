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
    <div className="grid-bg min-h-screen flex items-center justify-center p-4">
      <motion.div
        initial={{ opacity: 0, y: 24 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4 }}
        className="w-full max-w-md"
      >
        <div className="flex flex-col items-center mb-8">
          <div className="w-12 h-12 rounded-xl bg-violet-600/20 border border-violet-500/30 flex items-center justify-center mb-4 glow-accent">
            <Database className="w-6 h-6 text-violet-400" />
          </div>
          <h1 className="text-2xl font-semibold text-white">Create account</h1>
          <p className="text-sm text-(--text-secondary) mt-1">Join SqlSpace</p>
        </div>

        <div className="bg-(--bg-surface) border border-(--border-default) rounded-2xl p-8 shadow-2xl">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            <div className="space-y-2">
              <Label htmlFor="username" className="text-(--text-secondary) text-sm">Username</Label>
              <Input
                id="username"
                placeholder="your_username"
                className="bg-(--bg-elevated) border-(--border-default) text-white placeholder:text-(--text-muted)"
                {...register('username')}
              />
              {errors.username && <p className="text-xs text-red-400">{errors.username.message}</p>}
            </div>

            <div className="space-y-2">
              <Label htmlFor="email" className="text-(--text-secondary) text-sm">Email</Label>
              <Input
                id="email"
                type="email"
                placeholder="you@example.com"
                className="bg-(--bg-elevated) border-(--border-default) text-white placeholder:text-(--text-muted)"
                {...register('email')}
              />
              {errors.email && <p className="text-xs text-red-400">{errors.email.message}</p>}
            </div>

            <div className="space-y-2">
              <Label htmlFor="password" className="text-(--text-secondary) text-sm">Password</Label>
              <Input
                id="password"
                type="password"
                placeholder="••••••••"
                className="bg-(--bg-elevated) border-(--border-default) text-white placeholder:text-(--text-muted)"
                {...register('password')}
              />
              {errors.password && <p className="text-xs text-red-400">{errors.password.message}</p>}
            </div>

            {error && <p className="text-sm text-red-400 text-center">{error}</p>}

            <Button
              type="submit"
              disabled={isSubmitting}
              className="w-full bg-violet-600 hover:bg-violet-500 text-white font-medium h-10"
            >
              {isSubmitting ? <Loader2 className="w-4 h-4 animate-spin" /> : 'Create account'}
            </Button>
          </form>

          <p className="text-center text-sm text-(--text-muted) mt-6">
            Already have an account?{' '}
            <Link to="/login" className="text-violet-400 hover:text-violet-300">
              Sign in
            </Link>
          </p>
        </div>
      </motion.div>
    </div>
  )
}
