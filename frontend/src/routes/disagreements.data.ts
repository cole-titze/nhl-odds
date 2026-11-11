// /disagreements/data.json?season=2025: the Disagreements page's rows as JSON, for client-side
// navigation and the season selector (the first load renders them on the server)
import type { LoaderFunctionArgs } from 'react-router';
import { forwardClientIp } from '../api/client';
import { getDisagreements } from '../utils/disagreements.server';
import { FIRST_SEASON, getCurrentSeason } from '../utils/season';

export async function loader({ request }: LoaderFunctionArgs) {
  const season = Number(new URL(request.url).searchParams.get('season') ?? getCurrentSeason());
  if (!Number.isInteger(season) || season < FIRST_SEASON || season > getCurrentSeason()) {
    return Response.json({ message: 'Unknown season.' }, { status: 400 });
  }
  try {
    const data = await getDisagreements(season, forwardClientIp(request));
    return Response.json(data, { headers: { 'Cache-Control': 'public, max-age=60' } });
  } catch {
    return Response.json({ message: "Couldn't load the games." }, { status: 502 });
  }
}
