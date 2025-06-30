'use client'

import { useState, useRef, useEffect } from 'react'

interface DropdownItem {
  label: string
  href?: string
  onClick?: () => void
  icon?: React.ComponentType<{ className?: string }>
}

interface SimpleDropdownProps {
  trigger: React.ReactNode
  items: DropdownItem[]
  align?: 'left' | 'right'
}

export function SimpleDropdown({ trigger, items, align = 'right' }: SimpleDropdownProps) {
  const [isOpen, setIsOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const handleItemClick = (item: DropdownItem) => {
    if (item.onClick) {
      item.onClick()
    }
    setIsOpen(false)
  }

  return (
    <div className="relative" ref={dropdownRef}>
      <div onClick={() => setIsOpen(!isOpen)}>
        {trigger}
      </div>
      
      {isOpen && (
        <div 
          className={`absolute z-50 mt-2 w-56 rounded-md shadow-lg bg-gray-800 ring-1 ring-gray-600 ring-opacity-50 ${
            align === 'right' ? 'right-0' : 'left-0'
          }`}
        >
          <div className="py-1">
            {items.map((item, index) => (
              <div key={index}>
                {item.href ? (
                  <a
                    href={item.href}
                    className="flex items-center px-4 py-2 text-sm text-gray-300 hover:bg-gray-700 hover:text-white transition-colors"
                    onClick={() => setIsOpen(false)}
                  >
                    {item.icon && <item.icon className="mr-3 h-5 w-5" />}
                    {item.label}
                  </a>
                ) : (
                  <button
                    onClick={() => handleItemClick(item)}
                    className="w-full text-left flex items-center px-4 py-2 text-sm text-gray-300 hover:bg-gray-700 hover:text-white transition-colors"
                  >
                    {item.icon && <item.icon className="mr-3 h-5 w-5" />}
                    {item.label}
                  </button>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}