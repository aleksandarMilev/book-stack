const GUEST_PAYMENT_TOKENS_STORAGE_KEY = 'bookstack.guestPaymentTokens';
const PENDING_ORDER_ID_STORAGE_KEY = 'bookstack.payment.pendingOrderId';

interface GuestPaymentTokenMap {
  [orderId: string]: string;
}

const isBrowser = (): boolean => typeof window !== 'undefined';

const readGuestTokenMap = (): GuestPaymentTokenMap => {
  if (!isBrowser()) {
    return {};
  }

  const rawValue = window.sessionStorage.getItem(GUEST_PAYMENT_TOKENS_STORAGE_KEY);
  if (!rawValue) {
    return {};
  }

  try {
    const parsedValue = JSON.parse(rawValue);
    if (!parsedValue || typeof parsedValue !== 'object') {
      return {};
    }

    return Object.entries(parsedValue as Record<string, unknown>).reduce<GuestPaymentTokenMap>(
      (nextMap, [orderId, token]) => {
        if (typeof token === 'string' && token.length > 0) {
          nextMap[orderId] = token;
        }

        return nextMap;
      },
      {},
    );
  } catch {
    return {};
  }
};

const writeGuestTokenMap = (map: GuestPaymentTokenMap): void => {
  if (!isBrowser()) {
    return;
  }

  if (Object.keys(map).length === 0) {
    window.sessionStorage.removeItem(GUEST_PAYMENT_TOKENS_STORAGE_KEY);
    return;
  }

  window.sessionStorage.setItem(GUEST_PAYMENT_TOKENS_STORAGE_KEY, JSON.stringify(map));
};

export const paymentSessionStorage = {
  setGuestPaymentToken(orderId: string, token: string): void {
    const currentMap = readGuestTokenMap();
    currentMap[orderId] = token;
    writeGuestTokenMap(currentMap);
  },

  getGuestPaymentToken(orderId: string): string | null {
    const currentMap = readGuestTokenMap();
    return currentMap[orderId] ?? null;
  },

  clearGuestPaymentToken(orderId: string): void {
    const currentMap = readGuestTokenMap();
    delete currentMap[orderId];
    writeGuestTokenMap(currentMap);
  },

  setPendingOrderId(orderId: string): void {
    if (!isBrowser()) {
      return;
    }

    window.sessionStorage.setItem(PENDING_ORDER_ID_STORAGE_KEY, orderId);
  },

  getPendingOrderId(): string | null {
    if (!isBrowser()) {
      return null;
    }

    return window.sessionStorage.getItem(PENDING_ORDER_ID_STORAGE_KEY);
  },

  clearPendingOrderId(): void {
    if (!isBrowser()) {
      return;
    }

    window.sessionStorage.removeItem(PENDING_ORDER_ID_STORAGE_KEY);
  },
};
