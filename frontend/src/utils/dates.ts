import { addDays, format, parseISO } from 'date-fns';

export function formatDate(date: Date): string {
  return format(date, 'yyyy-MM-dd');
}

export function formatDisplayDate(date: Date): string {
  return format(date, 'EEEE, MMMM d, yyyy');
}

// timeZone formats in that zone instead of the local one (see useDisplayTimeZone)
export function formatShortDate(dateStr: string, timeZone?: string): string {
  const iso = dateStr.length === 10 ? dateStr + 'T12:00:00' : dateStr;
  if (timeZone) {
    return new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', timeZone }).format(
      parseISO(iso),
    );
  }
  return format(parseISO(iso), 'MMM d');
}

export function toDateInputValue(date: Date): string {
  return format(date, 'yyyy-MM-dd');
}

export function addDaysIso(iso: string, n: number): string {
  return formatDate(addDays(parseISO(iso), n));
}

// Inclusive list of yyyy-MM-dd strings from startIso to endIso (start <= end).
export function enumerateDateRange(startIso: string, endIso: string): string[] {
  const out: string[] = [];
  let cursor = startIso;
  while (cursor <= endIso) {
    out.push(cursor);
    cursor = addDaysIso(cursor, 1);
  }
  return out;
}
