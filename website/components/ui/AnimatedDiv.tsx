'use client'

import { useEffect, useRef, useState } from 'react'

interface AnimatedDivProps {
  children: React.ReactNode
  className?: string
  delay?: number
  duration?: number
  direction?: 'up' | 'down' | 'left' | 'right' | 'fade'
}

export function AnimatedDiv({ 
  children, 
  className = '', 
  delay = 0, 
  duration = 600,
  direction = 'up'
}: AnimatedDivProps) {
  const [isVisible, setIsVisible] = useState(false)
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setTimeout(() => setIsVisible(true), delay)
          observer.disconnect()
        }
      },
      { threshold: 0.1 }
    )

    if (ref.current) {
      observer.observe(ref.current)
    }

    return () => observer.disconnect()
  }, [delay])

  const getInitialStyles = () => {
    switch (direction) {
      case 'up':
        return { transform: 'translateY(20px)', opacity: 0 }
      case 'down':
        return { transform: 'translateY(-20px)', opacity: 0 }
      case 'left':
        return { transform: 'translateX(20px)', opacity: 0 }
      case 'right':
        return { transform: 'translateX(-20px)', opacity: 0 }
      case 'fade':
        return { opacity: 0 }
      default:
        return { transform: 'translateY(20px)', opacity: 0 }
    }
  }

  const getVisibleStyles = () => {
    return { transform: 'translateY(0) translateX(0)', opacity: 1 }
  }

  return (
    <div
      ref={ref}
      className={className}
      style={{
        ...(isVisible ? getVisibleStyles() : getInitialStyles()),
        transition: `all ${duration}ms ease-out`,
      }}
    >
      {children}
    </div>
  )
}