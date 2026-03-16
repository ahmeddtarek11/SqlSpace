import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  ArrowLeft,
  Loader2,
  CheckCircle2,
  AlertCircle,
  ToggleLeft,
  ToggleRight,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { connectionsApi } from '@/api/connections'
import { useConnectionStore } from '@/stores/connection-store'
import { cn } from '@/lib/utils'
import type { DBProvider } from '@/types'

// ── Providers ─────────────────────────────────────────────────

const PROVIDERS: {
  value: DBProvider
  label: string
  description: string
  defaultPort: number
  color: string
  dot: string
}[] = [
  {
    value: 'PostgreSql',
    label: 'PostgreSQL',
    description: 'Open-source relational database',
    defaultPort: 5432,
    color: 'border-blue-500/50 bg-blue-600/10 text-blue-300',
    dot: 'bg-blue-500',
  },
  {
    value: 'MySql',
    label: 'MySQL',
    description: 'Popular open-source RDBMS',
    defaultPort: 3306,
    color: 'border-orange-500/50 bg-orange-600/10 text-orange-300',
    dot: 'bg-orange-500',
  },
  {
    value: 'SqlServer',
    label: 'SQL Server',
    description: 'Microsoft SQL Server',
    defaultPort: 1433,
    color: 'border-red-500/50 bg-red-600/10 text-red-300',
    dot: 'bg-red-500',
  },
]

// ── Zod schemas ───────────────────────────────────────────────

const fieldsSchema = z.object({
  connectionName: z.string().min(1, 'Name is required'),
  databaseProvider: z.enum(['PostgreSql', 'MySql', 'SqlServer'] as const),
  host: z.string().min(1, 'Host is required'),
  port: z.coerce.number().int().min(1).max(65535),
  databaseName: z.string().min(1, 'Database name is required'),
  username: z.string().optional(),
  password: z.string().optional(),
  useSSL: z.boolean().default(false),
  additionalParameters: z.string().optional(),
})

const connStringSchema = z.object({
  connectionName: z.string().min(1, 'Name is required'),
  databaseProvider: z.enum(['PostgreSql', 'MySql', 'SqlServer'] as const),
  rawConnectionString: z.string().min(1, 'Connection string is required'),
})

type FieldsForm = z.infer<typeof fieldsSchema>
type ConnStringForm = z.infer<typeof connStringSchema>

type InputMode = 'IndividualFields' | 'RawConnectionString'

// ── Page ──────────────────────────────────────────────────────

