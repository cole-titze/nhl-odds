import { getSeasonOptions, formatSeasonLabel } from '../utils/season';

interface SeasonSelectorProps {
  value: number;
  onChange: (season: number) => void;
}

export function SeasonSelector({ value, onChange }: SeasonSelectorProps) {
  const options = getSeasonOptions();

  return (
    <select
      value={value}
      onChange={(e) => onChange(Number(e.target.value))}
      className="px-3 py-1.5 rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm"
    >
      {options.map((y) => (
        <option key={y} value={y}>{formatSeasonLabel(y)}</option>
      ))}
    </select>
  );
}
