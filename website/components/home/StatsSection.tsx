'use client'

import { useEffect, useState, useRef } from 'react'
import { AnimatedDiv } from '@/components/ui/AnimatedDiv'

interface Stat {
  id: string
  name: string
  value: string
  suffix?: string
  description: string
  icon: string
}

const stats: Stat[] = [
  {
    id: 'contracts',
    name: 'Smart Contracts',
    value: '34',
    suffix: '+',
    description: 'Production-ready service contracts',
    icon: '📦',
  },
  {
    id: 'deployments',
    name: 'Deployments',
    value: '1000',
    suffix: '+',
    description: 'Successful contract deployments',
    icon: '🚀',
  },
  {
    id: 'developers',
    name: 'Developers',
    value: '500',
    suffix: '+',
    description: 'Building with our platform',
    icon: '👥',
  },
  {
    id: 'uptime',
    name: 'Uptime',
    value: '99.9',
    suffix: '%',
    description: 'Service availability',
    icon: '⚡',
  },
]

function AnimatedNumber({ value, suffix = '', duration = 2000 }: { value: string; suffix?: string; duration?: number }) {
  const [displayValue, setDisplayValue] = useState(0)
  const [isVisible, setIsVisible] = useState(false)
  const ref = useRef<HTMLSpanElement>(null)
  const finalValue = parseInt(value.replace(/,/g, ''))

  useEffect(() => {
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && !isVisible) {
          setIsVisible(true)
          observer.disconnect()
        }
      },
      { threshold: 0.3 }
    )

    if (ref.current) {
      observer.observe(ref.current)
    }

    return () => observer.disconnect()
  }, [isVisible])

  useEffect(() => {
    if (isVisible) {
      let startTime: number
      let startValue = 0

      const animate = (timestamp: number) => {
        if (!startTime) startTime = timestamp
        const progress = timestamp - startTime
        const percentage = Math.min(progress / duration, 1)
        
        // Easing function for smooth animation
        const easeOut = 1 - Math.pow(1 - percentage, 3)
        const currentValue = Math.floor(startValue + (finalValue - startValue) * easeOut)
        
        setDisplayValue(currentValue)

        if (percentage < 1) {
          requestAnimationFrame(animate)
        }
      }

      requestAnimationFrame(animate)
    }
  }, [isVisible, finalValue, duration])

  return (
    <span ref={ref}>
      {displayValue.toLocaleString()}{suffix}
    </span>
  )
}

export function StatsSection() {
  return (
    <section className="relative py-16 sm:py-24">
      {/* Background elements */}
      <div className="absolute inset-0 overflow-hidden">
        <div className="absolute top-0 left-1/4 w-72 h-72 bg-neo-500/5 rounded-full blur-3xl" />
        <div className="absolute bottom-0 right-1/4 w-72 h-72 bg-services-business/5 rounded-full blur-3xl" />
      </div>

      <div className="relative mx-auto max-w-7xl px-6 lg:px-8">
        <AnimatedDiv direction="up" duration={600} className="text-center mb-16">
          <h2 className="text-3xl font-bold tracking-tight text-white sm:text-4xl">
            Trusted by the <span className="text-gradient">Neo Community</span>
          </h2>
          <p className="mt-4 text-lg text-gray-400">
            Join thousands of developers building the future of decentralized applications
          </p>
        </AnimatedDiv>

        <div className="grid grid-cols-2 gap-8 md:grid-cols-4">
          {stats.map((stat, index) => (
            <AnimatedDiv
              key={stat.id}
              direction="up"
              duration={600}
              delay={index * 100}
              className="text-center group"
            >
              <div className="relative">
                {/* Background glow */}
                <div className="absolute inset-0 bg-gradient-to-br from-neo-500/10 to-services-business/10 rounded-lg blur-xl group-hover:blur-lg transition-all duration-300" />
                
                {/* Content */}
                <div className="relative glass rounded-lg p-6 hover:border-neo-500/30 transition-all duration-300">
                  <div className="text-3xl mb-2">{stat.icon}</div>
                  <div className="text-3xl sm:text-4xl font-bold text-white mb-2">
                    <AnimatedNumber value={stat.value} suffix={stat.suffix} />
                  </div>
                  <div className="text-sm font-semibold text-neo-500 mb-1">
                    {stat.name}
                  </div>
                  <div className="text-xs text-gray-400">
                    {stat.description}
                  </div>
                </div>
              </div>
            </AnimatedDiv>
          ))}
        </div>

        {/* Additional metrics */}
        <AnimatedDiv direction="up" duration={600} delay={400} className="mt-16 text-center">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8 max-w-4xl mx-auto">
            <div className="flex items-center justify-center space-x-2">
              <div className="w-3 h-3 bg-green-500 rounded-full animate-pulse" />
              <span className="text-gray-400">100% Open Source</span>
            </div>
            <div className="flex items-center justify-center space-x-2">
              <div className="w-3 h-3 bg-blue-500 rounded-full animate-pulse" />
              <span className="text-gray-400">Enterprise Grade Security</span>
            </div>
            <div className="flex items-center justify-center space-x-2">
              <div className="w-3 h-3 bg-purple-500 rounded-full animate-pulse" />
              <span className="text-gray-400">24/7 Support</span>
            </div>
          </div>
        </AnimatedDiv>
      </div>
    </section>
  )
}