'use client'

import { useState, useEffect } from 'react'
import { useSession } from 'next-auth/react'
// import { motion } from 'framer-motion'
import {
  ChartBarIcon,
  CubeIcon,
  RocketLaunchIcon,
  KeyIcon,
  ClockIcon,
  CheckCircleIcon,
  ExclamationTriangleIcon,
  ArrowUpIcon,
  ArrowDownIcon,
  EyeIcon,
  CogIcon,
} from '@heroicons/react/24/outline'
import Link from 'next/link'
import { useRouter } from 'next/navigation'

interface DashboardData {
  user: {
    id: string
    name: string
    email: string
    plan: string
    memberSince: string
  }
  metrics: {
    overview: {
      totalProjects: number
      totalDeployments: number
      totalInteractions: number
      activeApiKeys: number
    }
    usage: {
      daily: Array<{ date: string; interactions: number; deployments: number }>
    }
    services: {
      popular: Array<{ service: string; usage: number; percentage: number }>
    }
    costs: {
      gasUsed: { current: number; previous: number; change: number }
      apiCalls: { current: number; previous: number; change: number }
      storage: { current: number; previous: number; change: number }
    }
  }
  realtime: {
    systemHealth: string
    activeConnections: number
    averageResponseTime: number
    uptime: string
  }
  activity: Array<{
    id: number
    type: string
    message: string
    timestamp: string
    status: string
  }>
}

function StatCard({ title, value, change, icon: Icon, trend }: {
  title: string
  value: string | number
  change?: number
  icon: any
  trend?: 'up' | 'down' | 'neutral'
}) {
  return (
    <div className="glass rounded-xl p-6">
      <div className="flex items-center justify-between">
        <div>
          <p className="text-sm text-gray-400">{title}</p>
          <p className="text-2xl font-bold text-white mt-1">{value}</p>
          {change !== undefined && (
            <div className="flex items-center mt-2">
              {trend === 'up' && <ArrowUpIcon className="h-4 w-4 text-green-500 mr-1" />}
              {trend === 'down' && <ArrowDownIcon className="h-4 w-4 text-red-500 mr-1" />}
              <span className={`text-sm ${
                trend === 'up' ? 'text-green-500' : 
                trend === 'down' ? 'text-red-500' : 'text-gray-400'
              }`}>
                {change > 0 ? '+' : ''}{change}% from last month
              </span>
            </div>
          )}
        </div>
        <div className="p-3 bg-neo-500/10 rounded-lg">
          <Icon className="h-6 w-6 text-neo-500" />
        </div>
      </div>
    </div>
  )
}

function ActivityItem({ activity }: { activity: DashboardData['activity'][0] }) {
  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'success':
        return <CheckCircleIcon className="h-5 w-5 text-green-500" />
      case 'error':
        return <ExclamationTriangleIcon className="h-5 w-5 text-red-500" />
      default:
        return <ClockIcon className="h-5 w-5 text-yellow-500" />
    }
  }

  const getTypeColor = (type: string) => {
    switch (type) {
      case 'deployment':
        return 'text-blue-400'
      case 'api_call':
        return 'text-green-400'
      case 'contract_interaction':
        return 'text-purple-400'
      case 'error':
        return 'text-red-400'
      default:
        return 'text-gray-400'
    }
  }

  return (
    <div className="flex items-start space-x-3 p-3 rounded-lg hover:bg-gray-800/50 transition-colors">
      {getStatusIcon(activity.status)}
      <div className="flex-1 min-w-0">
        <p className="text-sm text-white">{activity.message}</p>
        <div className="flex items-center space-x-2 mt-1">
          <span className={`text-xs px-2 py-1 rounded ${getTypeColor(activity.type)} bg-gray-800`}>
            {activity.type.replace('_', ' ')}
          </span>
          <span className="text-xs text-gray-500">
            {new Date(activity.timestamp).toLocaleTimeString()}
          </span>
        </div>
      </div>
    </div>
  )
}

