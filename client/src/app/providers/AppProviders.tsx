import type { PropsWithChildren } from 'react';
import { useEffect } from 'react';

import { useSellerProfileStore } from '@/features/sellerProfiles/store/sellerProfile.store';
import { useAuthStore } from '@/store/auth.store';

function SellerProfileSync() {
  const session = useAuthStore((state) => state.session);
  const loadMine = useSellerProfileStore((state) => state.loadMine);
  const clear = useSellerProfileStore((state) => state.clear);

  useEffect(() => {
    if (!session) {
      clear();
      return;
    }

    void loadMine(true);
  }, [clear, loadMine, session]);

  return null;
}

export function AppProviders({ children }: PropsWithChildren) {
  return (
    <>
      <SellerProfileSync />
      {children}
    </>
  );
}
