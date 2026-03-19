import { render } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { usePaginationScrollReset } from '@/hooks/usePaginationScrollReset';

interface TestComponentProps {
  attachRef?: boolean;
  pageIndex: number;
}

function TestComponent({ attachRef = true, pageIndex }: TestComponentProps) {
  const sectionRef = usePaginationScrollReset<HTMLDivElement>(pageIndex);

  if (!attachRef) {
    return <div data-testid="results-section" />;
  }

  return <div data-testid="results-section" ref={sectionRef} />;
}

describe('usePaginationScrollReset', () => {
  afterEach(() => {
    vi.clearAllMocks();
    vi.unstubAllGlobals();
  });

  it('scrolls the section into view when page index changes', () => {
    const scrollIntoViewDescriptor = Object.getOwnPropertyDescriptor(HTMLElement.prototype, 'scrollIntoView');
    const scrollIntoViewMock = vi.fn();
    Object.defineProperty(HTMLElement.prototype, 'scrollIntoView', {
      configurable: true,
      writable: true,
      value: scrollIntoViewMock,
    });

    try {
      const { rerender } = render(<TestComponent pageIndex={1} />);
      expect(scrollIntoViewMock).not.toHaveBeenCalled();

      rerender(<TestComponent pageIndex={2} />);
      expect(scrollIntoViewMock).toHaveBeenCalledWith({ behavior: 'smooth', block: 'start' });
    } finally {
      if (scrollIntoViewDescriptor) {
        Object.defineProperty(HTMLElement.prototype, 'scrollIntoView', scrollIntoViewDescriptor);
      } else {
        delete (HTMLElement.prototype as unknown as { scrollIntoView?: unknown }).scrollIntoView;
      }
    }
  });

  it('falls back to window scroll when no section ref is attached', () => {
    const scrollToMock = vi.fn();
    vi.stubGlobal('scrollTo', scrollToMock);

    const { rerender } = render(<TestComponent attachRef={false} pageIndex={1} />);
    rerender(<TestComponent attachRef={false} pageIndex={2} />);

    expect(scrollToMock).toHaveBeenCalledWith({ top: 0, left: 0, behavior: 'smooth' });
  });
});