export default function NewConnectionPage() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  const { upsertConnection, setActiveConnection } = useConnectionStore()

  const [inputMode, setInputMode] = useState<InputMode>('IndividualFields')
  const [selectedProvider, setSelectedProvider] = useState<DBProvider>('PostgreSql')
  const [testStatus, setTestStatus] = useState<'idle' | 'testing' | 'ok' | 'fail'>('idle')
  const [testMessage, setTestMessage] = useState('')

  // Fields form
  const fieldsForm = useForm<FieldsForm>({
    resolver: zodResolver(fieldsSchema),
    defaultValues: {
      databaseProvider: 'PostgreSql',
      port: 5432,
      useSSL: false,
    },
  })

  // Connection string form
  const connStringForm = useForm<ConnStringForm>({
    resolver: zodResolver(connStringSchema),
    defaultValues: { databaseProvider: 'PostgreSql' },
  })

  const handleProviderSelect = (p: (typeof PROVIDERS)[number]) => {
    setSelectedProvider(p.value)
    fieldsForm.setValue('databaseProvider', p.value)
    fieldsForm.setValue('port', p.defaultPort)
    connStringForm.setValue('databaseProvider', p.value)
  }

  // ── Test connection ────────────────────────────────────────

  const handleTest = async () => {
    setTestStatus('testing')
    try {
      let res: { success: boolean; message: string }

      if (inputMode === 'IndividualFields') {
        const valid = await fieldsForm.trigger()
        if (!valid) { setTestStatus('idle'); return }
        const v = fieldsForm.getValues()
        res = await connectionsApi.test({
          databaseProvider: v.databaseProvider,
          inputMode: 'IndividualFields',
          host: v.host,
          port: v.port,
          databaseName: v.databaseName,
          username: v.username,
          password: v.password,
          useSSL: v.useSSL,
          additionalParameters: v.additionalParameters,
        })
      } else {
        const valid = await connStringForm.trigger()
        if (!valid) { setTestStatus('idle'); return }
        const v = connStringForm.getValues()
        res = await connectionsApi.test({
          databaseProvider: v.databaseProvider,
          inputMode: 'RawConnectionString',
          rawConnectionString: v.rawConnectionString,
        })
      }

      setTestStatus(res.success ? 'ok' : 'fail')
      setTestMessage(res.message)
    } catch {
      setTestStatus('fail')
      setTestMessage('Connection failed')
    }
  }

  // ── Submit ─────────────────────────────────────────────────

  const handleFieldsSubmit = async (data: FieldsForm) => {
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
        useSSL: data.useSSL,
        additionalParameters: data.additionalParameters,
      })
      upsertConnection(conn)
      setActiveConnection(conn.connectionId)
      await qc.invalidateQueries({ queryKey: ['connections'] })
      toast.success(`"${conn.connectionName}" connected`)
      navigate('/connections')
    } catch {
      toast.error('Failed to create connection')
    }
  }

  const handleConnStringSubmit = async (data: ConnStringForm) => {
    try {
      const conn = await connectionsApi.create({
        connectionName: data.connectionName,
        databaseProvider: data.databaseProvider,
        inputMode: 'RawConnectionString',
        rawConnectionString: data.rawConnectionString,
      })
      upsertConnection(conn)
      setActiveConnection(conn.connectionId)
      await qc.invalidateQueries({ queryKey: ['connections'] })
      toast.success(`"${conn.connectionName}" connected`)
      navigate('/connections')
    } catch {
      toast.error('Failed to create connection')
    }
  }

  const isSubmitting =
    fieldsForm.formState.isSubmitting || connStringForm.formState.isSubmitting

  const fe = fieldsForm.formState.errors
  const ce = connStringForm.formState.errors

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex items-center gap-3 px-6 py-4 border-b border-(--border-default) bg-(--bg-surface) shrink-0">
        <Button
          variant="ghost"
          size="icon"
          className="w-8 h-8 text-(--text-muted) hover:text-(--text-primary)"
          onClick={() => navigate('/connections')}
        >
          <ArrowLeft className="w-4 h-4" />
        </Button>
        <h1 className="text-lg font-semibold text-(--text-primary)">New Connection</h1>
      </div>

      {/* Body */}
      <div className="flex-1 min-h-0 overflow-y-auto">
        <div className="max-w-2xl mx-auto px-6 py-8 space-y-8">

          {/* ── Provider selection ── */}
          <section className="space-y-3">
            <h2 className="text-sm font-medium text-(--text-secondary) uppercase tracking-wider">
              Database Provider
            </h2>
            <div className="grid grid-cols-3 gap-3">
              {PROVIDERS.map((p) => (
                <button
                  key={p.value}
                  type="button"
                  onClick={() => handleProviderSelect(p)}
                  className={cn(
                    'p-4 rounded-xl border text-left transition-all',
                    selectedProvider === p.value
                      ? p.color + ' border-2'
                      : 'border-(--border-default) bg-(--bg-surface) text-(--text-secondary) hover:border-(--border-strong) hover:bg-(--bg-elevated)'
                  )}
                >
                  <div className="flex items-center gap-2 mb-1">
                    <span className={cn('w-2.5 h-2.5 rounded-full', p.dot)} />
                    <span className="text-sm font-semibold">{p.label}</span>
                  </div>
                  <p className="text-xs opacity-70">{p.description}</p>
                </button>
              ))}
            </div>
          </section>

          {/* ── Input mode toggle ── */}
          <section className="space-y-3">
            <h2 className="text-sm font-medium text-(--text-secondary) uppercase tracking-wider">
              Connection Method
            </h2>
            <div className="flex items-center gap-1 p-1 rounded-lg bg-(--bg-elevated) border border-(--border-default) w-fit">
              <button
                type="button"
                onClick={() => setInputMode('IndividualFields')}
                className={cn(
                  'px-4 py-1.5 rounded-md text-sm font-medium transition-colors',
                  inputMode === 'IndividualFields'
                    ? 'bg-violet-600 text-white'
                    : 'text-(--text-muted) hover:text-(--text-secondary)'
                )}
              >
                Individual Fields
              </button>
              <button
                type="button"
                onClick={() => setInputMode('RawConnectionString')}
                className={cn(
                  'px-4 py-1.5 rounded-md text-sm font-medium transition-colors',
                  inputMode === 'RawConnectionString'
                    ? 'bg-violet-600 text-white'
                    : 'text-(--text-muted) hover:text-(--text-secondary)'
                )}
              >
                Connection String
              </button>
            </div>
          </section>

          {/* ── Fields form ── */}
          {inputMode === 'IndividualFields' && (
            <form
              id="conn-form"
              onSubmit={fieldsForm.handleSubmit(handleFieldsSubmit)}
              className="space-y-5"
            >
              <section className="space-y-4 p-5 rounded-xl border border-(--border-default) bg-(--bg-surface)">
                <h3 className="text-sm font-medium text-(--text-primary)">Connection Details</h3>

                <div className="space-y-1.5">
                  <Label className="text-(--text-secondary) text-sm">Connection Name</Label>
                  <Input
                    placeholder="My Production DB"
                    className="bg-(--bg-elevated) border-(--border-default) text-(--text-primary)"
                    {...fieldsForm.register('connectionName')}
                  />
                  {fe.connectionName && (
                    <p className="text-xs text-red-400">{fe.connectionName.message}</p>
                  )}
                </div>

                <div className="grid grid-cols-3 gap-3">
                  <div className="col-span-2 space-y-1.5">
                    <Label className="text-(--text-secondary) text-sm">Host</Label>
                    <Input
                      placeholder="localhost"
                      className="bg-(--bg-elevated) border-(--border-default) text-(--text-primary)"
                      {...fieldsForm.register('host')}
                    />
                    {fe.host && <p className="text-xs text-red-400">{fe.host.message}</p>}
                  </div>
                  <div className="space-y-1.5">
                    <Label className="text-(--text-secondary) text-sm">Port</Label>
                    <Input
                      type="number"
                      className="bg-(--bg-elevated) border-(--border-default) text-(--text-primary)"
                      {...fieldsForm.register('port')}
                    />
                    {fe.port && <p className="text-xs text-red-400">{fe.port.message}</p>}
                  </div>
                </div>

                <div className="space-y-1.5">
                  <Label className="text-(--text-secondary) text-sm">Database Name</Label>
                  <Input
                    placeholder="my_database"
                    className="bg-(--bg-elevated) border-(--border-default) text-(--text-primary)"
                    {...fieldsForm.register('databaseName')}
                  />
                  {fe.databaseName && (
                    <p className="text-xs text-red-400">{fe.databaseName.message}</p>
                  )}
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div className="space-y-1.5">
                    <Label className="text-(--text-secondary) text-sm">Username</Label>
                    <Input
                      placeholder="postgres"
                      className="bg-(--bg-elevated) border-(--border-default) text-(--text-primary)"
                      {...fieldsForm.register('username')}
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label className="text-(--text-secondary) text-sm">Password</Label>
                    <Input
                      type="password"
                      placeholder="••••••••"
                      className="bg-(--bg-elevated) border-(--border-default) text-(--text-primary)"
                      {...fieldsForm.register('password')}
                    />
                  </div>
                </div>
              </section>

              <section className="space-y-3 p-5 rounded-xl border border-(--border-default) bg-(--bg-surface)">
                <h3 className="text-sm font-medium text-(--text-primary)">Options</h3>
                <label className="flex items-center gap-3 cursor-pointer">
                  <input
                    type="checkbox"
                    className="w-4 h-4 rounded border-(--border-strong) accent-violet-500"
                    {...fieldsForm.register('useSSL')}
                  />
                  <span className="text-sm text-(--text-secondary)">Use SSL/TLS</span>
                </label>
                <div className="space-y-1.5">
                  <Label className="text-(--text-secondary) text-sm">
                    Additional Parameters{' '}
                    <span className="text-(--text-muted) font-normal">(optional)</span>
                  </Label>
                  <Input
                    placeholder="key=value;key2=value2"
                    className="bg-(--bg-elevated) border-(--border-default) text-(--text-primary)"
                    {...fieldsForm.register('additionalParameters')}
                  />
                </div>
              </section>
            </form>
          )}

          {/* ── Connection string form ── */}
          {inputMode === 'RawConnectionString' && (
            <form
              id="conn-form"
              onSubmit={connStringForm.handleSubmit(handleConnStringSubmit)}
              className="space-y-5"
            >
              <section className="space-y-4 p-5 rounded-xl border border-(--border-default) bg-(--bg-surface)">
                <h3 className="text-sm font-medium text-(--text-primary)">Connection Details</h3>

                <div className="space-y-1.5">
                  <Label className="text-(--text-secondary) text-sm">Connection Name</Label>
                  <Input
                    placeholder="My Production DB"
                    className="bg-(--bg-elevated) border-(--border-default) text-(--text-primary)"
                    {...connStringForm.register('connectionName')}
                  />
                  {ce.connectionName && (
                    <p className="text-xs text-red-400">{ce.connectionName.message}</p>
                  )}
                </div>

                <div className="space-y-1.5">
                  <Label className="text-(--text-secondary) text-sm">Connection String</Label>
                  <textarea
                    rows={4}
                    placeholder={
                      selectedProvider === 'PostgreSql'
                        ? 'Host=localhost;Port=5432;Database=mydb;Username=postgres;Password=secret'
                        : selectedProvider === 'MySql'
                        ? 'Server=localhost;Port=3306;Database=mydb;Uid=root;Pwd=secret'
                        : 'Server=localhost,1433;Database=mydb;User Id=sa;Password=secret'
                    }
                    className="w-full rounded-lg border border-(--border-default) bg-(--bg-elevated) text-(--text-primary) text-sm px-3 py-2 font-mono resize-y min-h-[100px] outline-none focus:border-violet-500/60 transition-colors"
                    {...connStringForm.register('rawConnectionString')}
                  />
                  {ce.rawConnectionString && (
                    <p className="text-xs text-red-400">{ce.rawConnectionString.message}</p>
                  )}
                </div>
              </section>
            </form>
          )}

          {/* ── Test + Save actions ── */}
          <div className="flex items-center gap-3">
            <Button
              type="button"
              variant="outline"
              className="border-(--border-strong) text-(--text-secondary) hover:text-(--text-primary) gap-2"
              onClick={handleTest}
              disabled={testStatus === 'testing'}
            >
              {testStatus === 'idle' && (
                <>
                  <ToggleLeft className="w-4 h-4" />
                  Test Connection
                </>
              )}
              {testStatus === 'testing' && (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  Testing…
                </>
              )}
              {testStatus === 'ok' && (
                <>
                  <CheckCircle2 className="w-4 h-4 text-green-400" />
                  Connected!
                </>
              )}
              {testStatus === 'fail' && (
                <>
                  <AlertCircle className="w-4 h-4 text-red-400" />
                  Retry Test
                </>
              )}
            </Button>

            {testMessage && (
              <span
                className={cn(
                  'text-xs',
                  testStatus === 'ok' ? 'text-green-400' : 'text-red-400'
                )}
              >
                {testMessage}
              </span>
            )}

            <div className="ml-auto flex gap-2">
              <Button
                type="button"
                variant="ghost"
                className="text-(--text-muted)"
                onClick={() => navigate('/connections')}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                form="conn-form"
                disabled={isSubmitting}
                className="bg-violet-600 hover:bg-violet-500 text-white min-w-32"
              >
                {isSubmitting ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <>
                    <ToggleRight className="w-4 h-4 mr-1.5" />
                    Save Connection
                  </>
                )}
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
