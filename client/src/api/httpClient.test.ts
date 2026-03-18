import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const { axiosCreateMock, clearSessionMock } = vi.hoisted(() => ({
  axiosCreateMock: vi.fn(),
  clearSessionMock: vi.fn(),
}));

vi.mock('axios', () => ({
  default: {
    create: axiosCreateMock,
    isAxiosError: vi.fn(() => false),
    AxiosHeaders: {
      from: (headers: unknown) => headers,
    },
  },
}));

vi.mock('@/store/auth.store', () => ({
  useAuthStore: {
    getState: () => ({
      session: null,
      clearSession: clearSessionMock,
    }),
  },
}));

vi.mock('@/features/auth/utils/jwtSession', () => ({
  isExpirationIsoExpired: vi.fn(() => false),
}));

const createHttpClientStub = () => ({
  interceptors: {
    request: { use: vi.fn() },
    response: { use: vi.fn() },
  },
});

describe('httpClient base URL configuration', () => {
  beforeEach(() => {
    vi.resetModules();
    axiosCreateMock.mockReset();
    axiosCreateMock.mockReturnValue(createHttpClientStub());
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.restoreAllMocks();
  });

  it('uses trimmed VITE_REACT_APP_SERVER_URL as axios baseURL', async () => {
    vi.stubEnv('VITE_REACT_APP_SERVER_URL', 'http://127.0.0.1:8080///');

    await import('@/api/httpClient');

    expect(axiosCreateMock).toHaveBeenCalledWith(
      expect.objectContaining({
        baseURL: 'http://127.0.0.1:8080',
      }),
    );
  });

  it('warns when localhost is used as API host in dev mode', async () => {
    vi.stubEnv('VITE_REACT_APP_SERVER_URL', 'http://localhost:8080');
    const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => undefined);

    await import('@/api/httpClient');

    expect(warnSpy).toHaveBeenCalledWith(expect.stringContaining('localhost'));
    expect(warnSpy).toHaveBeenCalledWith(expect.stringContaining('127.0.0.1:8080'));
  });
});
