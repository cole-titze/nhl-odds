import { addDays, format, parseISO } from 'date-fns';

export function formatDate(date: Date): string {
  return format(date, 'yyyy-MM-dd');
}

export function formatDisplayDate(date: Date): string {
  return format(date, 'EEEE, MMMM d, yyyy');
}

// Building an Intl.DateTimeFormat costs far more than formatting with one, and charts format
// every game of a season (on a phone that was most of the Teams page's load time), so each
// zone's formatter is made once
const shortDateFormats = new Map<string, Intl.DateTimeFormat>();

// timeZone formats in that zone instead of the local one (see useDisplayTimeZone)
export function formatShortDate(dateStr: string, timeZone?: string): string {
  const iso = dateStr.length === 10 ? dateStr + 'T12:00:00' : dateStr;
  if (timeZone) {
    let formatter = shortDateFormats.get(timeZone);
    if (!formatter) {
      formatter = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', timeZone });
      shortDateFormats.set(timeZone, formatter);
    }
    return formatter.format(parseISO(iso));
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

// Made on first use, not when the module loads: creating one is the expensive part
let centralDate: Intl.DateTimeFormat | undefined;

// Bucket a game's UTC datetime to a yyyy-MM-dd string in Central time (DST-aware).
export function gameDateBucket(isoGameDate: string): string {
  centralDate ??= new Intl.DateTimeFormat('en-CA', {
    timeZone: 'America/Chicago',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  });
  return centralDate.format(new Date(isoGameDate));
}

/** A UTC game time's day in Central time (yyyy-MM-dd), the day the Games feed lists it under */
export function centralDay(utc: string): string {
  const iso = /(?:[zZ]|[+-]\d\d:\d\d)$/.test(utc) ? utc : `${utc}Z`;
  // Same Central-time formatter as gameDateBucket, made on first use rather than at page load
  return gameDateBucket(iso);
}

/** A UTC game time's day in Central time, as people write it: "November 21, 2026" */
export function longGameDay(utc: string): string {
  const iso = /(?:[zZ]|[+-]\d\d:\d\d)$/.test(utc) ? utc : `${utc}Z`;
  return new Date(iso).toLocaleDateString('en-US', {
    timeZone: 'America/Chicago',
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  });
}
