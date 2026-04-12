import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Loader2, CheckCircle2, AlertCircle, ChevronRight } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { connectionsApi } from '@/api/connections'
import { useConnectionStore } from '@/stores/connection-store'
import type { DBProvider } from '@/types'
import { DB_ICONS } from '@/components/icons/dbIconMap'

const PROVIDERS: { value: DBProvider; label: string; defaultPort: number }[] = [
  { value: 'PostgreSql',  label: 'PostgreSQL',      defaultPort: 5432  },
  { value: 'MySql',       label: 'MySQL',            defaultPort: 3306  },
  { value: 'SqlServer',   label: 'SQL Server',       defaultPort: 1433  },
  { value: 'MariaDb',     label: 'MariaDB',          defaultPort: 3306  },
  { value: 'CockroachDb', label: 'CockroachDB',      defaultPort: 26257 },
  { value: 'Supabase',    label: 'Supabase',         defaultPort: 5432  },
  { value: 'PlanetScale', label: 'PlanetScale',      defaultPort: 3306  },
  { value: 'Redshift',    label: 'Amazon Redshift',  defaultPort: 5439  },
]

const ALL_PROVIDERS = ['PostgreSql', 'MySql', 'SqlServer', 'MariaDb', 'CockroachDb', 'Supabase', 'PlanetScale', 'Redshift'] as const

const schema = z.object({
  connectionName: z.string().min(1, 'Name required'),
  databaseProvider: z.enum(ALL_PROVIDERS),
  host: z.string().optional(),
  port: z.coerce.number().optional(),
  databaseName: z.string().min(1, 'Database required'),
  username: z.string().optional(),
  password: z.string().optional(),
})

