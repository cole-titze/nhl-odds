/** Marks a player on their team's injured reserve list (the NHL's roster report) */
export function InjuredReserveBadge({ short = false }: { short?: boolean }) {
  return (
    <span className="inline-flex items-center rounded px-1.5 py-0.5 text-[11px] font-semibold uppercase tracking-wide bg-amber-100 text-amber-900 dark:bg-amber-400/15 dark:text-amber-200">
      {short ? (
        <>
          <span aria-hidden="true">IR</span>
          <span className="sr-only">Injured reserve</span>
        </>
      ) : (
        'Injured reserve'
      )}
    </span>
  );
}
