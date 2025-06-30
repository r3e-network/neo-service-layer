import type { NextApiRequest, NextApiResponse } from 'next'
import { getServerSession } from 'next-auth/next'
import { authOptions } from '@/lib/auth'
import { prisma } from '@/lib/prisma'

interface AnalyticsResponse {
  success: boolean
  data?: any
  error?: string
}

interface DashboardMetrics {
  overview: {
    totalProjects: number
    totalDeployments: number
    totalInteractions: number
    activeApiKeys: number
  }
  usage: {
    daily: Array<{ date: string; interactions: number; deployments: number }>
    weekly: Array<{ week: string; interactions: number; deployments: number }>
    monthly: Array<{ month: string; interactions: number; deployments: number }>
  }
  services: {
    popular: Array<{ service: string; usage: number; percentage: number }>
    performance: Array<{ service: string; avgResponseTime: number; successRate: number }>
  }
  costs: {
    gasUsed: { current: number; previous: number; change: number }
    apiCalls: { current: number; previous: number; change: number }
    storage: { current: number; previous: number; change: number }
  }
}

// Mock data generators for demonstration
function generateMockDailyData(days: number) {
  const data = []
  for (let i = days - 1; i >= 0; i--) {
    const date = new Date()
    date.setDate(date.getDate() - i)
    data.push({
      date: date.toISOString().split('T')[0],
      interactions: Math.floor(Math.random() * 100) + 20,
      deployments: Math.floor(Math.random() * 10) + 1
    })
  }
  return data
}

function generateMockServiceData() {
  const services = [
    'Storage', 'Oracle', 'Randomness', 'CrossChain', 'Analytics', 
    'Compute', 'Identity', 'Marketplace', 'Voting', 'ZeroKnowledge'
  ]
  
  const popular = services.map(service => ({
    service,
    usage: Math.floor(Math.random() * 1000) + 100,
    percentage: Math.floor(Math.random() * 25) + 5
  })).sort((a, b) => b.usage - a.usage)

  const performance = services.map(service => ({
    service,
    avgResponseTime: Math.floor(Math.random() * 500) + 100,
    successRate: Math.floor(Math.random() * 10) + 90
  }))

  return { popular: popular.slice(0, 5), performance }
}

export default async function handler(
  req: NextApiRequest,
  res: NextApiResponse<AnalyticsResponse>
) {
  if (req.method !== 'GET') {
    return res.status(405).json({ success: false, error: 'Method not allowed' })
  }

  const session = await getServerSession(req, res, authOptions)
  if (!session?.user?.email) {
    return res.status(401).json({ success: false, error: 'Unauthorized' })
  }

  try {
    const user = await prisma.user.findUnique({
      where: { email: session.user.email },
      include: {
        _count: {
          select: {
            projects: true,
            deployments: true,
            // serviceInteractions: true,
            apiKeys: true
          }
        }
      }
    })

    if (!user) {
      return res.status(404).json({ success: false, error: 'User not found' })
    }

    // Generate comprehensive dashboard metrics
    const metrics: DashboardMetrics = {
      overview: {
        totalProjects: user._count?.projects || 0,
        totalDeployments: user._count?.deployments || 0,
        totalInteractions: 0, // user._count?.serviceInteractions || 0,
        activeApiKeys: user._count?.apiKeys || 0
      },
      usage: {
        daily: generateMockDailyData(30),
        weekly: generateMockDailyData(12).map((item, index) => ({
          week: `Week ${index + 1}`,
          interactions: item.interactions * 7,
          deployments: item.deployments * 2
        })),
        monthly: generateMockDailyData(6).map((item, index) => ({
          month: new Date(Date.now() - (index * 30 * 24 * 60 * 60 * 1000)).toLocaleString('default', { month: 'long' }),
          interactions: item.interactions * 30,
          deployments: item.deployments * 10
        }))
      },
      services: generateMockServiceData(),
      costs: {
        gasUsed: {
          current: Math.floor(Math.random() * 100) + 50,
          previous: Math.floor(Math.random() * 100) + 50,
          change: Math.floor(Math.random() * 40) - 20
        },
        apiCalls: {
          current: Math.floor(Math.random() * 10000) + 5000,
          previous: Math.floor(Math.random() * 10000) + 5000,
          change: Math.floor(Math.random() * 2000) - 1000
        },
        storage: {
          current: Math.floor(Math.random() * 1000) + 500,
          previous: Math.floor(Math.random() * 1000) + 500,
          change: Math.floor(Math.random() * 200) - 100
        }
      }
    }

    // Add real-time status
    const realtimeStatus = {
      systemHealth: 'healthy',
      activeConnections: Math.floor(Math.random() * 100) + 20,
      averageResponseTime: Math.floor(Math.random() * 200) + 50,
      uptime: '99.98%',
      lastUpdate: new Date().toISOString()
    }

    // Recent activity (mock data)
    const recentActivity = [
      {
        id: 1,
        type: 'deployment',
        message: 'Project "DeFi Analytics" deployed to mainnet',
        timestamp: new Date(Date.now() - 300000).toISOString(),
        status: 'success'
      },
      {
        id: 2,
        type: 'api_call',
        message: 'Oracle service requested for BTC/USD price',
        timestamp: new Date(Date.now() - 600000).toISOString(),
        status: 'success'
      },
      {
        id: 3,
        type: 'contract_interaction',
        message: 'Storage contract: data stored successfully',
        timestamp: new Date(Date.now() - 900000).toISOString(),
        status: 'success'
      },
      {
        id: 4,
        type: 'error',
        message: 'Cross-chain transfer failed: insufficient balance',
        timestamp: new Date(Date.now() - 1200000).toISOString(),
        status: 'error'
      }
    ]

    return res.status(200).json({
      success: true,
      data: {
        user: {
          id: user.id,
          name: user.name,
          email: user.email,
          plan: user.plan,
          memberSince: user.createdAt
        },
        metrics,
        realtime: realtimeStatus,
        activity: recentActivity,
        generatedAt: new Date().toISOString()
      }
    })

  } catch (error) {
    console.error('Analytics dashboard API error:', error)
    return res.status(500).json({ 
      success: false, 
      error: error instanceof Error ? error.message : 'Internal server error' 
    })
  }
}