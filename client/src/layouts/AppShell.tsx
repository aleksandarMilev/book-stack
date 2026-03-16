import { Outlet } from 'react-router-dom';

import { AppFooter } from '@/components/navigation/AppFooter';
import { AppHeader } from '@/components/navigation/AppHeader';

export function AppShell() {
  return (
    <div className="app-shell">
      <AppHeader />
      <main className="app-main" id="main-content">
        <Outlet />
      </main>
      <AppFooter />
    </div>
  );
}
