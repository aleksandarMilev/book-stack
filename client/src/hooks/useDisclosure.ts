import { useCallback, useState } from 'react';

interface UseDisclosureResult {
  isOpen: boolean;
  open: () => void;
  close: () => void;
  toggle: () => void;
}

export function useDisclosure(initialValue = false): UseDisclosureResult {
  const [isOpen, setIsOpen] = useState(initialValue);

  const open = useCallback(() => {
    setIsOpen(true);
  }, []);

  const close = useCallback(() => {
    setIsOpen(false);
  }, []);

  const toggle = useCallback(() => {
    setIsOpen((previousState) => !previousState);
  }, []);

  return { isOpen, open, close, toggle };
}
