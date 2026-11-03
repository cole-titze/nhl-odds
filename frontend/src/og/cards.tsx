// The link preview images (render.server.ts draws them). Satori supports a subset of CSS
// with inline styles only, and every element with more than one child must be a flex box.
import type { CSSProperties, ReactNode } from 'react';
import type { GameOddsVM, MatchupTeamVM, SeasonTotalsVM, TeamSummaryVM } from '../types';
import { Winner } from '../types';
import { formatSeasonLabel } from '../utils/season';
import { formatOdds } from '../utils/oddsFormat';
import { SITE_URL } from '../utils/site';
import { OG_HEIGHT, OG_WIDTH } from './render.server';

// The site's dark theme
const C = {
  bg: '#0e1016',
  text: '#fafafa',
  muted: '#a1a1aa',
  faint: '#52525b',
  accent: '#3b82f6',
  accentLight: '#60a5fa',
  panel: 'rgba(255,255,255,0.05)',
  border: 'rgba(255,255,255,0.10)',
  green: '#34d399',
  red: '#f87171',
};

// Server-rendered times on the site are Central until the browser takes over
const TIME_ZONE = 'America/Chicago';

const mono: CSSProperties = { fontFamily: 'Mono' };

/** Logos fetched for a card, by logo URL; missing ones draw the team's initials. */
export type Logos = Record<string, string | null>;

/** The NHL abbreviation from a logo URL (…/svg/TOR_light.svg), else the team name. */
export function teamAbbrev(team: Pick<MatchupTeamVM, 'logoUri' | 'teamName'>): string {
  return team.logoUri.match(/\/([A-Z]{2,3})_(?:light|dark)\.svg/)?.[1] ?? team.teamName;
}

export function pct(p: number | null | undefined): string {
  return p == null ? '–' : `${Math.round(p * 100)}%`;
}

function Logo({
  team,
  logos,
  size,
}: {
  team: Pick<MatchupTeamVM, 'logoUri' | 'teamName'>;
  logos: Logos;
  size: number;
}) {
  const src = logos[team.logoUri];
  if (src) return <img src={src} width={size} height={size} style={{ objectFit: 'contain' }} />;
  const initials = team.teamName
    .split(/\s+/)
    .map((w) => w[0])
    .join('')
    .slice(0, 3);
  return (
    <div
      style={{
        width: size,
        height: size,
        borderRadius: size / 2,
        background: C.panel,
        border: `2px solid ${C.border}`,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        fontSize: size / 3,
        fontWeight: 800,
        color: C.muted,
      }}
    >
      {initials}
    </div>
  );
}

