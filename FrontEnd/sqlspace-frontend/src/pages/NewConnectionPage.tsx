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
import { DB_ICONS } from '@/components/icons/dbIconMap'

// ── Providers ─────────────────────────────────────────────────

const PROVIDERS: {
  value: DBProvider
  label: string
  description: string
  defaultPort: number
  color: string
}[] = [
  {
    value: 'PostgreSql',
    label: 'PostgreSQL',
    description: 'Open-source relational database',
    defaultPort: 5432,
    color: 'border-blue-500/50 bg-blue-500/10 text-blue-300',
  },
  {
    value: 'MySql',
    label: 'MySQL',
    description: 'Popular open-source RDBMS',
    defaultPort: 3306,
    color: 'border-orange-500/50 bg-orange-500/10 text-orange-300',
  },
  {
    value: 'SqlServer',
    label: 'SQL Server',
    description: 'Microsoft SQL Server',
    defaultPort: 1433,
    color: 'border-red-500/50 bg-red-500/10 text-red-300',
  },
  {
    value: 'MariaDb',
    label: 'MariaDB',
    description: 'MySQL-compatible open-source fork',
    defaultPort: 3306,
    color: 'border-amber-500/50 bg-amber-500/10 text-amber-300',
  },
  {
    value: 'CockroachDb',
    label: 'CockroachDB',
    description: 'Distributed SQL database',
    defaultPort: 26257,
    color: 'border-green-500/50 bg-green-500/10 text-green-300',
  },
  {
    value: 'Supabase',
    label: 'Supabase',
    description: 'Hosted PostgreSQL platform',
    defaultPort: 5432,
    color: 'border-emerald-500/50 bg-emerald-500/10 text-emerald-300',
  },
  {
    value: 'PlanetScale',
    label: 'PlanetScale',
    description: 'Serverless MySQL-compatible DB',
    defaultPort: 3306,
    color: 'border-sky-500/50 bg-sky-500/10 text-sky-300',
  },
  {
    value: 'Redshift',
    label: 'Amazon Redshift',
    description: 'AWS cloud data warehouse',
    defaultPort: 5439,
    color: 'border-yellow-500/50 bg-yellow-500/10 text-yellow-300',
  },
]

// ── Zod schemas ───────────────────────────────────────────────

const ALL_PROVIDERS = ['PostgreSql', 'MySql', 'SqlServer', 'MariaDb', 'CockroachDb', 'Supabase', 'PlanetScale', 'Redshift'] as const

