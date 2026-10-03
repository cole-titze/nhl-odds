import { useId, useState } from 'react';
import { ScrollRegion } from './A11y';

export interface ChartTableRow {
  key: string;
  label: string;
  values: Record<string, number | undefined>;
}

interface Props {
  caption: string;
  /** Header for the first column (e.g. "Date") */
  rowHeader: string;
  /** Data columns, in display order */
  columns: string[];
  rows: ChartTableRow[];
  format: (value: number, column: string) => string;
}

/** A show/hide toggle that presents a chart's data as a table, so its numbers
 * are available to screen reader users (and anyone who'd rather read them). */
export function ChartDataTable({ caption, rowHeader, columns, rows, format }: Props) {
  const [open, setOpen] = useState(false);
  const id = useId();

  return (
    <div className="mt-3">
      <button
        type="button"
        aria-expanded={open}
        aria-controls={id}
        onClick={() => setOpen(!open)}
        className="text-xs font-medium text-surface-500 dark:text-surface-400 hover:text-surface-900 dark:hover:text-white underline underline-offset-2"
      >
        {open ? 'Hide data table' : 'Show data table'}
      </button>
      {open && (
        <div id={id} className="mt-3">
          <ScrollRegion label={caption} className="max-h-80 overflow-auto rounded-lg">
            <table className="w-full text-xs font-mono">
              <caption className="sr-only">{caption}</caption>
              <thead className="sticky top-0 bg-surface-100 dark:bg-surface-850">
                <tr className="text-left text-surface-600 dark:text-surface-400">
                  <th scope="col" className="py-2 px-3 font-semibold">
                    {rowHeader}
                  </th>
                  {columns.map((c) => (
                    <th key={c} scope="col" className="py-2 px-3 font-semibold text-right">
                      {c}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr
                    key={row.key}
                    className="border-t border-surface-200/60 dark:border-white/[0.04]"
                  >
                    <th
                      scope="row"
                      className="py-1.5 px-3 text-left font-normal text-surface-600 dark:text-surface-400 whitespace-nowrap"
                    >
                      {row.label}
                    </th>
                    {columns.map((c) => {
                      const v = row.values[c];
                      return (
                        <td key={c} className="py-1.5 px-3 text-right">
                          {v == null ? (
                            <>
                              <span aria-hidden="true">–</span>
                              <span className="sr-only">no data</span>
                            </>
                          ) : (
                            format(v, c)
                          )}
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </ScrollRegion>
        </div>
      )}
    </div>
  );
}
