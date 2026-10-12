import { createContext, useContext } from 'react';

// Per-request CSP nonce for React Router's inline scripts; set by entry.server.tsx, unset in the browser
export const NonceContext = createContext<string | undefined>(undefined);

export const useNonce = () => useContext(NonceContext);
