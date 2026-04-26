import { addDays, format, parseISO } from 'date-fns';

export function formatDate(date: Date): string {
  return format(date, 'yyyy-MM-dd');
}

export function formatDisplayDate(date: Date): string {
  return format(date, 'EEEE, MMMM d, yyyy');
}

export function formatShortDate(dateStr: string): string {
  return format(parseISO(dateStr), 'MMM d');
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
