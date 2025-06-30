import type { NextApiRequest, NextApiResponse } from 'next'
import { getServerSession } from 'next-auth/next'
import { authOptions } from '@/lib/auth'
import { neoService } from '@/lib/neo'

interface OracleRequest {
  action: 'request' | 'get' | 'list'
  dataType?: string
  parameters?: any
  requestId?: string
}

interface OracleResponse {
  success: boolean
  data?: any
  error?: string
  requestId?: string
}

// Mock price data for demonstration
const mockPriceData: Record<string, number> = {
  'BTC': 67234.56,
  'ETH': 3432.12,
  'NEO': 18.45,
  'GAS': 4.23,
  'USDT': 1.00,
  'BNB': 312.45
}

export default async function handler(
  req: NextApiRequest,
  res: NextApiResponse<OracleResponse>
) {
  if (req.method !== 'POST') {
    return res.status(405).json({ success: false, error: 'Method not allowed' })
  }

  const session = await getServerSession(req, res, authOptions)
  if (!session) {
    return res.status(401).json({ success: false, error: 'Unauthorized' })
  }

  try {
    const { action, dataType, parameters, requestId }: OracleRequest = req.body

    switch (action) {
      case 'request':
        if (!dataType || !parameters) {
          return res.status(400).json({ 
            success: false, 
            error: 'dataType and parameters are required for request operation' 
          })
        }

        const newRequestId = await neoService.requestOracleData(dataType, parameters)
        
        // Simulate oracle response delay
        setTimeout(async () => {
          // In a real implementation, this would be handled by oracle nodes
          console.log(`Oracle request ${newRequestId} processed`)
        }, 1000)

        return res.status(200).json({
          success: true,
          requestId: newRequestId,
          data: { 
            status: 'pending',
            estimatedResponse: '2-5 seconds',
            dataType,
            parameters
          }
        })

      case 'get':
        if (!requestId) {
          return res.status(400).json({ 
            success: false, 
            error: 'requestId is required for get operation' 
          })
        }

        const response = await neoService.getOracleResponse(requestId)
        
        // Mock response based on request type
        let mockResponse: any = response
        if (requestId.includes('price')) {
          const symbol = requestId.split('_')[1]?.toUpperCase()
          if (symbol && mockPriceData[symbol]) {
            mockResponse = mockPriceData[symbol].toString()
          }
        }

        return res.status(200).json({
          success: true,
          requestId,
          data: {
            value: mockResponse,
            timestamp: Date.now(),
            status: 'completed',
            confidence: 0.98
          }
        })

      case 'list':
        // Mock list of recent oracle requests
        const recentRequests = [
          {
            requestId: 'req_btc_12345678',
            dataType: 'price_feed',
            status: 'completed',
            value: mockPriceData.BTC,
            timestamp: Date.now() - 30000
          },
          {
            requestId: 'req_eth_87654321',
            dataType: 'price_feed', 
            status: 'completed',
            value: mockPriceData.ETH,
            timestamp: Date.now() - 60000
          },
          {
            requestId: 'req_neo_11223344',
            dataType: 'price_feed',
            status: 'pending',
            value: null,
            timestamp: Date.now() - 5000
          }
        ]

        return res.status(200).json({
          success: true,
          data: {
            requests: recentRequests,
            total: recentRequests.length,
            pendingCount: recentRequests.filter(r => r.status === 'pending').length
          }
        })

      default:
        return res.status(400).json({ 
          success: false, 
          error: 'Invalid action. Supported actions: request, get, list' 
        })
    }
  } catch (error) {
    console.error('Oracle API error:', error)
    return res.status(500).json({ 
      success: false, 
      error: error instanceof Error ? error.message : 'Internal server error' 
    })
  }
}