/** The brand header and site footer every card shares. */
function Frame({ label, children }: { label?: string; children: ReactNode }) {
  return (
    <div
      style={{
        width: OG_WIDTH,
        height: OG_HEIGHT,
        display: 'flex',
        flexDirection: 'column',
        padding: '44px 56px 36px',
        // One linear gradient: resvg takes ~0.5 s per radial one
        background: `linear-gradient(160deg, #1a2a4a 0%, ${C.bg} 55%, #121a2c 100%)`,
        color: C.text,
        fontFamily: 'Outfit',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
          <div
            style={{
              width: 48,
              height: 48,
              borderRadius: 12,
              background: C.accent,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: 30,
              fontWeight: 800,
            }}
          >
            N
          </div>
          <div style={{ fontSize: 30, fontWeight: 600, letterSpacing: -0.5 }}>NHL Odds</div>
        </div>
        {label && (
          <div
            style={{
              ...mono,
              fontSize: 22,
              fontWeight: 700,
              letterSpacing: 3,
              textTransform: 'uppercase',
              color: C.accentLight,
            }}
          >
            {label}
          </div>
        )}
      </div>
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column' }}>{children}</div>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          fontSize: 22,
          color: C.muted,
        }}
      >
        <div>{SITE_URL.replace(/^https?:\/\//, '')}</div>
        <div>Machine learning odds for every NHL game</div>
      </div>
    </div>
  );
}

function formatGameTime(gameDate: string): string {
  const d = new Date(gameDate);
  const day = d.toLocaleDateString('en-US', {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    timeZone: TIME_ZONE,
  });
  const time = d.toLocaleTimeString('en-US', {
    hour: 'numeric',
    minute: '2-digit',
    timeZone: TIME_ZONE,
  });
  return `${day} · ${time} CT`;
}

function formatGameDay(gameDate: string): string {
  return new Date(gameDate).toLocaleDateString('en-US', {
    month: 'long',
    day: 'numeric',
    year: 'numeric',
    timeZone: TIME_ZONE,
  });
}

/** The book shown on a game's card: DraftKings when it has odds, else the first listed. */
export function cardBook(game: GameOddsVM) {
  return (
    (game.bookmakerOdds ?? []).find((b) => b.bookmakerName === 'DraftKings') ??
    game.bookmakerOdds?.[0]
  );
}

/** Which side the model favoured, and whether it won. null before the game or without odds. */
export function modelCalledIt(game: GameOddsVM): boolean | null {
  const home = game.homeTeam?.modelOdds;
  if (!game.hasBeenPlayed || home == null) return null;
  return home >= 0.5 === (game.winner === Winner.HOME);
}

function GameSide({
  team,
  logos,
  won,
}: {
  team: MatchupTeamVM | null;
  logos: Logos;
  won: boolean;
}) {
  return (
    <div
      style={{
        width: 380,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        gap: 10,
      }}
    >
      {team ? (
        <Logo team={team} logos={logos} size={140} />
      ) : (
        <div style={{ width: 140, height: 140, display: 'flex' }} />
      )}
      <div style={{ fontSize: 26, color: C.muted }}>{team?.locationName ?? 'TBD'}</div>
      <div
        style={{
          fontSize: 40,
          fontWeight: won ? 800 : 600,
          lineHeight: 1,
          textAlign: 'center',
        }}
      >
        {team?.teamName ?? ''}
      </div>
    </div>
  );
}

/** The model's split as a bar, away on the left like the rest of the site. */
function ProbabilityBar({ away, home }: { away: MatchupTeamVM; home: MatchupTeamVM }) {
  const awayP = away.modelOdds ?? 0.5;
  const segment = (p: number, bg: string, align: 'flex-start' | 'flex-end', text: string) => (
    <div
      style={{
        width: `${Math.max(p, 0.12) * 100}%`,
        height: '100%',
        background: bg,
        display: 'flex',
        alignItems: 'center',
        justifyContent: align,
        padding: '0 22px',
        ...mono,
        fontSize: 28,
        fontWeight: 700,
      }}
    >
      {text}
    </div>
  );
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
      <div style={{ display: 'flex', justifyContent: 'center', fontSize: 22, color: C.muted }}>
        Model win probability
      </div>
      <div
        style={{
          display: 'flex',
          height: 58,
          borderRadius: 14,
          overflow: 'hidden',
          border: `1px solid ${C.border}`,
        }}
      >
        {segment(awayP, 'rgba(96,165,250,0.30)', 'flex-start', `${teamAbbrev(away)} ${pct(awayP)}`)}
        {segment(1 - awayP, C.accent, 'flex-end', `${pct(1 - awayP)} ${teamAbbrev(home)}`)}
      </div>
    </div>
  );
}

export function GameCard({ game, logos }: { game: GameOddsVM; logos: Logos }) {
  const { homeTeam: home, awayTeam: away } = game;
  const played = game.hasBeenPlayed;
  const book = cardBook(game);
  const calledIt = modelCalledIt(game);
  const hasModel = home?.modelOdds != null && away != null;
  const playoffs = game.gameType === 3;

  return (
    <Frame label={played ? 'Final' : playoffs ? 'Playoffs' : 'Game preview'}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'center',
          marginTop: 4,
          fontSize: 26,
          color: C.muted,
        }}
      >
        {game.gameDate
          ? played
            ? `${formatGameDay(game.gameDate)}${playoffs ? ' · Playoffs' : ''}`
            : formatGameTime(game.gameDate)
          : ''}
      </div>
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          marginTop: 6,
        }}
      >
        <GameSide team={away} logos={logos} won={played && game.winner === Winner.AWAY} />
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6 }}>
          {played ? (
            <div style={{ ...mono, display: 'flex', fontSize: 84, fontWeight: 700 }}>
              {`${away?.goals ?? 0}–${home?.goals ?? 0}`}
            </div>
          ) : (
            <div style={{ fontSize: 44, color: C.muted }}>at</div>
          )}
        </div>
        <GameSide team={home} logos={logos} won={played && game.winner === Winner.HOME} />
      </div>
      <div style={{ flex: 1, minHeight: 16, display: 'flex' }} />
      {hasModel && home && away ? <ProbabilityBar away={away} home={home} /> : null}
      <div
        style={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          gap: 24,
          margin: '16px 0 20px',
          fontSize: 24,
          color: C.muted,
        }}
      >
        {book && away && home ? (
          <div style={{ ...mono, display: 'flex' }}>
            {`${book.bookmakerName}: ${teamAbbrev(away)} ${formatOdds(book.awayOdds, 'vegas')} · ${teamAbbrev(home)} ${formatOdds(book.homeOdds, 'vegas')}`}
          </div>
        ) : null}
        {calledIt != null ? (
          <div
            style={{
              display: 'flex',
              padding: '4px 16px',
              borderRadius: 999,
              fontWeight: 600,
              color: calledIt ? C.green : C.red,
              background: calledIt ? 'rgba(52,211,153,0.12)' : 'rgba(248,113,113,0.12)',
            }}
          >
            {calledIt ? 'Model called it' : 'Model missed'}
          </div>
        ) : null}
      </div>
    </Frame>
  );
}

