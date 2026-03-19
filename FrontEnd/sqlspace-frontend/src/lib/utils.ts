import { type ClassValue, clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function formatMs(ms: number): string {
  if (ms < 1000) return `${parseFloat(ms.toFixed(2))}ms`
  return `${(ms / 1000).toFixed(2)}s`
}

export function formatNumber(n: number): string {
  return new Intl.NumberFormat().format(n)
}

export function formatDate(iso: string): string {
  return new Intl.DateTimeFormat('en-US', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(iso))
}

export function truncate(str: string, max = 80): string {
  return str.length <= max ? str : str.slice(0, max) + '…'
}
