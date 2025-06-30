'use client'

import { useState } from 'react'
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter'
import { oneDark } from 'react-syntax-highlighter/dist/cjs/styles/prism'
import {
  ClipboardIcon,
  CheckIcon,
  DocumentDuplicateIcon,
} from '@heroicons/react/24/outline'
import { classNames } from '@/utils/classNames'

interface CodeBlockProps {
  code: string
  language: string
  filename?: string
  showLineNumbers?: boolean
  className?: string
  maxHeight?: string
}

export function CodeBlock({
  code,
  language,
  filename,
  showLineNumbers = false,
  className,
  maxHeight = '400px',
}: CodeBlockProps) {
  const [copied, setCopied] = useState(false)

  const copyToClipboard = async () => {
    try {
      await navigator.clipboard.writeText(code)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch (err) {
      // Failed to copy text - silently handle for production
      // Could implement fallback copy mechanism here if needed
    }
  }

  return (
    <div className={classNames(
      'relative rounded-lg border border-gray-700 bg-gray-900 overflow-hidden',
      className
    )}>
      {/* Header */}
      {filename && (
        <div className="flex items-center justify-between px-4 py-2 bg-gray-800 border-b border-gray-700">
          <div className="flex items-center space-x-2">
            <div className="flex space-x-1">
              <div className="w-3 h-3 rounded-full bg-red-500" />
              <div className="w-3 h-3 rounded-full bg-yellow-500" />
              <div className="w-3 h-3 rounded-full bg-green-500" />
            </div>
            <span className="text-sm text-gray-400 font-mono">{filename}</span>
          </div>
          <div className="flex items-center space-x-2">
            <span className="text-xs text-gray-500 uppercase">{language}</span>
          </div>
        </div>
      )}

      {/* Code content */}
      <div className="relative">
        <SyntaxHighlighter
          language={language}
          style={oneDark}
          showLineNumbers={showLineNumbers}
          customStyle={{
            margin: 0,
            padding: '1rem',
            fontSize: '0.875rem',
            lineHeight: '1.5',
            maxHeight,
            overflow: 'auto',
            background: 'transparent',
          }}
          lineNumberStyle={{
            color: '#6b7280',
            fontSize: '0.75rem',
            paddingRight: '1rem',
            minWidth: '2rem',
          }}
          wrapLines={true}
          wrapLongLines={true}
        >
          {code}
        </SyntaxHighlighter>

        {/* Copy button */}
        <button
          onClick={copyToClipboard}
          className="absolute top-2 right-2 p-2 rounded-md bg-gray-800 hover:bg-gray-700 text-gray-400 hover:text-white transition-colors border border-gray-600"
          title="Copy to clipboard"
        >
          {copied ? (
            <CheckIcon className="h-4 w-4 text-green-500" />
          ) : (
            <ClipboardIcon className="h-4 w-4" />
          )}
        </button>

        {/* Copy feedback */}
        {copied && (
          <div className="absolute top-12 right-2 bg-green-500 text-white text-xs px-2 py-1 rounded shadow-lg">
            Copied!
          </div>
        )}
      </div>
    </div>
  )
}

// Alternative simpler code block for inline use
export function InlineCode({ children, className }: { children: React.ReactNode; className?: string }) {
  return (
    <code className={classNames(
      'px-2 py-1 text-sm font-mono bg-gray-800 text-neo-500 rounded border border-gray-700',
      className
    )}>
      {children}
    </code>
  )
}

// Code block with tabs for multiple examples
interface CodeTabsProps {
  tabs: Array<{
    label: string
    language: string
    code: string
    filename?: string
  }>
  defaultTab?: number
}

export function CodeTabs({ tabs, defaultTab = 0 }: CodeTabsProps) {
  const [activeTab, setActiveTab] = useState(defaultTab)

  return (
    <div className="w-full">
      {/* Tab headers */}
      <div className="flex space-x-1 bg-gray-800 rounded-t-lg p-1">
        {tabs.map((tab, index) => (
          <button
            key={index}
            onClick={() => setActiveTab(index)}
            className={classNames(
              'px-3 py-2 text-sm font-medium rounded-md transition-colors',
              activeTab === index
                ? 'bg-gray-700 text-white'
                : 'text-gray-400 hover:text-white hover:bg-gray-700'
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Tab content */}
      <div className="border-t-0 rounded-t-none">
        <CodeBlock
          code={tabs[activeTab].code}
          language={tabs[activeTab].language}
          filename={tabs[activeTab].filename}
          showLineNumbers={true}
        />
      </div>
    </div>
  )
}