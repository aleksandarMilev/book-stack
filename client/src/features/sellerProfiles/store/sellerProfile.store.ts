import { create } from 'zustand';

import {
  type SellerProfileResponse,
  sellerProfilesApi,
} from '@/features/sellerProfiles/api/sellerProfiles.api';
import { useAuthStore } from '@/store/auth.store';

type SellerProfileLoadState = 'idle' | 'loading' | 'ready' | 'error';

interface SellerProfileState {
  profile: SellerProfileResponse | null;
  loadState: SellerProfileLoadState;
  loadedForUserId: string | null;
  loadMine: (force?: boolean) => Promise<SellerProfileResponse | null>;
  setProfile: (profile: SellerProfileResponse | null) => void;
  clear: () => void;
}

const shouldSkipLoad = (
  state: SellerProfileState,
  currentUserId: string,
  force: boolean,
): boolean => {
  if (force) {
    return false;
  }

  return state.loadedForUserId === currentUserId && state.loadState === 'ready';
};

export const useSellerProfileStore = create<SellerProfileState>((set, get) => ({
  profile: null,
  loadState: 'idle',
  loadedForUserId: null,
  async loadMine(force = false): Promise<SellerProfileResponse | null> {
    const session = useAuthStore.getState().session;
    const currentUserId = session?.user.id ?? null;

    if (!currentUserId) {
      set({
        profile: null,
        loadState: 'idle',
        loadedForUserId: null,
      });

      return null;
    }

    const currentState = get();
    if (shouldSkipLoad(currentState, currentUserId, force)) {
      return currentState.profile;
    }

    set({
      loadState: 'loading',
      loadedForUserId: currentUserId,
    });

    try {
      const profile = await sellerProfilesApi.getMine();
      set({
        profile,
        loadState: 'ready',
        loadedForUserId: currentUserId,
      });

      return profile;
    } catch {
      set({
        profile: null,
        loadState: 'error',
        loadedForUserId: currentUserId,
      });

      return null;
    }
  },
  setProfile(profile): void {
    const currentUserId = useAuthStore.getState().session?.user.id ?? null;

    set({
      profile,
      loadState: 'ready',
      loadedForUserId: currentUserId,
    });
  },
  clear(): void {
    set({
      profile: null,
      loadState: 'idle',
      loadedForUserId: null,
    });
  },
}));
