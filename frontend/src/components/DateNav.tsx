import { addDays, subDays } from 'date-fns';
import { formatDisplayDate, toDateInputValue } from '../utils/dates';

interface DateNavProps {
  date: Date;
  onChange: (date: Date) => void;
}

export function DateNav({ date, onChange }: DateNavProps) {
  return (
    <div className="flex flex-col sm:flex-row items-center gap-3 mb-6">
      <button
        onClick={() => onChange(subDays(date, 1))}
        className="px-3 py-1.5 rounded-md border border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 text-sm"
      >
        Prev Day
      </button>
      <div className="flex items-center gap-3">
        <h2 className="text-lg font-semibold">{formatDisplayDate(date)}</h2>
        <input
          type="date"
          value={toDateInputValue(date)}
          onChange={(e) => {
            if (e.target.value) onChange(new Date(e.target.value + 'T12:00:00'));
          }}
          className="px-2 py-1 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm"
        />
      </div>
      <button
        onClick={() => onChange(addDays(date, 1))}
        className="px-3 py-1.5 rounded-md border border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 text-sm"
      >
        Next Day
      </button>
    </div>
  );
}
