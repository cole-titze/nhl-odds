import { useEffect, useId, useRef, useState } from 'react';
import { Link, useLocation } from 'react-router';
import { ApiError, apiFetch } from '../api/client';
import { useHydrated } from '../hooks/useHydrated';

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
}

function errorMessage(err: unknown, limits: Limits) {
  if (err instanceof ApiError && err.status === 429)
    return `That's the limit for now — you get one more question every ${limits.minutesPerQuestion} minutes.`;
  if (err instanceof ApiError && err.status === 503)
    return 'Someone else is asking a question right now. Try again in a minute.';
  return "Sorry, the assistant couldn't answer. It runs on a home computer that may be off.";
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

  useEffect(() => {
    if (!hydrated) return;
    apiFetch<Partial<Limits>>('/api/Assistant/Status')
      .then((l) =>
        setLimits({ burst: l.burst ?? 3, minutesPerQuestion: l.minutesPerQuestion ?? 2 }),
      )
      .catch(() => setLimits(null));
  }, [hydrated]);

  useEffect(() => {
    if (open) inputRef.current?.focus();
  }, [open]);

  useEffect(() => {
    logRef.current?.scrollTo({ top: logRef.current.scrollHeight });
  }, [messages, busy]);

  if (!limits) return null;

  const close = () => {
    setOpen(false);
    launcherRef.current?.focus();
  };

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
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ messages: next, page: pathname }),
      });
      setMessages([...next, { role: 'assistant', content: reply }]);
    } catch (err) {
      setError(errorMessage(err, limits));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="fixed right-4 bottom-[calc(var(--footer-height,2rem)+1rem)] z-40 flex flex-col items-end gap-3">
      {open && (
        <div
          role="dialog"
          aria-labelledby={titleId}
          onKeyDown={(e) => e.key === 'Escape' && close()}
          className="flex max-h-[min(32rem,60vh)] w-[min(22rem,calc(100vw-2rem))] flex-col overflow-hidden rounded-xl bg-white dark:bg-surface-900 shadow-[0_12px_40px_rgba(0,0,0,0.35)] ring-2 ring-accent-600/60 dark:ring-accent-400/50"
        >
          <div className="flex items-center justify-between bg-accent-600 px-4 py-2 text-on-accent">
            <h2 id={titleId} className="text-sm font-semibold">
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
            aria-live="polite"
            className="flex-1 space-y-3 overflow-y-auto px-4 py-3 text-sm text-surface-800 dark:text-surface-100"
          >
            {messages.length === 0 && (
              <div className="space-y-2 text-surface-600 dark:text-surface-300">
                <p>Ask about today&apos;s games, a team, or the model&apos;s odds.</p>
                <p className="text-xs">
                  Small home-run AI: {limits.burst} questions, then 1 every{' '}
                  {limits.minutesPerQuestion} min.{' '}
                  <Link
                    to="/about#mcp"
                    onClick={close}
                    className="text-accent-600 dark:text-accent-400 underline hover:no-underline"
                  >
                    Connect your own AI
                  </Link>
                </p>
              </div>
            )}
            {messages.map((m, i) => (
              <p
                key={i}
                className={`whitespace-pre-wrap break-words rounded-lg px-3 py-2 ${
                  m.role === 'user'
                    ? 'ml-6 bg-accent-600 text-on-accent'
                    : 'mr-6 bg-surface-200/70 dark:bg-white/[0.06]'
                }`}
              >
                <span className="sr-only">{m.role === 'user' ? 'You: ' : 'Assistant: '}</span>
                {m.role === 'assistant' ? renderReply(m.content) : m.content}
              </p>
            ))}
            {busy && <p className="text-surface-600 dark:text-surface-300">Thinking…</p>}
            {error && <p className="text-red-700 dark:text-red-300">{error}</p>}
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
              className="min-w-0 flex-1 resize-none rounded-lg border border-surface-300 dark:border-white/10 bg-white dark:bg-surface-900 px-3 py-2 text-sm text-surface-900 dark:text-surface-50 placeholder:text-surface-500 dark:placeholder:text-surface-400"
            />
            <button
              type="submit"
              disabled={busy || !draft.trim()}
              className="rounded-lg bg-accent-600 px-3 py-2 text-sm font-semibold text-on-accent disabled:opacity-60"
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
        className="flex h-12 w-12 items-center justify-center rounded-full bg-accent-600 text-on-accent shadow-lg"
      >
        <svg
          viewBox="0 0 24 24"
          aria-hidden="true"
          className="h-6 w-6"
          fill="none"
          stroke="currentColor"
          strokeWidth={2}
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M8 10h8M8 14h5M21 12a8.5 8.5 0 0 1-12.4 7.6L3 21l1.4-5.6A8.5 8.5 0 1 1 21 12Z"
          />
        </svg>
      </button>
    </div>
  );
}
