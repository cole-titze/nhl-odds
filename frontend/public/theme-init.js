// Runs before first paint to avoid a light-mode flash (and a white iOS status
// bar) for dark-mode users. Kept as an external file so it satisfies the
// `script-src 'self'` CSP. Applies the snapshot src/hooks/useTheme.ts saves,
// so the theme list only lives in src/themes.ts.
(function () {
  var root = document.documentElement;
  var meta = document.querySelector('meta[name="theme-color"]');
  var state = null;
  try {
    state = JSON.parse(localStorage.getItem('themeState'));
  } catch (e) {}

  if (state && state.classes) {
    root.classList.add.apply(root.classList, state.classes);
    for (var name in state.vars || {}) root.style.setProperty(name, state.vars[name]);
    if (meta) meta.setAttribute('content', state.metaColor);
    return;
  }

  // First visit, or saved by a version before themeState existed
  var stored = localStorage.getItem('theme');
  var dark = stored ? stored === 'dark' : matchMedia('(prefers-color-scheme: dark)').matches;
  root.classList.add(dark ? 'theme-dark' : 'theme-light');
  if (dark) root.classList.add('dark');
  if (meta) meta.setAttribute('content', dark ? '#141418' : '#fafafa');
})();
