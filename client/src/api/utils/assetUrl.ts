const getServerBaseUrl = (): string => {
  const serverBaseUrl = import.meta.env.VITE_REACT_APP_SERVER_URL ?? '';
  if (!serverBaseUrl) {
    return '';
  }

  return serverBaseUrl.endsWith('/') ? serverBaseUrl.slice(0, -1) : serverBaseUrl;
};

export const resolveAssetUrl = (assetPath: string | null | undefined): string => {
  if (!assetPath) {
    return '';
  }

  if (assetPath.startsWith('http://') || assetPath.startsWith('https://')) {
    return assetPath;
  }

  const serverBaseUrl = getServerBaseUrl();
  if (!serverBaseUrl) {
    return assetPath;
  }

  const normalizedAssetPath = assetPath.startsWith('/') ? assetPath : `/${assetPath}`;
  return `${serverBaseUrl}${normalizedAssetPath}`;
};
