'use client'

import { SessionProvider } from 'next-auth/react'
import { useState, useEffect } from 'react'

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [mounted, setMounted] = useState(false)

  useEffect(() => {
    setMounted(true)
  }, [])

  if (!mounted) {
    return <>{children}</>
  }

  return <SessionProvider>{children}</SessionProvider>
}