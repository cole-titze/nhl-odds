/** A short sample of a chart line (its color and dash pattern) for legends and tooltips. */
export function LineKey({
  color,
  dash,
  active = true,
}: {
  color: string;
  dash: string;
  active?: boolean;
}) {
  return (
    <svg aria-hidden="true" width="18" height="6" viewBox="0 0 18 6" className="shrink-0">
      <line
        x1="0"
        y1="3"
        x2="18"
        y2="3"
        stroke={active ? color : 'currentColor'}
        strokeOpacity={active ? 1 : 0.5}
        strokeWidth="2.5"
        strokeDasharray={dash || undefined}
      />
    </svg>
  );
}
