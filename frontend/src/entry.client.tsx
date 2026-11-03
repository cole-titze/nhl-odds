import { StrictMode, startTransition } from 'react';
import { hydrateRoot } from 'react-dom/client';
import { HydratedRouter } from 'react-router/dom';
import { installErrorReporting } from './utils/errorReporting';

// Before hydrating, so errors while the page starts up are reported too
installErrorReporting();

if ('serviceWorker' in navigator) {
  navigator.serviceWorker.register('/sw.js');
}

startTransition(() => {
  hydrateRoot(
    document,
    <StrictMode>
      <HydratedRouter />
    </StrictMode>,
  );
});
