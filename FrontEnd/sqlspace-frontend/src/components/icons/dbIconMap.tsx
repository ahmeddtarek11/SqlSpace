import type { DBProvider } from '@/types'
import { DbIcon } from './DbIcons'

interface IconProps {
  className?: string
  size?: number
}

export const DB_ICONS: Record<DBProvider, React.FC<IconProps>> = {
  PostgreSql:  (props) => <DbIcon provider="PostgreSql"  {...props} />,
  MySql:       (props) => <DbIcon provider="MySql"       {...props} />,
  SqlServer:   (props) => <DbIcon provider="SqlServer"   {...props} />,
  MariaDb:     (props) => <DbIcon provider="MariaDb"     {...props} />,
  CockroachDb: (props) => <DbIcon provider="CockroachDb" {...props} />,
  Supabase:    (props) => <DbIcon provider="Supabase"    {...props} />,
  PlanetScale: (props) => <DbIcon provider="PlanetScale" {...props} />,
  Redshift:    (props) => <DbIcon provider="Redshift"    {...props} />,
}
