import { RouterProvider } from 'react-router-dom';

import { AppProviders } from '@/app/providers/AppProviders';
import { appRouter } from '@/routes/router';

export function App() {
  return (
    <AppProviders>
      <RouterProvider router={appRouter} />
    </AppProviders>
  );
}