const fieldsSchema = z.object({
  connectionName: z.string().min(1, 'Name is required'),
  databaseProvider: z.enum(ALL_PROVIDERS),
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
  databaseProvider: z.enum(ALL_PROVIDERS),
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

  const fieldsForm = useForm<FieldsForm>({
    resolver: zodResolver(fieldsSchema),
    defaultValues: {
      databaseProvider: 'PostgreSql',
      port: 5432,
      useSSL: false,
    },
  })

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

  const inputClass = "bg-[#18181b] border-white/10 text-white placeholder:text-zinc-600 focus:border-sky-500 focus:ring-1 focus:ring-sky-500 transition-all"

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex items-center gap-3 px-6 py-4 border-b border-white/10 bg-[#111113] shrink-0">
        <Button
          variant="ghost"
          size="icon"
          className="w-8 h-8 text-zinc-500 hover:text-zinc-200 hover:bg-white/5"
          onClick={() => navigate('/connections')}
        >
          <ArrowLeft className="w-4 h-4" />
        </Button>
        <h1 className="text-lg font-semibold text-white">New Connection</h1>
      </div>

      {/* Body */}
      <div className="flex-1 min-h-0 overflow-y-auto bg-[#080809]">
        <div className="max-w-2xl mx-auto px-6 py-8 space-y-8">

          {/* ── Provider selection ── */}
          <section className="space-y-3">
            <h2 className="text-sm font-medium text-zinc-500 uppercase tracking-wider">
              Database Provider
            </h2>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
              {PROVIDERS.map((p) => {
                const Icon = DB_ICONS[p.value]
                return (
                  <button
                    key={p.value}
                    type="button"
                    onClick={() => handleProviderSelect(p)}
                    className={cn(
                      'p-3 rounded-xl border text-left transition-all',
                      selectedProvider === p.value
                        ? p.color + ' border-2'
                        : 'border-white/10 bg-[#111113] text-zinc-400 hover:border-white/20 hover:bg-[#18181b]'
                    )}
                  >
                    <Icon size={28} className="mb-2" />
                    <div className="text-sm font-semibold leading-tight">{p.label}</div>
                    <p className="text-xs opacity-70 mt-0.5 leading-tight">{p.description}</p>
                  </button>
                )
              })}
            </div>
          </section>

          {/* ── Input mode toggle ── */}
          <section className="space-y-3">
            <h2 className="text-sm font-medium text-zinc-500 uppercase tracking-wider">
              Connection Method
            </h2>
            <div className="flex items-center gap-1 p-1 rounded-lg bg-[#18181b] border border-white/10 w-fit">
              <button
                type="button"
                onClick={() => setInputMode('IndividualFields')}
                className={cn(
                  'px-4 py-1.5 rounded-md text-sm font-medium transition-colors',
                  inputMode === 'IndividualFields'
                    ? 'bg-sky-600 text-white'
                    : 'text-zinc-500 hover:text-zinc-300'
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
                    ? 'bg-sky-600 text-white'
                    : 'text-zinc-500 hover:text-zinc-300'
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
              <section className="space-y-4 p-5 rounded-xl border border-white/10 bg-[#111113]">
                <h3 className="text-sm font-medium text-zinc-200">Connection Details</h3>

                <div className="space-y-1.5">
                  <Label className="text-zinc-400 text-sm">Connection Name</Label>
                  <Input
                    placeholder="My Production DB"
                    className={inputClass}
                    {...fieldsForm.register('connectionName')}
                  />
                  {fe.connectionName && (
                    <p className="text-xs text-red-400">{fe.connectionName.message}</p>
                  )}
                </div>

                <div className="grid grid-cols-3 gap-3">
                  <div className="col-span-2 space-y-1.5">
                    <Label className="text-zinc-400 text-sm">Host</Label>
                    <Input
                      placeholder="localhost"
                      className={inputClass}
                      {...fieldsForm.register('host')}
                    />
                    {fe.host && <p className="text-xs text-red-400">{fe.host.message}</p>}
                  </div>
                  <div className="space-y-1.5">
                    <Label className="text-zinc-400 text-sm">Port</Label>
                    <Input
                      type="number"
                      className={inputClass}
                      {...fieldsForm.register('port')}
                    />
                    {fe.port && <p className="text-xs text-red-400">{fe.port.message}</p>}
                  </div>
                </div>

                <div className="space-y-1.5">
                  <Label className="text-zinc-400 text-sm">Database Name</Label>
                  <Input
                    placeholder="my_database"
                    className={inputClass}
                    {...fieldsForm.register('databaseName')}
                  />
                  {fe.databaseName && (
                    <p className="text-xs text-red-400">{fe.databaseName.message}</p>
                  )}
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div className="space-y-1.5">
                    <Label className="text-zinc-400 text-sm">Username</Label>
                    <Input
                      placeholder="postgres"
                      className={inputClass}
                      {...fieldsForm.register('username')}
                    />
                  </div>
                  <div className="space-y-1.5">
                    <Label className="text-zinc-400 text-sm">Password</Label>
                    <Input
                      type="password"
                      placeholder="••••••••"
                      className={inputClass}
                      {...fieldsForm.register('password')}
                    />
                  </div>
                </div>
              </section>

              <section className="space-y-3 p-5 rounded-xl border border-white/10 bg-[#111113]">
                <h3 className="text-sm font-medium text-zinc-200">Options</h3>
                <label className="flex items-center gap-3 cursor-pointer">
                  <input
                    type="checkbox"
                    className="w-4 h-4 rounded border-white/10 accent-sky-500"
                    {...fieldsForm.register('useSSL')}
                  />
                  <span className="text-sm text-zinc-400">Use SSL/TLS</span>
                </label>
                <div className="space-y-1.5">
                  <Label className="text-zinc-400 text-sm">
                    Additional Parameters{' '}
                    <span className="text-zinc-600 font-normal">(optional)</span>
                  </Label>
                  <Input
                    placeholder="key=value;key2=value2"
                    className={inputClass}
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
              <section className="space-y-4 p-5 rounded-xl border border-white/10 bg-[#111113]">
                <h3 className="text-sm font-medium text-zinc-200">Connection Details</h3>

                <div className="space-y-1.5">
                  <Label className="text-zinc-400 text-sm">Connection Name</Label>
                  <Input
                    placeholder="My Production DB"
                    className={inputClass}
                    {...connStringForm.register('connectionName')}
                  />
                  {ce.connectionName && (
                    <p className="text-xs text-red-400">{ce.connectionName.message}</p>
                  )}
                </div>

                <div className="space-y-1.5">
                  <Label className="text-zinc-400 text-sm">Connection String</Label>
                  <textarea
                    rows={4}
                    placeholder={
                      selectedProvider === 'SqlServer'
                        ? 'Server=localhost,1433;Database=mydb;User Id=sa;Password=secret'
                        : selectedProvider === 'MySql' || selectedProvider === 'MariaDb' || selectedProvider === 'PlanetScale'
                        ? 'Server=localhost;Port=3306;Database=mydb;Uid=root;Pwd=secret'
                        : selectedProvider === 'CockroachDb'
                        ? 'Host=localhost;Port=26257;Database=mydb;Username=root;Password=secret'
                        : selectedProvider === 'Redshift'
                        ? 'Host=mycluster.us-east-1.redshift.amazonaws.com;Port=5439;Database=mydb;Username=awsuser;Password=secret'
                        : 'Host=localhost;Port=5432;Database=mydb;Username=postgres;Password=secret'
                    }
                    className="w-full rounded-xl border border-white/10 bg-[#18181b] text-white text-sm px-3 py-2 font-mono resize-y min-h-25 outline-none focus:border-sky-500 focus:ring-1 focus:ring-sky-500 transition-all placeholder:text-zinc-600"
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
              className="border-white/10 text-zinc-400 hover:text-zinc-200 hover:bg-white/5 gap-2"
              onClick={handleTest}
              disabled={testStatus === 'testing'}
            >
              {testStatus === 'idle' && (
                <><ToggleLeft className="w-4 h-4" />Test Connection</>
              )}
              {testStatus === 'testing' && (
                <><Loader2 className="w-4 h-4 animate-spin" />Testing…</>
              )}
              {testStatus === 'ok' && (
                <><CheckCircle2 className="w-4 h-4 text-green-400" />Connected!</>
              )}
              {testStatus === 'fail' && (
                <><AlertCircle className="w-4 h-4 text-red-400" />Retry Test</>
              )}
            </Button>

            {testMessage && (
              <span className={cn('text-xs', testStatus === 'ok' ? 'text-green-400' : 'text-red-400')}>
                {testMessage}
              </span>
            )}

            <div className="ml-auto flex gap-2">
              <Button
                type="button"
                variant="ghost"
                className="text-zinc-600 hover:text-zinc-300 hover:bg-white/5"
                onClick={() => navigate('/connections')}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                form="conn-form"
                disabled={isSubmitting}
                className="bg-sky-600 hover:bg-sky-500 text-white min-w-32 shadow-lg shadow-sky-500/25 active:scale-[0.98] transition-all"
              >
                {isSubmitting ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <><ToggleRight className="w-4 h-4 mr-1.5" />Save Connection</>
                )}
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
