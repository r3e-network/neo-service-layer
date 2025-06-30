import type { NextApiRequest, NextApiResponse } from 'next'
import { prisma } from '@/lib/prisma'

type HealthStatus = {
  status: 'healthy' | 'degraded' | 'unhealthy'
  timestamp: string
  version: string
  services: {
    database: boolean
    neo: boolean
    nextauth: boolean
  }
  uptime: number
}

export default async function handler(
  req: NextApiRequest,
  res: NextApiResponse<HealthStatus | { error: string }>
) {
  if (req.method !== 'GET') {
    return res.status(405).json({ error: 'Method not allowed' })
  }

  const startTime = Date.now()
  const services = {
    database: false,
    neo: false,
    nextauth: false,
  }

  try {
    // Check database connection
    try {
      await prisma.$queryRaw`SELECT 1`
      services.database = true
    } catch (error) {
      console.error('Database health check failed:', error)
    }

    // Check Neo network connectivity
    try {
      const neoUrl = process.env.NEO_NETWORK === 'mainnet' 
        ? 'https://mainnet1.neo.coz.io:443'
        : 'https://testnet1.neo.coz.io:443'
      
      const response = await fetch(neoUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          jsonrpc: '2.0',
          method: 'getblockcount',
          params: [],
          id: 1,
        }),
        signal: AbortSignal.timeout(5000),
      })
      
      if (response.ok) {
        services.neo = true
      }
    } catch (error) {
      console.error('Neo network health check failed:', error)
    }

    // Check NextAuth configuration
    try {
      if (process.env.NEXTAUTH_SECRET && process.env.NEXTAUTH_URL) {
        services.nextauth = true
      }
    } catch (error) {
      console.error('NextAuth health check failed:', error)
    }

    // Determine overall status
    const healthyServices = Object.values(services).filter(Boolean).length
    const totalServices = Object.keys(services).length
    
    let status: HealthStatus['status'] = 'healthy'
    if (healthyServices === 0) {
      status = 'unhealthy'
    } else if (healthyServices < totalServices) {
      status = 'degraded'
    }

    const healthStatus: HealthStatus = {
      status,
      timestamp: new Date().toISOString(),
      version: process.env.npm_package_version || '1.0.0',
      services,
      uptime: process.uptime(),
    }

    // Set appropriate status code
    const statusCode = status === 'healthy' ? 200 : status === 'degraded' ? 503 : 500

    res.status(statusCode).json(healthStatus)
  } catch (error) {
    console.error('Health check error:', error)
    res.status(500).json({ error: 'Health check failed' })
  }
}