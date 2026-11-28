import { StrictMode, startTransition } from 'react';
import { hydrateRoot } from 'react-dom/client';
import { HydratedRouter } from 'react-router/dom';
import { installErrorReporting } from './utils/errorReporting';

// Before hydrating, so errors while the page starts up are reported too
installErrorReporting();

if ('serviceWorker' in navigator) {
  // Crawlers (AdsBot-Google) stub register() to reject; the site works without the worker, so
  // don't report that as an unhandled rejection
  navigator.serviceWorker.register('/sw.js').catch(() => {});
}

startTransition(() => {
  hydrateRoot(
    document,
    <StrictMode>
      <HydratedRouter />
    </StrictMode>,
  );
});
