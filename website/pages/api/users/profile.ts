import type { NextApiRequest, NextApiResponse } from 'next'
import { getServerSession } from 'next-auth/next'
import { authOptions } from '@/lib/auth'
import { prisma } from '@/lib/prisma'

interface ProfileRequest {
  action: 'get' | 'update'
  profileData?: {
    displayName?: string
    bio?: string
    website?: string
    location?: string
    preferences?: any
  }
}

interface ProfileResponse {
  success: boolean
  data?: any
  error?: string
}

export default async function handler(
  req: NextApiRequest,
  res: NextApiResponse<ProfileResponse>
) {
  const session = await getServerSession(req, res, authOptions)
  if (!session?.user?.email) {
    return res.status(401).json({ success: false, error: 'Unauthorized' })
  }

  try {
    if (req.method === 'GET') {
      // Get user profile
      const user = await prisma.user.findUnique({
        where: { email: session.user.email },
        select: {
          id: true,
          name: true,
          email: true,
          image: true,
          emailVerified: true,
          createdAt: true,
          updatedAt: true,
          role: true,
          plan: true,
          projects: {
            select: {
              id: true,
              name: true,
              createdAt: true,
              status: true
            }
          },
          apiKeys: {
            select: {
              id: true,
              name: true,
              createdAt: true,
              lastUsedAt: true
            }
          },
          _count: {
            select: {
              projects: true,
              apiKeys: true,
              deployments: true,
              // serviceInteractions: true
            }
          }
        }
      })

      if (!user) {
        return res.status(404).json({ success: false, error: 'User not found' })
      }

      // Calculate usage statistics
      const usageStats = {
        totalProjects: user._count?.projects || 0,
        totalApiKeys: user._count?.apiKeys || 0,
        totalDeployments: user._count?.deployments || 0,
        totalInteractions: 0, // user._count?.serviceInteractions || 0,
        memberSince: user.createdAt,
        lastActivity: user.updatedAt
      }

      return res.status(200).json({
        success: true,
        data: {
          profile: {
            id: user.id,
            name: user.name,
            email: user.email,
            image: user.image,
            emailVerified: user.emailVerified,
            role: user.role,
            plan: user.plan
          },
          projects: user.projects || [],
          apiKeys: user.apiKeys || [],
          statistics: usageStats
        }
      })
    }

    if (req.method === 'PUT') {
      // Update user profile
      const { action, profileData }: ProfileRequest = req.body

      if (action !== 'update' || !profileData) {
        return res.status(400).json({ 
          success: false, 
          error: 'Invalid request. action must be "update" with profileData' 
        })
      }

      const updatedUser = await prisma.user.update({
        where: { email: session.user.email },
        data: {
          name: profileData.displayName,
          // Note: In a real implementation, you'd have additional profile fields
          updatedAt: new Date()
        },
        select: {
          id: true,
          name: true,
          email: true,
          image: true,
          updatedAt: true
        }
      })

      return res.status(200).json({
        success: true,
        data: {
          profile: updatedUser,
          message: 'Profile updated successfully'
        }
      })
    }

    return res.status(405).json({ success: false, error: 'Method not allowed' })

  } catch (error) {
    console.error('Profile API error:', error)
    return res.status(500).json({ 
      success: false, 
      error: error instanceof Error ? error.message : 'Internal server error' 
    })
  }
}