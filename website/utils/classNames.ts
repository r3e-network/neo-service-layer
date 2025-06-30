/**
 * Utility function for conditionally joining classNames together.
 * This is a simple alternative to the popular `clsx` library.
 */
export function classNames(...classes: (string | undefined | null | false)[]): string {
  return classes.filter(Boolean).join(' ')
}