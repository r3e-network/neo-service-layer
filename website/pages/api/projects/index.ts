import type { NextApiRequest, NextApiResponse } from 'next'
import { getServerSession } from 'next-auth/next'
import { authOptions } from '@/lib/auth'
import { prisma } from '@/lib/prisma'

interface ProjectRequest {
  action: 'create' | 'list' | 'update' | 'delete'
  projectData?: {
    name: string
    description?: string
    services?: string[]
    configuration?: any
  }
  projectId?: string
}

interface ProjectResponse {
  success: boolean
  data?: any
  error?: string
}

export default async function handler(
  req: NextApiRequest,
  res: NextApiResponse<ProjectResponse>
) {
  const session = await getServerSession(req, res, authOptions)
  if (!session?.user?.email) {
    return res.status(401).json({ success: false, error: 'Unauthorized' })
  }

  try {
    const user = await prisma.user.findUnique({
      where: { email: session.user.email },
      select: { id: true }
    })

    if (!user) {
      return res.status(404).json({ success: false, error: 'User not found' })
    }

    if (req.method === 'GET') {
      // List user projects
      const projects = await prisma.project.findMany({
        where: { userId: user.id },
        include: {
          deployments: {
            select: {
              id: true,
              status: true,
              createdAt: true,
              environment: true
            },
            orderBy: { createdAt: 'desc' },
            take: 5
          },
          _count: {
            select: {
              deployments: true,
              // serviceInteractions: true
            }
          }
        },
        orderBy: { updatedAt: 'desc' }
      })

      return res.status(200).json({
        success: true,
        data: {
          projects: projects.map(project => ({
            ...project,
            stats: {
              totalDeployments: project._count?.deployments || 0,
              totalInteractions: 0, // project._count?.serviceInteractions || 0,
              lastDeployment: project.deployments?.[0]?.createdAt || null
            }
          })),
          total: projects.length
        }
      })
    }

    if (req.method === 'POST') {
      // Create or manage project
      const { action, projectData, projectId }: ProjectRequest = req.body

      switch (action) {
        case 'create':
          if (!projectData?.name) {
            return res.status(400).json({ 
              success: false, 
              error: 'Project name is required' 
            })
          }

          const newProject = await prisma.project.create({
            data: {
              name: projectData.name,
              description: projectData.description || '',
              userId: user.id,
              status: 'ACTIVE',
              config: projectData.configuration || {}
            }
          })

          // Create initial API key for the project
          const apiKey = await prisma.apiKey.create({
            data: {
              name: `${projectData.name} - Default Key`,
              hashedKey: `neo_${Math.random().toString(36).substr(2, 32)}`,
              key: `neo_${Math.random().toString(36).substr(2, 32)}`,
              userId: user.id,
              // projectId: newProject.id // Remove this line as it's not in schema
            }
          })

          return res.status(201).json({
            success: true,
            data: {
              project: newProject,
              apiKey: {
                id: apiKey.id,
                name: apiKey.name,
                key: apiKey.hashedKey
              }
            }
          })

        case 'update':
          if (!projectId || !projectData) {
            return res.status(400).json({ 
              success: false, 
              error: 'projectId and projectData are required for update' 
            })
          }

          const updatedProject = await prisma.project.update({
            where: { 
              id: projectId,
              userId: user.id  // Ensure user owns the project
            },
            data: {
              name: projectData.name,
              description: projectData.description,
              config: projectData.configuration || {},
              updatedAt: new Date()
            }
          })

          return res.status(200).json({
            success: true,
            data: { project: updatedProject }
          })

        case 'delete':
          if (!projectId) {
            return res.status(400).json({ 
              success: false, 
              error: 'projectId is required for delete' 
            })
          }

          await prisma.project.update({
            where: { 
              id: projectId,
              userId: user.id
            },
            data: {
              status: 'DELETED',
              updatedAt: new Date()
            }
          })

          return res.status(200).json({
            success: true,
            data: { message: 'Project deleted successfully' }
          })

        default:
          return res.status(400).json({ 
            success: false, 
            error: 'Invalid action. Supported actions: create, update, delete' 
          })
      }
    }

    return res.status(405).json({ success: false, error: 'Method not allowed' })

  } catch (error) {
    console.error('Projects API error:', error)
    return res.status(500).json({ 
      success: false, 
      error: error instanceof Error ? error.message : 'Internal server error' 
    })
  }
}