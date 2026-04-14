import { useQuery } from '@tanstack/react-query'
import { accessApi } from '@/api/insights'

export function useConnectionIsAdmin(connectionId: string | null | undefined, enabled = true) {
  return useQuery({
    queryKey: ['connection-is-admin', connectionId],
    queryFn: () => accessApi.isAdmin(connectionId!),
    enabled: enabled && !!connectionId,
    staleTime: 60_000,
    retry: false,
  })
}
