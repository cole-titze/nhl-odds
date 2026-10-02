// Theme registry. To add a theme: add an entry here, add its icon in
// ThemePicker.tsx, and (if scenic) a `.scene-<id>` backdrop in ThemeBackdrop.tsx
// plus `.theme-<id>` styles in index.css.
//
// - dark:   also applies the `dark` class, so the theme inherits all dark: styles
// - scenic: transparent page shell over a fixed backdrop; glass/nav styling is
//           driven by CSS variables the theme sets in index.css
export const THEMES = [
  { id: 'light', label: 'Light', dark: false, scenic: false, metaColor: '#fafafa' },
  { id: 'dark', label: 'Dark', dark: true, scenic: false, metaColor: '#141418' },
  { id: 'ice', label: 'Fresh Ice', dark: false, scenic: true, metaColor: '#eaf2f7' },
  { id: 'stars', label: 'Stars', dark: true, scenic: true, metaColor: '#05070c' },
  { id: 'nebula', label: 'Nebula', dark: true, scenic: true, metaColor: '#06040c' },
  { id: 'aurora', label: 'Northern Lights', dark: true, scenic: true, metaColor: '#03070a' },
  { id: 'scoreboard', label: 'Scoreboard', dark: true, scenic: true, metaColor: '#050505' },
  {
    id: 'contrast-light',
    label: 'High Contrast Light',
    dark: false,
    scenic: true,
    metaColor: '#ffffff',
  },
  {
    id: 'contrast-dark',
    label: 'High Contrast Dark',
    dark: true,
    scenic: true,
    metaColor: '#000000',
  },
  { id: 'team', label: 'Team Colors', dark: true, scenic: true, metaColor: '#07090d' },
] as const;

export type ThemeId = (typeof THEMES)[number]['id'];
export type ThemeDef = (typeof THEMES)[number];

export const themeById = (id: string): ThemeDef | undefined => THEMES.find((t) => t.id === id);

export interface TeamColors {
  abbrev: string;
  name: string;
  /** Readable on a dark background; drives the accent color */
  accent: string;
  /** Background glow colors */
  primary: string;
  secondary: string;
}

// Abbreviations come from the API's logo URIs. Colors are approximate.
export const TEAMS: TeamColors[] = [
  { abbrev: 'ANA', name: 'Ducks', accent: '#F47A38', primary: '#F47A38', secondary: '#B9975B' },
  { abbrev: 'BOS', name: 'Bruins', accent: '#FFB81C', primary: '#FFB81C', secondary: '#5a5a5a' },
  { abbrev: 'BUF', name: 'Sabres', accent: '#FCB514', primary: '#003087', secondary: '#FCB514' },
  { abbrev: 'CGY', name: 'Flames', accent: '#E8323C', primary: '#D2001C', secondary: '#FAAF19' },
  {
    abbrev: 'CAR',
    name: 'Hurricanes',
    accent: '#E8323C',
    primary: '#CE1126',
    secondary: '#A2AAAD',
  },
  {
    abbrev: 'CHI',
    name: 'Blackhawks',
    accent: '#E8323C',
    primary: '#CF0A2C',
    secondary: '#FF671B',
  },
  { abbrev: 'COL', name: 'Avalanche', accent: '#4A90D9', primary: '#6F263D', secondary: '#236192' },
  {
    abbrev: 'CBJ',
    name: 'Blue Jackets',
    accent: '#E8323C',
    primary: '#002654',
    secondary: '#CE1126',
  },
  { abbrev: 'DAL', name: 'Stars', accent: '#1FA672', primary: '#006847', secondary: '#8F8F8C' },
  { abbrev: 'DET', name: 'Red Wings', accent: '#E8323C', primary: '#CE1126', secondary: '#ffffff' },
  { abbrev: 'EDM', name: 'Oilers', accent: '#FF4C00', primary: '#FF4C00', secondary: '#041E42' },
  { abbrev: 'FLA', name: 'Panthers', accent: '#E8323C', primary: '#C8102E', secondary: '#B9975B' },
  { abbrev: 'LAK', name: 'Kings', accent: '#A2AAAD', primary: '#A2AAAD', secondary: '#333333' },
  { abbrev: 'MIN', name: 'Wild', accent: '#3A9A6E', primary: '#154734', secondary: '#A6192E' },
  { abbrev: 'MTL', name: 'Canadiens', accent: '#E8323C', primary: '#AF1E2D', secondary: '#192168' },
  { abbrev: 'NYI', name: 'Islanders', accent: '#F47D30', primary: '#00539B', secondary: '#F47D30' },
  { abbrev: 'NYR', name: 'Rangers', accent: '#3B74E0', primary: '#0038A8', secondary: '#CE1126' },
  { abbrev: 'NSH', name: 'Predators', accent: '#FFB81C', primary: '#FFB81C', secondary: '#041E42' },
  { abbrev: 'NJD', name: 'Devils', accent: '#E8323C', primary: '#CE1126', secondary: '#333333' },
  { abbrev: 'OTT', name: 'Senators', accent: '#E8323C', primary: '#DA1A32', secondary: '#B79257' },
  { abbrev: 'PHI', name: 'Flyers', accent: '#F74902', primary: '#F74902', secondary: '#333333' },
  { abbrev: 'PIT', name: 'Penguins', accent: '#FCB514', primary: '#FCB514', secondary: '#333333' },
  { abbrev: 'SJS', name: 'Sharks', accent: '#00A3AD', primary: '#006D75', secondary: '#EA7200' },
  { abbrev: 'SEA', name: 'Kraken', accent: '#99D9D9', primary: '#355464', secondary: '#99D9D9' },
  { abbrev: 'STL', name: 'Blues', accent: '#3D74D9', primary: '#002F87', secondary: '#FCB514' },
  { abbrev: 'TBL', name: 'Lightning', accent: '#3D74D9', primary: '#002868', secondary: '#ffffff' },
  {
    abbrev: 'TOR',
    name: 'Maple Leafs',
    accent: '#3D74D9',
    primary: '#00205B',
    secondary: '#ffffff',
  },
  { abbrev: 'UTA', name: 'Mammoth', accent: '#6CACE4', primary: '#6CACE4', secondary: '#333333' },
  { abbrev: 'VAN', name: 'Canucks', accent: '#1FA055', primary: '#00205B', secondary: '#00843D' },
  {
    abbrev: 'VGK',
    name: 'Golden Knights',
    accent: '#B4975A',
    primary: '#B4975A',
    secondary: '#333F42',
  },
  { abbrev: 'WSH', name: 'Capitals', accent: '#E8323C', primary: '#C8102E', secondary: '#041E42' },
  { abbrev: 'WPG', name: 'Jets', accent: '#5B8FD6', primary: '#004C97', secondary: '#AC162C' },
];

export const DEFAULT_TEAM = 'TOR';

export const teamLogo = (abbrev: string) =>
  `https://assets.nhle.com/logos/nhl/svg/${abbrev}_dark.svg`;

/** CSS custom properties the `team` theme reads (see index.css). */
export function teamVars(abbrev: string): Record<string, string> {
  const team =
    TEAMS.find((t) => t.abbrev === abbrev) ?? TEAMS.find((t) => t.abbrev === DEFAULT_TEAM)!;
  return {
    '--team-accent': team.accent,
    '--team-1': team.primary,
    '--team-2': team.secondary,
    '--team-logo': `url("${teamLogo(team.abbrev)}")`,
  };
}

export const TEAM_VAR_NAMES = Object.keys(teamVars(DEFAULT_TEAM));
