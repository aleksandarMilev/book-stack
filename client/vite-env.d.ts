/// <reference types="vite/client" />
interface ImportMetaEnv {
  readonly VITE_REACT_APP_SERVER_URL: string;
  readonly VITE_REACT_APP_PAYMENT_PROVIDER?: string;
  readonly VITE_REACT_APP_ENABLE_MOCK_PAYMENT_UI?: string;
}
interface ImportMeta {
  readonly env: ImportMetaEnv;
}
