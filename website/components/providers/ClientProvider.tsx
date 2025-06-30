'use client'

import dynamic from 'next/dynamic'
import { SessionProvider } from 'next-auth/react'
import { Toaster } from 'react-hot-toast'


interface ClientProviderProps {
  children: React.ReactNode
  session?: any
}

export function ClientProvider({ children, session }: ClientProviderProps) {
  return (
    <SessionProvider session={session}>
      {children}
      <Toaster
        position="top-right"
        toastOptions={{
          duration: 4000,
          style: {
            background: '#1f2937',
            color: '#f3f4f6',
            border: '1px solid #374151',
          },
          success: {
            iconTheme: {
              primary: '#10b981',
              secondary: '#1f2937',
            },
          },
          error: {
            iconTheme: {
              primary: '#ef4444',
              secondary: '#1f2937',
            },
          },
        }}
      />
    </SessionProvider>
  )
}