type FormInput  = z.input<typeof schema>   // raw field values  (port: unknown — coerce accepts anything)
type FormData   = z.output<typeof schema>  // post-transform values (port: number | undefined)
type Step = 'provider' | 'fields' | 'test'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function ConnectionForm({ open, onOpenChange }: Props) {
  const [step, setStep] = useState<Step>('provider')
  const [testStatus, setTestStatus] = useState<'idle' | 'testing' | 'ok' | 'fail'>('idle')
  const [testMessage, setTestMessage] = useState('')
  const qc = useQueryClient()
  const { upsertConnection, setActiveConnection } = useConnectionStore()

  const [selectedProvider, setSelectedProvider] = useState<DBProvider>('PostgreSql')

  const {
    register,
    handleSubmit,
    setValue,
    getValues,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormInput, unknown, FormData>({
    resolver: zodResolver(schema),
    defaultValues: { databaseProvider: 'PostgreSql', port: 5432 },
  })

  const handleClose = () => {
    reset()
    setStep('provider')
    setTestStatus('idle')
    setTestMessage('')
    onOpenChange(false)
  }

  const handleTest = async () => {
    setTestStatus('testing')
    const values = getValues()
    try {
      const res = await connectionsApi.test({
        databaseProvider: values.databaseProvider,
        inputMode: 'IndividualFields',
        host: values.host,
        port: values.port as number | undefined,
        databaseName: values.databaseName,
        username: values.username,
        password: values.password,
        useSSL: false,
      })
      setTestStatus(res.success ? 'ok' : 'fail')
      setTestMessage(res.message)
    } catch {
      setTestStatus('fail')
      setTestMessage('Connection failed')
    }
  }

  const onSubmit = async (data: FormData) => {
    try {
      const conn = await connectionsApi.create({
        connectionName: data.connectionName,
        databaseProvider: data.databaseProvider,
        inputMode: 'IndividualFields',
        host: data.host,
        port: data.port,
        databaseName: data.databaseName,
        username: data.username,
        password: data.password,
        useSSL: false,
      })
      upsertConnection(conn)
      setActiveConnection(conn.connectionId)
      await qc.invalidateQueries({ queryKey: ['connections'] })
      toast.success(`"${conn.connectionName}" connected`)
      handleClose()
    } catch {
      toast.error('Failed to create connection')
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-md bg-(--bg-elevated) border-(--border-default) text-(--text-primary)">
        <DialogHeader>
          <DialogTitle className="text-(--text-primary)">New Connection</DialogTitle>
          <div className="flex items-center gap-2 mt-2">
            {(['provider', 'fields', 'test'] as Step[]).map((s, i) => (
              <div key={s} className="flex items-center gap-2">
                <span className={`text-xs px-2 py-0.5 rounded-full ${step === s ? 'bg-sky-600/30 text-sky-300 border border-sky-500/40' : 'text-(--text-muted)'}`}>
                  {i + 1}. {s.charAt(0).toUpperCase() + s.slice(1)}
                </span>
                {i < 2 && <ChevronRight className="w-3 h-3 text-(--text-muted)" />}
              </div>
            ))}
          </div>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 mt-2">
          {/* Step 1: Provider */}
          {step === 'provider' && (
            <div className="space-y-4">
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
                {PROVIDERS.map((p) => {
                  const Icon = DB_ICONS[p.value]
                  return (
                    <button
                      key={p.value}
                      type="button"
                      onClick={() => { setSelectedProvider(p.value); setValue('databaseProvider', p.value); setValue('port', p.defaultPort) }}
                      className={`p-3 rounded-lg border text-sm text-left transition-colors ${selectedProvider === p.value ? 'border-sky-500/60 bg-sky-600/15 text-sky-300' : 'border-(--border-default) bg-(--bg-surface) text-(--text-secondary) hover:border-(--border-strong)'}`}
                    >
                      <Icon size={22} className="mb-1.5" />
                      <div className="text-xs font-medium leading-tight">{p.label}</div>
                    </button>
                  )
                })}
              </div>
              <Button type="button" className="w-full bg-sky-600 hover:bg-sky-500" onClick={() => setStep('fields')}>
                Continue
              </Button>
            </div>
          )}

          {/* Step 2: Fields */}
          {step === 'fields' && (
            <div className="space-y-3">
              <div className="space-y-1">
                <Label className="text-(--text-secondary) text-sm">Connection name</Label>
                <Input placeholder="My Database" className="bg-(--bg-surface) border-(--border-default) text-(--text-primary)" {...register('connectionName')} />
                {errors.connectionName && <p className="text-xs text-red-400">{errors.connectionName.message}</p>}
              </div>
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
                <div className="col-span-2 space-y-1">
                  <Label className="text-(--text-secondary) text-sm">Host</Label>
                  <Input placeholder="localhost" className="bg-(--bg-surface) border-(--border-default) text-(--text-primary)" {...register('host')} />
                </div>
                <div className="space-y-1">
                  <Label className="text-(--text-secondary) text-sm">Port</Label>
                  <Input type="number" className="bg-(--bg-surface) border-(--border-default) text-(--text-primary)" {...register('port')} />
                </div>
              </div>
              <div className="space-y-1">
                <Label className="text-(--text-secondary) text-sm">Database</Label>
                <Input placeholder="my_database" className="bg-(--bg-surface) border-(--border-default) text-(--text-primary)" {...register('databaseName')} />
                {errors.databaseName && <p className="text-xs text-red-400">{errors.databaseName.message}</p>}
              </div>
              <div className="space-y-1">
                <Label className="text-(--text-secondary) text-sm">Username</Label>
                <Input className="bg-(--bg-surface) border-(--border-default) text-(--text-primary)" {...register('username')} />
              </div>
              <div className="space-y-1">
                <Label className="text-(--text-secondary) text-sm">Password</Label>
                <Input type="password" className="bg-(--bg-surface) border-(--border-default) text-(--text-primary)" {...register('password')} />
              </div>
              <div className="flex gap-2">
                <Button type="button" variant="ghost" className="flex-1" onClick={() => setStep('provider')}>Back</Button>
                <Button type="button" className="flex-1 bg-sky-600 hover:bg-sky-500" onClick={() => setStep('test')}>Continue</Button>
              </div>
            </div>
          )}

          {/* Step 3: Test + Create */}
          {step === 'test' && (
            <div className="space-y-4">
              <div className="rounded-lg border border-(--border-default) bg-(--bg-surface) p-4 space-y-3">
                <p className="text-sm text-(--text-secondary)">Test connection before saving</p>
                <Button type="button" variant="outline" className="w-full border-(--border-strong) text-(--text-secondary)" onClick={handleTest} disabled={testStatus === 'testing'}>
                  {testStatus === 'idle' && 'Test connection'}
                  {testStatus === 'testing' && <><Loader2 className="w-4 h-4 animate-spin mr-2" />Testing…</>}
                  {testStatus === 'ok' && <><CheckCircle2 className="w-4 h-4 text-green-400 mr-2" />Connected!</>}
                  {testStatus === 'fail' && <><AlertCircle className="w-4 h-4 text-red-400 mr-2" />Failed</>}
                </Button>
                {testMessage && <p className={`text-xs ${testStatus === 'ok' ? 'text-green-400' : 'text-red-400'}`}>{testMessage}</p>}
              </div>
              <div className="flex gap-2">
                <Button type="button" variant="ghost" className="flex-1" onClick={() => setStep('fields')}>Back</Button>
                <Button type="submit" disabled={isSubmitting} className="flex-1 bg-sky-600 hover:bg-sky-500">
                  {isSubmitting ? <Loader2 className="w-4 h-4 animate-spin" /> : 'Save connection'}
                </Button>
              </div>
            </div>
          )}
        </form>
      </DialogContent>
    </Dialog>
  )
}
