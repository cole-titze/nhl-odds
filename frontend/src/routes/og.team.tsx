// /og/team/:teamId.png: a team's link preview (record, model accuracy, next game) for
// the current season
import type { LoaderFunctionArgs } from 'react-router';
import { ApiError } from '../api/client';
import { getTeam } from '../api/teams';
import { TeamCard, teamAbbrev, type TeamCardData } from '../og/cards';
import { imageId } from '../og/paths';
import { imageResponse, logoDataUri } from '../og/render.server';
import { getCurrentSeason } from '../utils/season';
import type { TeamVM } from '../types';

function nextGame(team: TeamVM): TeamCardData['next'] {
  // Newest first, so the next game is the last one not yet played
  const upcoming = team.gameOddsVM.filter((g) => !g.hasBeenPlayed && g.homeTeam && g.awayTeam);
  const game = upcoming.at(-1);
  if (!game) return null;
  const home = game.homeTeam!.id === team.id;
  const us = home ? game.homeTeam! : game.awayTeam!;
  const them = home ? game.awayTeam! : game.homeTeam!;
  return {
    opponent: teamAbbrev(them),
    home,
    gameDate: game.gameDate,
    modelOdds: us.modelOdds,
  };
}

export async function loader({ params, request }: LoaderFunctionArgs) {
  const id = imageId(params.file);
  const season = getCurrentSeason();
  let team: TeamVM | null = null;
  if (id != null) {
    try {
      team = await getTeam(id, season);
    } catch (e) {
      if (!(e instanceof ApiError && (e.status === 404 || e.status === 400))) throw e;
    }
  }
  if (!team?.teamName) return new Response('Not found', { status: 404 });

  const logo = await logoDataUri(team.logoUri);
  const data: TeamCardData = {
    team: {
      locationName: team.locationName,
      teamName: team.teamName,
      logoUri: team.logoUri,
      seasonWins: team.seasonWins,
      seasonLosses: team.seasonLosses,
      seasonOvertimeLosses: team.seasonOvertimeLosses,
      totalGameCount: team.totalGameCount,
      totalModelAccurateGameCount: team.totalModelAccurateGameCount,
    },
    season,
    next: nextGame(team),
  };
  const logos = { [team.logoUri]: logo };
  return imageResponse(
    request,
    { v: 1, ...data, logo: logo != null },
    () => <TeamCard {...data} logos={logos} />,
    60 * 60,
  );
}