function Stat({ value, label, accent }: { value: string; label: string; accent?: boolean }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
      <div
        style={{
          ...mono,
          fontSize: 52,
          fontWeight: 700,
          color: accent ? C.accentLight : C.text,
        }}
      >
        {value}
      </div>
      <div style={{ fontSize: 22, color: C.muted }}>{label}</div>
    </div>
  );
}

export interface TeamCardData {
  team: Pick<
    TeamSummaryVM,
    | 'locationName'
    | 'teamName'
    | 'logoUri'
    | 'seasonWins'
    | 'seasonLosses'
    | 'seasonOvertimeLosses'
    | 'totalGameCount'
    | 'totalModelAccurateGameCount'
  >;
  season: number;
  /** The team's next game, from its side */
  next: { opponent: string; home: boolean; gameDate: string; modelOdds: number | null } | null;
}

export function TeamCard({ team, season, next, logos }: TeamCardData & { logos: Logos }) {
  const accuracy =
    team.totalGameCount > 0 ? team.totalModelAccurateGameCount / team.totalGameCount : null;
  return (
    <Frame label={`${formatSeasonLabel(season)} season`}>
      <div style={{ flex: 1, display: 'flex', alignItems: 'center', gap: 56 }}>
        <Logo team={team} logos={logos} size={300} />
        <div style={{ display: 'flex', flexDirection: 'column', flex: 1 }}>
          <div style={{ fontSize: 34, color: C.muted }}>{team.locationName}</div>
          <div style={{ fontSize: 76, fontWeight: 800, lineHeight: 1.05, letterSpacing: -1 }}>
            {team.teamName}
          </div>
          <div style={{ display: 'flex', gap: 56, marginTop: 30 }}>
            <Stat
              value={`${team.seasonWins}-${team.seasonLosses}-${team.seasonOvertimeLosses}`}
              label="Record (W-L-OT)"
            />
            {accuracy != null && (
              <Stat
                value={pct(accuracy)}
                label={`Model right in ${team.totalModelAccurateGameCount} of ${team.totalGameCount}`}
                accent
              />
            )}
          </div>
          {next && (
            <div
              style={{
                display: 'flex',
                marginTop: 30,
                padding: '14px 20px',
                borderRadius: 14,
                background: C.panel,
                border: `1px solid ${C.border}`,
                fontSize: 26,
                gap: 12,
              }}
            >
              <div style={{ color: C.muted }}>Next:</div>
              <div style={{ fontWeight: 600 }}>{`${next.home ? 'vs' : 'at'} ${next.opponent}`}</div>
              <div style={{ color: C.muted }}>{formatGameTime(next.gameDate)}</div>
              {next.modelOdds != null && (
                <div style={{ ...mono, color: C.accentLight, fontWeight: 700 }}>
                  {`${pct(next.modelOdds)} to win`}
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </Frame>
  );
}

export interface SlateGame {
  away: Pick<MatchupTeamVM, 'logoUri' | 'teamName' | 'modelOdds' | 'goals'>;
  home: Pick<MatchupTeamVM, 'logoUri' | 'teamName' | 'modelOdds' | 'goals'>;
  played: boolean;
}

export const SLATE_SHOWN = 8;

/** The Games page: the day it opens on, every game with the model's odds (or the score). */
export function GamesCard({
  dayLabel,
  games,
  logos,
}: {
  dayLabel: string;
  games: SlateGame[];
  logos: Logos;
}) {
  const shown = games.slice(0, SLATE_SHOWN);
  // A light night gets one column of bigger rows
  const wide = shown.length <= 4;
  const fontSize = wide ? 34 : 28;
  const side = (t: SlateGame['away'], align: 'left' | 'right', played: boolean) => {
    const value = played ? String(t.goals) : pct(t.modelOdds);
    const label = <div style={{ fontSize, fontWeight: 600 }}>{teamAbbrev(t)}</div>;
    const number = (
      <div style={{ ...mono, fontSize, fontWeight: 700, color: played ? C.text : C.accentLight }}>
        {value}
      </div>
    );
    return (
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 12,
          flexDirection: align === 'left' ? 'row' : 'row-reverse',
        }}
      >
        <Logo team={t} logos={logos} size={wide ? 64 : 52} />
        {label}
        {number}
      </div>
    );
  };
  return (
    <Frame label="Games">
      <div style={{ display: 'flex', alignItems: 'baseline', gap: 18, margin: '18px 0 18px' }}>
        <div style={{ fontSize: 48, fontWeight: 800, letterSpacing: -0.5 }}>{dayLabel}</div>
        <div style={{ fontSize: 26, color: C.muted }}>
          {`${games.length} game${games.length === 1 ? '' : 's'}${games.length > shown.length ? `, first ${shown.length} shown` : ''} · model win probability`}
        </div>
      </div>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 14 }}>
        {shown.map((g, i) => (
          <div
            key={i}
            style={{
              width: wide ? 1088 : 537,
              height: wide ? 92 : 78,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              padding: wide ? '0 32px' : '0 20px',
              borderRadius: 14,
              background: C.panel,
              border: `1px solid ${C.border}`,
            }}
          >
            {side(g.away, 'left', g.played)}
            <div style={{ fontSize: 22, color: C.faint }}>{g.played ? 'final' : 'at'}</div>
            {side(g.home, 'right', g.played)}
          </div>
        ))}
      </div>
    </Frame>
  );
}

