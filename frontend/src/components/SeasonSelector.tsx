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
      className="pl-3 pr-8 py-2 rounded-lg glass text-sm font-mono font-medium cursor-pointer appearance-none bg-[url('/chevron-down.svg')] bg-[length:16px] bg-[right_8px_center] bg-no-repeat"
    >
      {options.map((y) => (
        <option key={y} value={y}>
          {formatSeasonLabel(y)}
        </option>
      ))}
    </select>
  );
}
