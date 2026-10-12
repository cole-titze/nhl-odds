import type { Config } from '@react-router/dev/config';

export default {
  appDirectory: 'src',
  // Pages render on the server (server.mjs), so crawlers and link previews get real content
  ssr: true,
} satisfies Config;
