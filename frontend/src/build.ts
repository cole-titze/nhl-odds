// This build's version (vite.config.ts): the commit's date and short SHA, e.g. 2026.11.03-abc1234.
declare const __BUILD_VERSION__: string;
declare const __BUILD_SHA__: string;

export const BUILD_VERSION = __BUILD_VERSION__;
/** The full commit SHA, or empty for a build outside git */
export const BUILD_SHA = __BUILD_SHA__;
