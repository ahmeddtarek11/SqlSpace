import type { DBProvider } from '@/types'

interface IconProps {
  className?: string
  size?: number
}

const DB_ICON_PATHS: Record<DBProvider, string> = {
  PostgreSql:  '/db-icons/postgresql.svg',
  MySql:       '/db-icons/mysql.svg',
  SqlServer:   '/db-icons/sqlserver.svg',
  MariaDb:     '/db-icons/mariadb.svg',
  CockroachDb: '/db-icons/cockroachdb.svg',
  Supabase:    '/db-icons/supabase.svg',
  PlanetScale: '/db-icons/planetscale.svg',
  Redshift:    '/db-icons/redshift.svg',
}

function DbIcon({ provider, size = 24, className }: { provider: DBProvider } & IconProps) {
  return (
    <img
      src={DB_ICON_PATHS[provider]}
      alt={provider}
      width={size}
      height={size}
      className={className}
      draggable={false}
    />
  )
}

// ── PostgreSQL (#336791) ───────────────────────────────────────
export function PostgreSQLIcon({ className, size = 24 }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" className={className} fill="none" xmlns="http://www.w3.org/2000/svg">
      {/* Head */}
      <circle cx="12" cy="10" r="7.5" fill="#336791" />
      {/* Trunk curves right and down */}
      <path d="M18 12.5 Q22.5 13.5 21.5 19.5 Q20.5 21.5 18.5 20.5"
        stroke="#336791" strokeWidth="2.8" fill="none" strokeLinecap="round" />
      {/* Left ear bump */}
      <path d="M5.5 8.5 Q3 5.5 5.5 2.5" stroke="#336791" strokeWidth="2.5"
        fill="none" strokeLinecap="round" />
      {/* Eye */}
      <circle cx="14.5" cy="8" r="1.4" fill="white" />
      <circle cx="14.9" cy="8" r="0.65" fill="#1a1a2e" />
      {/* Tusk */}
      <path d="M8.5 15.5 Q7.5 18.5 10 19"
        stroke="white" strokeWidth="1.4" fill="none" strokeLinecap="round" />
    </svg>
  )
}

// ── MySQL (#4479A1 + #F29111) ──────────────────────────────────
export function MySQLIcon({ className, size = 24 }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" className={className} fill="none" xmlns="http://www.w3.org/2000/svg">
      {/* Body */}
      <path d="M4.5 14.5 Q6 6 13.5 8 Q20 9.5 18.5 16.5 Q16.5 22 10 20.5 Q5.5 18.5 4.5 14.5Z"
        fill="#4479A1" />
      {/* Dorsal fin */}
      <path d="M11 8.5 Q13.5 3 16.5 7"
        stroke="#F29111" strokeWidth="2.2" fill="none" strokeLinecap="round" />
      {/* Upper tail fork */}
      <path d="M4.5 14.5 Q0.5 12 1.5 8"
        stroke="#4479A1" strokeWidth="2.8" fill="none" strokeLinecap="round" />
      {/* Lower tail fork */}
      <path d="M4.5 14.5 Q0.5 17 1.5 21"
        stroke="#4479A1" strokeWidth="2.8" fill="none" strokeLinecap="round" />
      {/* Eye */}
      <circle cx="15.5" cy="12" r="1.3" fill="white" />
      <circle cx="15.85" cy="12" r="0.6" fill="#1a1a2e" />
    </svg>
  )
}

// ── SQL Server — Microsoft Windows logo (exact brand) ──────────
export function SqlServerIcon({ className, size = 24 }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" className={className} xmlns="http://www.w3.org/2000/svg">
      <rect x="1"    y="1"    width="10.5" height="10.5" rx="0.5" fill="#F25022" />
      <rect x="12.5" y="1"    width="10.5" height="10.5" rx="0.5" fill="#7FBA00" />
      <rect x="1"    y="12.5" width="10.5" height="10.5" rx="0.5" fill="#00A4EF" />
      <rect x="12.5" y="12.5" width="10.5" height="10.5" rx="0.5" fill="#FFB900" />
    </svg>
  )
}

// ── MariaDB (#003545 + #C0392B) ────────────────────────────────
export function MariaDBIcon({ className, size = 24 }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" className={className} fill="none" xmlns="http://www.w3.org/2000/svg">
      {/* Body */}
      <ellipse cx="13.5" cy="13.5" rx="8.5" ry="5.5" fill="#003545" />
      {/* Head */}
      <circle cx="20.5" cy="11" r="3.5" fill="#003545" />
      {/* Tail upper */}
      <path d="M5 13.5 Q1 11 2 7.5"
        stroke="#003545" strokeWidth="3" fill="none" strokeLinecap="round" />
      {/* Tail lower */}
      <path d="M5 13.5 Q1 16 2 19.5"
        stroke="#003545" strokeWidth="3" fill="none" strokeLinecap="round" />
      {/* Dorsal fin */}
      <path d="M11 8 Q13 3 16.5 7.5"
        stroke="#C0392B" strokeWidth="2.2" strokeLinecap="round" fill="none" />
      {/* Eye */}
      <circle cx="21" cy="10" r="1.3" fill="white" />
      <circle cx="21.35" cy="10" r="0.6" fill="#1a1a2e" />
    </svg>
  )
}

