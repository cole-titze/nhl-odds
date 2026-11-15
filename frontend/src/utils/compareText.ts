// String.localeCompare builds an ICU collator on every call, and even one shared Intl.Collator
// costs ~100 ms of a phone's page load to create. Sorting the 32 teams that way took 130–210 ms on
// every page, so sorts use plain code-unit order instead. For what's sorted here (team names, which
// are capitalized ASCII words apart from Montréal's accent, and yyyy-MM-dd dates) it gives the
// same order as localeCompare.
export const compareText = (a: string, b: string): number => (a < b ? -1 : a > b ? 1 : 0);