export default function DashboardPage() {
  const { data: session, status } = useSession()
  const router = useRouter()
  const [dashboardData, setDashboardData] = useState<DashboardData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (status === 'unauthenticated') {
      router.push('/auth/signin')
      return
    }

    if (status === 'authenticated') {
      fetchDashboardData()
    }
  }, [status, router])

  const fetchDashboardData = async () => {
    try {
      const response = await fetch('/api/analytics/dashboard')
      if (!response.ok) {
        throw new Error('Failed to fetch dashboard data')
      }
      const result = await response.json()
      if (result.success) {
        setDashboardData(result.data)
      } else {
        throw new Error(result.error || 'Unknown error')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load dashboard')
    } finally {
      setLoading(false)
    }
  }

  if (status === 'loading' || loading) {
    return (
      <div className="min-h-screen bg-background-primary pt-20 flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-neo-500"></div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="min-h-screen bg-background-primary pt-20 flex items-center justify-center">
        <div className="text-center">
          <ExclamationTriangleIcon className="h-12 w-12 text-red-500 mx-auto mb-4" />
          <h2 className="text-xl font-semibold text-white mb-2">Error Loading Dashboard</h2>
          <p className="text-gray-400 mb-4">{error}</p>
          <button
            onClick={fetchDashboardData}
            className="px-4 py-2 bg-neo-500 text-white rounded-lg hover:bg-neo-600 transition-colors"
          >
            Retry
          </button>
        </div>
      </div>
    )
  }

  if (!dashboardData) return null

  return (
    <div className="min-h-screen bg-background-primary pt-20">
      <div className="mx-auto max-w-7xl px-6 lg:px-8 py-12">
        {/* Header */}
        <div className="mb-8">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-white">
                Welcome back, {dashboardData.user.name}
              </h1>
              <p className="text-gray-400 mt-1">
                Here's what's happening with your Neo Service Layer projects
              </p>
            </div>
            <div className="flex items-center space-x-4">
              <Link
                href="/playground"
                className="flex items-center space-x-2 px-4 py-2 bg-neo-500 text-white rounded-lg hover:bg-neo-600 transition-colors"
              >
                <CubeIcon className="h-5 w-5" />
                <span>Playground</span>
              </Link>
              <Link
                href="/profile"
                className="flex items-center space-x-2 px-4 py-2 glass text-white rounded-lg hover:border-neo-500/50 transition-colors"
              >
                <CogIcon className="h-5 w-5" />
                <span>Settings</span>
              </Link>
            </div>
          </div>
        </div>

        {/* Overview Stats */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          <StatCard
            title="Total Projects"
            value={dashboardData.metrics.overview.totalProjects}
            change={12}
            trend="up"
            icon={CubeIcon}
          />
          <StatCard
            title="Deployments"
            value={dashboardData.metrics.overview.totalDeployments}
            change={8}
            trend="up"
            icon={RocketLaunchIcon}
          />
          <StatCard
            title="API Interactions"
            value={dashboardData.metrics.overview.totalInteractions.toLocaleString()}
            change={-3}
            trend="down"
            icon={ChartBarIcon}
          />
          <StatCard
            title="Active API Keys"
            value={dashboardData.metrics.overview.activeApiKeys}
            icon={KeyIcon}
          />
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Usage Chart */}
          <div className="lg:col-span-2">
            <div className="glass rounded-xl p-6">
              <h3 className="text-xl font-semibold text-white mb-6">Usage Overview</h3>
              <div className="space-y-4">
                {dashboardData.metrics.usage.daily.slice(-7).map((day, index) => (
                  <div key={day.date} className="flex items-center space-x-4">
                    <div className="w-16 text-sm text-gray-400">
                      {new Date(day.date).toLocaleDateString('en', { weekday: 'short' })}
                    </div>
                    <div className="flex-1">
                      <div className="flex items-center space-x-2 mb-1">
                        <span className="text-sm text-white">Interactions</span>
                        <span className="text-sm text-neo-500">{day.interactions}</span>
                      </div>
                      <div className="w-full bg-gray-700 rounded-full h-2">
                        <div
                          className="bg-neo-500 h-2 rounded-full"
                          style={{ width: `${(day.interactions / 100) * 100}%` }}
                        />
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Real-time Status */}
          <div className="space-y-6">
            {/* System Health */}
            <div className="glass rounded-xl p-6">
              <h3 className="text-lg font-semibold text-white mb-4">System Status</h3>
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <span className="text-gray-400">Health</span>
                  <div className="flex items-center space-x-2">
                    <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                    <span className="text-green-500 capitalize">{dashboardData.realtime.systemHealth}</span>
                  </div>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-gray-400">Uptime</span>
                  <span className="text-white">{dashboardData.realtime.uptime}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-gray-400">Response Time</span>
                  <span className="text-white">{dashboardData.realtime.averageResponseTime}ms</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-gray-400">Active Connections</span>
                  <span className="text-white">{dashboardData.realtime.activeConnections}</span>
                </div>
              </div>
            </div>

            {/* Popular Services */}
            <div className="glass rounded-xl p-6">
              <h3 className="text-lg font-semibold text-white mb-4">Popular Services</h3>
              <div className="space-y-3">
                {dashboardData.metrics.services.popular.map((service, index) => (
                  <div key={service.service} className="flex items-center justify-between">
                    <div className="flex items-center space-x-2">
                      <div className="w-6 h-6 bg-neo-500/20 rounded flex items-center justify-center text-xs text-neo-500">
                        {index + 1}
                      </div>
                      <span className="text-gray-300">{service.service}</span>
                    </div>
                    <span className="text-white">{service.usage}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>

        {/* Recent Activity */}
        <div className="mt-8">
          <div className="glass rounded-xl p-6">
            <div className="flex items-center justify-between mb-6">
              <h3 className="text-xl font-semibold text-white">Recent Activity</h3>
              <Link
                href="/activity"
                className="flex items-center space-x-1 text-neo-500 hover:text-neo-400 transition-colors"
              >
                <span>View All</span>
                <EyeIcon className="h-4 w-4" />
              </Link>
            </div>
            <div className="space-y-2">
              {dashboardData.activity.map((activity) => (
                <ActivityItem key={activity.id} activity={activity} />
              ))}
            </div>
          </div>
        </div>

        {/* Usage Costs */}
        <div className="mt-8 grid grid-cols-1 md:grid-cols-3 gap-6">
          <StatCard
            title="Gas Used (GAS)"
            value={dashboardData.metrics.costs.gasUsed.current.toFixed(2)}
            change={dashboardData.metrics.costs.gasUsed.change}
            trend={dashboardData.metrics.costs.gasUsed.change > 0 ? 'up' : 'down'}
            icon={ChartBarIcon}
          />
          <StatCard
            title="API Calls"
            value={dashboardData.metrics.costs.apiCalls.current.toLocaleString()}
            change={dashboardData.metrics.costs.apiCalls.change}
            trend={dashboardData.metrics.costs.apiCalls.change > 0 ? 'up' : 'down'}
            icon={CubeIcon}
          />
          <StatCard
            title="Storage (MB)"
            value={dashboardData.metrics.costs.storage.current}
            change={dashboardData.metrics.costs.storage.change}
            trend={dashboardData.metrics.costs.storage.change > 0 ? 'up' : 'down'}
            icon={RocketLaunchIcon}
          />
        </div>
      </div>
    </div>
  )
}