// ── CockroachDB (#6933FF) ──────────────────────────────────────
export function CockroachDBIcon({ className, size = 24 }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" className={className} fill="none" xmlns="http://www.w3.org/2000/svg">
      {/* Hexagonal body */}
      <path d="M12 2 L20.66 7 L20.66 17 L12 22 L3.34 17 L3.34 7 Z" fill="#6933FF" />
      {/* Left antenna */}
      <line x1="10" y1="7" x2="6.5" y2="2.5" stroke="white" strokeWidth="1.6" strokeLinecap="round" />
      {/* Right antenna */}
      <line x1="14" y1="7" x2="17.5" y2="2.5" stroke="white" strokeWidth="1.6" strokeLinecap="round" />
      {/* Head */}
      <ellipse cx="12" cy="9.5" rx="2.8" ry="2" fill="white" opacity="0.9" />
      {/* Thorax */}
      <ellipse cx="12" cy="13.5" rx="3.5" ry="2.5" fill="white" opacity="0.65" />
      {/* Abdomen */}
      <ellipse cx="12" cy="17.5" rx="2.8" ry="2" fill="white" opacity="0.45" />
      {/* Left legs */}
      <line x1="9.2" y1="11" x2="5.5"  y2="9.5"  stroke="white" strokeWidth="1.1" strokeLinecap="round" />
      <line x1="8.5" y1="13.5" x2="4.5" y2="13"  stroke="white" strokeWidth="1.1" strokeLinecap="round" />
      <line x1="9.2" y1="16" x2="5.5"  y2="15.5" stroke="white" strokeWidth="1.1" strokeLinecap="round" />
      {/* Right legs */}
      <line x1="14.8" y1="11"  x2="18.5" y2="9.5"  stroke="white" strokeWidth="1.1" strokeLinecap="round" />
      <line x1="15.5" y1="13.5" x2="19.5" y2="13"  stroke="white" strokeWidth="1.1" strokeLinecap="round" />
      <line x1="14.8" y1="16"  x2="18.5" y2="15.5" stroke="white" strokeWidth="1.1" strokeLinecap="round" />
    </svg>
  )
}

// ── Supabase (#3ECF8E) — exact path from simple-icons (CC0) ────
export function SupabaseIcon({ className, size = 24 }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" className={className} xmlns="http://www.w3.org/2000/svg">
      <path
        fill="#3ECF8E"
        d="M11.9 1.036c-.015-.986-1.26-1.41-1.874-.637L.764 12.05C.199 12.801.752 13.9 1.686 13.9h7.396l.28 9.068c.015.986 1.26 1.409 1.874.637l9.262-11.652c.565-.75.012-1.849-.922-1.849h-7.396L11.9 1.036z"
      />
    </svg>
  )
}

// ── PlanetScale (black / dark) ─────────────────────────────────
export function PlanetScaleIcon({ className, size = 24 }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" className={className} fill="none" xmlns="http://www.w3.org/2000/svg">
      {/* Planet body */}
      <circle cx="12" cy="12" r="7.5" fill="#111" />
      {/* Orbital ring — tilted ellipse, clipped by planet */}
      <ellipse cx="12" cy="12" rx="11.5" ry="4" stroke="#555" strokeWidth="1.8"
        fill="none" transform="rotate(-35 12 12)" />
      {/* Re-draw planet on top of ring to create "behind" effect on lower arc */}
      <circle cx="12" cy="12" r="7.5" fill="#111" />
      {/* Upper orbital arc (in front of planet) */}
      <path d="M3.2 7.8 Q12 15.5 20.8 8.2"
        stroke="#888" strokeWidth="1.8" fill="none" strokeLinecap="round" />
      {/* Surface highlight */}
      <circle cx="9" cy="9" r="2.5" fill="#222" />
      <circle cx="14.5" cy="13.5" r="1.8" fill="#1a1a1a" />
    </svg>
  )
}

// ── Amazon Redshift (#8C4FFF + #FF9900) ───────────────────────
export function RedshiftIcon({ className, size = 24 }: IconProps) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" className={className} xmlns="http://www.w3.org/2000/svg">
      {/* Top disc */}
      <ellipse cx="12" cy="5.5" rx="9" ry="3" fill="#8C4FFF" />
      {/* Upper cylinder body */}
      <rect x="3" y="5.5" width="18" height="6.5" fill="#6B3CC7" />
      {/* Middle disc */}
      <ellipse cx="12" cy="12" rx="9" ry="3" fill="#8C4FFF" />
      {/* Lower cylinder body */}
      <rect x="3" y="12" width="18" height="6.5" fill="#5A2EA6" />
      {/* Bottom disc */}
      <ellipse cx="12" cy="18.5" rx="9" ry="3" fill="#8C4FFF" />
      {/* AWS orange accent stripe on top disc */}
      <path d="M5 5.5 Q12 2.5 19 5.5 Q12 3.8 5 5.5Z" fill="#FF9900" opacity="0.55" />
    </svg>
  )
}

// ── Map ────────────────────────────────────────────────────────
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