/** The Teams page: the model against DraftKings over the season. */
export function TeamsCard({ season, totals }: { season: number; totals: SeasonTotalsVM }) {
  const modelAcc =
    totals.totalGameCount > 0 ? totals.totalModelAccurateGameCount / totals.totalGameCount : null;
  const dkAcc =
    totals.draftKingsGameCount > 0
      ? totals.draftKingsAccurateGameCount / totals.draftKingsGameCount
      : null;
  return (
    <Frame label={`${formatSeasonLabel(season)} season`}>
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
        <div style={{ fontSize: 72, fontWeight: 800, letterSpacing: -1 }}>Model vs the books</div>
        <div style={{ fontSize: 30, color: C.muted, marginTop: 6 }}>
          {`Every NHL team, ${totals.totalGameCount} games played this season`}
        </div>
        <div style={{ display: 'flex', gap: 28, marginTop: 44 }}>
          {[
            { name: 'NHL Odds model', acc: modelAcc, ll: totals.modelLogLoss, accent: true },
            {
              name: 'DraftKings',
              acc: dkAcc,
              ll: totals.draftKingsGameCount > 0 ? totals.draftKingsLogLoss : null,
              accent: false,
            },
          ].map((s) => (
            <div
              key={s.name}
              style={{
                flex: 1,
                display: 'flex',
                flexDirection: 'column',
                gap: 18,
                padding: '24px 30px',
                borderRadius: 18,
                background: s.accent ? 'rgba(59,130,246,0.14)' : C.panel,
                border: `1px solid ${s.accent ? 'rgba(96,165,250,0.4)' : C.border}`,
              }}
            >
              <div style={{ fontSize: 28, fontWeight: 600 }}>{s.name}</div>
              <div style={{ display: 'flex', gap: 48 }}>
                <Stat value={pct(s.acc)} label="Picked the winner" accent={s.accent} />
                <Stat value={s.ll != null ? s.ll.toFixed(3) : '–'} label="Log loss" />
              </div>
            </div>
          ))}
        </div>
      </div>
    </Frame>
  );
}

/** Pages without data of their own: a title and a line under it. */
export function PageCard({ label, title, text }: { label?: string; title: string; text: string }) {
  return (
    <Frame label={label}>
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', justifyContent: 'center' }}>
        <div style={{ fontSize: 84, fontWeight: 800, letterSpacing: -1.5, lineHeight: 1.05 }}>
          {title}
        </div>
        <div
          style={{ fontSize: 36, color: C.muted, marginTop: 22, maxWidth: 980, lineHeight: 1.3 }}
        >
          {text}
        </div>
        <div style={{ display: 'flex', gap: 12, marginTop: 44 }}>
          {[0.62, 0.38].map((w, i) => (
            <div
              key={i}
              style={{
                width: 1088 * w - 6,
                height: 18,
                borderRadius: 9,
                background: i === 0 ? C.accent : 'rgba(96,165,250,0.30)',
              }}
            />
          ))}
        </div>
      </div>
    </Frame>
  );
}
