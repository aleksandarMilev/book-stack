import { identityApi } from '@/features/auth/api/identity.api';
import { profileApi } from '@/features/auth/api/profile.api';
import { useAuthStore } from '@/store/auth.store';

const enrichSessionFromProfile = async (): Promise<void> => {
  const currentSession = useAuthStore.getState().session;
  if (!currentSession) {
    return;
  }

  try {
    const profile = await profileApi.getMine();
    useAuthStore.getState().setSession({
      ...currentSession,
      user: {
        ...currentSession.user,
        displayName: `${profile.firstName} ${profile.lastName}`.trim(),
      },
    });
  } catch {
    // profile enrichment is optional and should not block authentication flow
  }
};

export const authService = {
  async login(credentials: string, password: string, rememberMe: boolean): Promise<void> {
    const response = await identityApi.login({ credentials, password, rememberMe });
    const didStoreSession = useAuthStore.getState().setSessionFromToken(response.token);

    if (!didStoreSession) {
      throw new Error('Invalid token received from server.');
    }

    await enrichSessionFromProfile();
  },

  async register(payload: {
    username: string;
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    image?: File | null;
  }): Promise<void> {
    const response = await identityApi.register(payload);
    const didStoreSession = useAuthStore.getState().setSessionFromToken(response.token);

    if (!didStoreSession) {
      throw new Error('Invalid token received from server.');
    }

    await enrichSessionFromProfile();
  },

  logout(): void {
    useAuthStore.getState().clearSession();
  },
};
