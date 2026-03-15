import { addDays, subDays } from 'date-fns';
import { formatDisplayDate, toDateInputValue } from '../utils/dates';

interface DateNavProps {
  date: Date;
  onChange: (date: Date) => void;
}

export function DateNav({ date, onChange }: DateNavProps) {
  return (
    <div className="flex flex-col sm:flex-row items-center gap-3">
      <button
        onClick={() => onChange(subDays(date, 1))}
        className="group flex items-center gap-1.5 px-4 py-2 rounded-lg glass text-sm font-medium text-surface-600 dark:text-surface-300 hover:text-surface-900 dark:hover:text-white transition-colors"
      >
        <svg
          className="w-3.5 h-3.5 transition-transform group-hover:-translate-x-0.5"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          strokeWidth={2.5}
        >
          <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
        </svg>
        Prev
      </button>
      <div className="flex items-center gap-3">
        <h2 className="font-display text-xl font-bold tracking-tight">{formatDisplayDate(date)}</h2>
        <input
          type="date"
          value={toDateInputValue(date)}
          onChange={(e) => {
            if (e.target.value) onChange(new Date(e.target.value + 'T12:00:00'));
          }}
          className="px-2.5 py-1.5 rounded-lg glass text-sm font-mono cursor-pointer"
        />
      </div>
      <button
        onClick={() => onChange(addDays(date, 1))}
        className="group flex items-center gap-1.5 px-4 py-2 rounded-lg glass text-sm font-medium text-surface-600 dark:text-surface-300 hover:text-surface-900 dark:hover:text-white transition-colors"
      >
        Next
        <svg
          className="w-3.5 h-3.5 transition-transform group-hover:translate-x-0.5"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          strokeWidth={2.5}
        >
          <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" />
        </svg>
      </button>
    </div>
  );
}
