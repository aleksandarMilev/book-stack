import { useEffect, useRef } from 'react';

type PaginationScrollBehavior = 'auto' | 'smooth';

interface UsePaginationScrollResetOptions {
  behavior?: PaginationScrollBehavior;
}

const resolveScrollBehavior = (behavior: PaginationScrollBehavior): PaginationScrollBehavior => {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
    return behavior;
  }

  return window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : behavior;
};

export function usePaginationScrollReset<T extends HTMLElement>(
  pageIndex: number,
  options: UsePaginationScrollResetOptions = {},
) {
  const { behavior = 'smooth' } = options;
  const sectionRef = useRef<T | null>(null);
  const hasMountedRef = useRef(false);

  useEffect(() => {
    if (!hasMountedRef.current) {
      hasMountedRef.current = true;
      return;
    }

    const scrollBehavior = resolveScrollBehavior(behavior);
    const sectionElement = sectionRef.current;

    if (sectionElement && typeof sectionElement.scrollIntoView === 'function') {
      sectionElement.scrollIntoView({ behavior: scrollBehavior, block: 'start' });
      return;
    }

    if (typeof window.scrollTo === 'function') {
      window.scrollTo({ top: 0, left: 0, behavior: scrollBehavior });
    }
  }, [behavior, pageIndex]);

  return sectionRef;
}
