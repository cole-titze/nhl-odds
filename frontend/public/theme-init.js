// Runs before first paint to avoid a light-mode flash (and a white iOS status
// bar) for dark-mode users. Kept as an external file so it satisfies the
// `script-src 'self'` CSP. Mirrors the logic in src/hooks/useTheme.ts.
(function () {
  var stored = localStorage.getItem('theme');
  var theme = stored || (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
  if (theme === 'dark') document.documentElement.classList.add('dark');
  var meta = document.querySelector('meta[name="theme-color"]');
  if (meta) meta.setAttribute('content', theme === 'dark' ? '#141418' : '#fafafa');
})();
