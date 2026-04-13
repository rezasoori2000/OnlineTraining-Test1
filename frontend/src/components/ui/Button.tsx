import { ButtonHTMLAttributes } from 'react'
import { cn } from '@/utils/helpers'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger'
}

export function Button({ variant = 'primary', className, children, ...props }: ButtonProps) {
  const variantClasses = {
    primary: 'bg-brand-500 text-white hover:bg-brand-600 focus:ring-brand-100',
    secondary: 'bg-white text-gray-700 border border-gray-300 hover:bg-gray-50 focus:ring-brand-100',
    danger: 'bg-red-600 text-white hover:bg-red-700 focus:ring-red-100',
  }

  return (
    <button
      className={cn(
        'inline-flex items-center px-4 py-2.5 rounded-lg text-theme-sm font-medium transition-colors focus:outline-none focus:ring-2 disabled:opacity-50 disabled:cursor-not-allowed',
        variantClasses[variant],
        className,
      )}
      {...props}
    >
      {children}
    </button>
  )
}
