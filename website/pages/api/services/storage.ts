import type { NextApiRequest, NextApiResponse } from 'next'
import { getServerSession } from 'next-auth/next'
import { authOptions } from '@/lib/auth'
import { neoService } from '@/lib/neo'

interface StorageRequest {
  action: 'store' | 'retrieve' | 'list' | 'delete'
  fileId?: string
  data?: string
  metadata?: any
}

interface StorageResponse {
  success: boolean
  data?: any
  error?: string
  transactionId?: string
  fileId?: string
}

export default async function handler(
  req: NextApiRequest,
  res: NextApiResponse<StorageResponse>
) {
  if (req.method !== 'POST') {
    return res.status(405).json({ success: false, error: 'Method not allowed' })
  }

  const session = await getServerSession(req, res, authOptions)
  if (!session) {
    return res.status(401).json({ success: false, error: 'Unauthorized' })
  }

  try {
    const { action, fileId, data, metadata }: StorageRequest = req.body

    switch (action) {
      case 'store':
        if (!fileId || !data) {
          return res.status(400).json({ 
            success: false, 
            error: 'fileId and data are required for store operation' 
          })
        }

        const storeData = {
          content: data,
          metadata: metadata || {},
          timestamp: Date.now(),
          owner: session.user?.email || 'anonymous'
        }

        const transactionId = await neoService.storeData(fileId, JSON.stringify(storeData))
        
        return res.status(200).json({
          success: true,
          transactionId,
          fileId,
          data: { stored: true, size: JSON.stringify(storeData).length }
        })

      case 'retrieve':
        if (!fileId) {
          return res.status(400).json({ 
            success: false, 
            error: 'fileId is required for retrieve operation' 
          })
        }

        const retrievedData = await neoService.retrieveData(fileId)
        if (!retrievedData) {
          return res.status(404).json({ 
            success: false, 
            error: 'File not found' 
          })
        }

        const parsedData = JSON.parse(retrievedData)
        
        return res.status(200).json({
          success: true,
          fileId,
          data: parsedData
        })

      case 'list':
        // Mock implementation for listing user files
        const userFiles = [
          {
            fileId: 'user-profile',
            name: 'User Profile',
            size: 256,
            created: new Date().toISOString(),
            type: 'profile'
          },
          {
            fileId: 'app-settings',
            name: 'Application Settings',
            size: 128,
            created: new Date(Date.now() - 86400000).toISOString(),
            type: 'settings'
          }
        ]

        return res.status(200).json({
          success: true,
          data: { files: userFiles, total: userFiles.length }
        })

      case 'delete':
        if (!fileId) {
          return res.status(400).json({ 
            success: false, 
            error: 'fileId is required for delete operation' 
          })
        }

        // Mock delete operation
        const deleteTransactionId = `delete_${Math.random().toString(36).substr(2, 16)}`
        
        return res.status(200).json({
          success: true,
          transactionId: deleteTransactionId,
          fileId,
          data: { deleted: true }
        })

      default:
        return res.status(400).json({ 
          success: false, 
          error: 'Invalid action. Supported actions: store, retrieve, list, delete' 
        })
    }
  } catch (error) {
    console.error('Storage API error:', error)
    return res.status(500).json({ 
      success: false, 
      error: error instanceof Error ? error.message : 'Internal server error' 
    })
  }
}