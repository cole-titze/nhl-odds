import { useEffect, useId, useRef, useState } from 'react';
import { Link, useLocation } from 'react-router';
import { ApiError, apiFetch } from '../api/client';
import { useHydrated } from '../hooks/useHydrated';
import { getAdminKey } from '../hooks/useAdminKey';

// Experimental chat assistant backed by a local Ollama model (AssistantController in the web
// API). It only appears when the API says it's enabled, and only after hydration, so server
// rendered (and cached) pages never include it.

interface Message {
  role: 'user' | 'assistant';
  content: string;
}

// Models answer in Markdown despite being asked not to; show **bold** and drop other markers
function renderReply(text: string) {
  return text
    .replace(/^#+\s*/gm, '')
    .split(/\*\*(.+?)\*\*/g)
    .map((part, i) => (i % 2 ? <strong key={i}>{part}</strong> : part));
}

// Sent by /api/Assistant/Status (AssistantLimits in the web API)
interface Limits {
  burst: number;
  minutesPerQuestion: number;
  // Sent the admin key: no per-visitor limit
  unlimited: boolean;
}

// The admin key (stored by the Admin page) lifts the assistant's per-visitor limit
function adminHeaders(): Record<string, string> {
  const key = getAdminKey();
  return key ? { 'X-Admin-Key': key } : {};
}

function errorMessage(err: unknown, limits: Limits) {
  if (err instanceof ApiError && err.status === 429)
    return `That's the limit for now — you get one more question every ${limits.minutesPerQuestion} minutes.`;
  if (err instanceof ApiError && err.status === 503)
    return 'Someone else is asking a question right now. Try again in a minute.';
  return "Sorry, the assistant couldn't answer. It runs on a home computer that may be off.";
}

// Phones get a full-screen chat whose contents stay in the visual viewport, so the on-screen
// keyboard can't cover them (iOS doesn't shrink the layout for the keyboard). Matches Tailwind's max-sm.
const PHONE = '(max-width: 639.98px)';

const BOT_BUBBLE =
  'glass w-fit whitespace-pre-wrap break-words rounded-2xl rounded-bl-md px-3.5 py-2 text-surface-900 shadow-sm dark:text-surface-50';

// The assistant's mascot: a hockey puck with eyes that blink (puck-* classes in index.css)
function PuckBot({ className = '' }: { className?: string }) {
  return (
    <svg viewBox="0 0 32 32" aria-hidden="true" className={className}>
      <path d="M3 13v6c0 3.3 5.8 6 13 6s13-2.7 13-6v-6Z" fill="#0b0f19" />
      <ellipse
        cx="16"
        cy="13"
        rx="13"
        ry="6"
        fill="#2a3242"
        stroke="#ffffff"
        strokeOpacity="0.35"
      />
      <g className="puck-eyes" fill="#ffffff">
        <ellipse cx="11.5" cy="13" rx="1.6" ry="2.1" />
        <ellipse cx="20.5" cy="13" rx="1.6" ry="2.1" />
      </g>
      <path
        d="M13.5 16.5q2.5 1.6 5 0"
        fill="none"
        stroke="#ffffff"
        strokeWidth="1.2"
        strokeLinecap="round"
      />
    </svg>
  );
}

// The mascot beside its messages, on a light disc so the dark puck shows in dark mode too
function Avatar({ hop = false }: { hop?: boolean }) {
  return (
    <span className="inline-flex shrink-0 rounded-full bg-white p-0.5 ring-1 ring-surface-200 dark:ring-white/10">
      <PuckBot className={`h-7 w-7 ${hop ? 'puck-throbber' : ''}`} />
    </span>
  );
}

export function Assistant() {
  const hydrated = useHydrated();
  const [limits, setLimits] = useState<Limits | null>(null);
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState<Message[]>([]);
  const [draft, setDraft] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { pathname } = useLocation();
  const titleId = useId();
  const launcherRef = useRef<HTMLButtonElement>(null);
  const inputRef = useRef<HTMLTextAreaElement>(null);
  const logRef = useRef<HTMLDivElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);
  const wasOpen = useRef(false);

  useEffect(() => {
    if (!hydrated) return;
    apiFetch<Partial<Limits> & { enabled?: boolean }>('/api/Assistant/Status', {
      headers: adminHeaders(),
    })
      .then((l) =>
        setLimits(
          l.enabled === false
            ? null
            : {
                burst: l.burst ?? 3,
                minutesPerQuestion: l.minutesPerQuestion ?? 2,
                unlimited: l.unlimited ?? false,
              },
        ),
      )
      .catch(() => setLimits(null));
  }, [hydrated]);

  useEffect(() => {
    if (open) inputRef.current?.focus();
    // After the re-render, since the launcher is hidden behind the full-screen chat on phones
    else if (wasOpen.current) launcherRef.current?.focus();
    wasOpen.current = open;
  }, [open]);

  // On phones, keep the chat's contents in the part of the screen the keyboard leaves
  useEffect(() => {
    const vv = window.visualViewport;
    if (!open || !vv || !window.matchMedia(PHONE).matches) return;
    const fit = () => {
      // The card covers the whole screen; padding keeps its contents in the part above the keyboard
      const below = window.innerHeight - vv.offsetTop - vv.height;
      panelRef.current?.style.setProperty('--vv-top', `${vv.offsetTop}px`);
      panelRef.current?.style.setProperty('--vv-bottom', `${Math.max(0, below)}px`);
    };
    fit();
    vv.addEventListener('resize', fit);
    vv.addEventListener('scroll', fit);
    // A swipe with nothing to scroll goes to the page, and iOS shifts the whole screen (the
    // header jitters). Only the message list and a long message box may scroll, and only once
    // they overflow.
    const panel = panelRef.current;
    const onTouchMove = (e: TouchEvent) => {
      const scroller = (e.target as Element).closest<HTMLElement>('[data-scroll]');
      if (!scroller || scroller.scrollHeight <= scroller.clientHeight) e.preventDefault();
    };
    panel?.addEventListener('touchmove', onTouchMove, { passive: false });
    // Pin the page where it is: iOS ignores overflow: hidden and scrolls the page behind the
    // chat as the keyboard comes and goes, which made it jump
    const body = document.body;
    const saved = body.style.cssText;
    const { scrollX, scrollY } = window;
    Object.assign(body.style, { position: 'fixed', top: `${-scrollY}px`, left: '0', right: '0' });
    return () => {
      vv.removeEventListener('resize', fit);
      vv.removeEventListener('scroll', fit);
      panel?.removeEventListener('touchmove', onTouchMove);
      body.style.cssText = saved;
      // Back where it was. iOS can also leave the viewport shifted once the keyboard closes,
      // floating the fixed footer mid-screen, so do it again as the keyboard animates away.
      const restore = () => window.scrollTo({ left: scrollX, top: scrollY, behavior: 'instant' });
      restore();
      vv.addEventListener('resize', restore);
      setTimeout(() => vv.removeEventListener('resize', restore), 1000);
    };
  }, [open]);

  useEffect(() => {
    logRef.current?.scrollTo({ top: logRef.current.scrollHeight });
  }, [messages, busy]);

  // One line tall, growing with what's typed (up to max-h-36, then it scrolls)
  useEffect(() => {
    const input = inputRef.current;
    if (!input) return;
    input.style.height = 'auto';
    input.style.height = `${input.scrollHeight + input.offsetHeight - input.clientHeight}px`;
  }, [draft, open]);

  if (!limits) return null;

  const close = () => setOpen(false);

  const send = async () => {
    const content = draft.trim();
    if (!content || busy) return;
    const next: Message[] = [...messages, { role: 'user' as const, content }].slice(-20);
    setMessages(next);
    setDraft('');
    setError(null);
    setBusy(true);
    try {
      const { reply } = await apiFetch<{ reply: string }>('/api/Assistant/Chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', ...adminHeaders() },
        body: JSON.stringify({ messages: next, page: pathname }),
      });
      setMessages([...next, { role: 'assistant', content: reply }]);
    } catch (err) {
      setError(errorMessage(err, limits));
    } finally {
      setBusy(false);
      inputRef.current?.focus();
    }
  };

  return (
    <div
      className={`fixed right-4 bottom-[calc(var(--footer-height,2rem)+1rem)] z-40 flex flex-col items-end gap-3 ${open ? 'max-sm:z-[60]' : ''}`}
    >
      {open && (
        <div
          ref={panelRef}
          role="dialog"
          aria-labelledby={titleId}
          onKeyDown={(e) => e.key === 'Escape' && close()}
          className="assistant-pop flex max-h-[min(32rem,60vh)] w-[min(22rem,calc(100vw-2rem))] sm:max-h-[min(42rem,75vh)] sm:w-[27rem] lg:w-[30rem] assistant-panel flex-col overflow-hidden rounded-xl shadow-[0_12px_40px_rgba(0,0,0,0.35)] ring-2 ring-accent-600/60 dark:ring-accent-400/50 max-sm:fixed max-sm:inset-0 max-sm:pt-[var(--vv-top,0px)] max-sm:pb-[var(--vv-bottom,0px)] max-sm:max-h-none max-sm:w-full max-sm:rounded-none max-sm:ring-0"
        >
          <div className="assistant-header flex items-center justify-between px-4 py-2 text-on-accent">
            <h2 id={titleId} className="flex items-center gap-2 text-sm font-semibold sm:text-base">
              <span className="puck-hello inline-flex rounded-full bg-white/90 p-0.5">
                <PuckBot className="h-7 w-7" />
              </span>
              Assistant <span className="font-normal">(experimental)</span>
            </h2>
            <button
              type="button"
              onClick={close}
              aria-label="Close assistant"
              className="rounded-md px-2 py-1 hover:bg-white/20"
            >
              ✕
            </button>
          </div>
          <div
            ref={logRef}
            data-scroll
            aria-live="polite"
            className="flex flex-1 flex-col gap-2.5 overflow-y-auto overscroll-contain px-3 py-3 text-sm sm:text-base text-surface-800 dark:text-surface-100"
          >
            {messages.length === 0 && (
              <div className="space-y-2 text-surface-600 dark:text-surface-300">
                <p>Ask about today&apos;s games, a team, or the model&apos;s odds.</p>
                <p className="text-xs">
                  {limits.unlimited
                    ? 'Small local AI. Admin: no question limit. '
                    : `Small local AI: ${limits.burst} questions, then 1 every ${limits.minutesPerQuestion} min. `}
                  <Link
                    to="/about#mcp"
                    // The full-screen phone chat would hide the section
                    onClick={() => window.matchMedia(PHONE).matches && close()}
                    className="text-accent-600 dark:text-accent-400 underline hover:no-underline"
                  >
                    Connect your own AI
                  </Link>
                </p>
              </div>
            )}
            {messages.map((m, i) =>
              m.role === 'user' ? (
                <p
                  key={i}
                  className="bubble-in ml-auto w-fit max-w-[85%] whitespace-pre-wrap break-words rounded-2xl rounded-br-md bg-accent-600 px-3.5 py-2 text-on-accent shadow-sm"
                >
                  <span className="sr-only">You: </span>
                  {m.content}
                </p>
              ) : (
                <div key={i} className="bubble-in flex max-w-[85%] items-end gap-2">
                  <Avatar />
                  <p className={BOT_BUBBLE}>
                    <span className="sr-only">Assistant: </span>
                    {renderReply(m.content)}
                  </p>
                </div>
              ),
            )}
            {busy && (
              <div className="bubble-in flex items-end gap-2">
                <Avatar hop />
                <p className={`${BOT_BUBBLE} flex gap-1 text-lg leading-none`}>
                  <span className="sr-only">Thinking</span>
                  <span aria-hidden="true" className="throbber-dot">
                    •
                  </span>
                  <span aria-hidden="true" className="throbber-dot [animation-delay:200ms]">
                    •
                  </span>
                  <span aria-hidden="true" className="throbber-dot [animation-delay:400ms]">
                    •
                  </span>
                </p>
              </div>
            )}
            {error && (
              <div className="bubble-in flex max-w-[85%] items-end gap-2">
                <Avatar />
                <p className="w-fit rounded-2xl rounded-bl-md bg-red-50 px-3.5 py-2 text-red-800 shadow-sm dark:bg-red-950/60 dark:text-red-200">
                  {error}
                </p>
              </div>
            )}
          </div>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              void send();
            }}
            className="flex gap-2 border-t border-surface-200 dark:border-white/[0.06] p-3"
          >
            <label htmlFor={`${titleId}-input`} className="sr-only">
              Message
            </label>
            <textarea
              id={`${titleId}-input`}
              ref={inputRef}
              data-scroll
              rows={1}
              maxLength={2000}
              value={draft}
              onChange={(e) => setDraft(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                  e.preventDefault();
                  void send();
                }
              }}
              placeholder="Ask a question…"
              className="min-w-0 flex-1 resize-none rounded-lg border border-surface-300 dark:border-white/10 bg-white/70 dark:bg-black/30 max-h-36 px-3 py-2 text-base text-surface-900 dark:text-surface-50 placeholder:text-surface-500 dark:placeholder:text-surface-400"
            />
            <button
              type="submit"
              // Keep focus (and the phone keyboard) in the message box when Send is tapped
              onMouseDown={(e) => e.preventDefault()}
              disabled={busy || !draft.trim()}
              className="rounded-lg bg-accent-600 px-4 py-2 text-sm sm:text-base font-semibold text-on-accent disabled:opacity-60"
            >
              Send
            </button>
          </form>
        </div>
      )}
      <button
        ref={launcherRef}
        type="button"
        onClick={() => (open ? close() : setOpen(true))}
        aria-expanded={open}
        aria-label={open ? 'Close assistant' : 'Open assistant'}
        className={`flex h-14 w-14 items-center justify-center rounded-full bg-white shadow-lg ring-2 ring-accent-600 transition-transform hover:-translate-y-0.5 dark:ring-accent-400 ${open ? 'max-sm:hidden' : ''}`}
      >
        <PuckBot className="h-10 w-10" />
      </button>
    </div>
  );
}
