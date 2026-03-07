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
      className="px-3 py-2 rounded-lg glass text-sm font-mono font-medium cursor-pointer appearance-none bg-[length:16px] bg-[right_8px_center] bg-no-repeat"
      style={{
        backgroundImage: `url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' fill='none' viewBox='0 0 24 24' stroke='%23737373' stroke-width='2'%3E%3Cpath stroke-linecap='round' stroke-linejoin='round' d='M19 9l-7 7-7-7'/%3E%3C/svg%3E")`,
        paddingRight: '2rem',
      }}
    >
      {options.map((y) => (
        <option key={y} value={y}>
          {formatSeasonLabel(y)}
        </option>
      ))}
    </select>
  );
}
