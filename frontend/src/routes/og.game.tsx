// /og/game/:gameId.png: a game's link preview (both teams, the model's odds, a book's
// line, and the score once played)
import type { LoaderFunctionArgs } from 'react-router';
import { getGame } from '../api/gameOdds';
import { GameCard, cardBook } from '../og/cards';
import { imageId } from '../og/paths';
import { imageResponse, logoDataUri } from '../og/render.server';

export async function loader({ params, request }: LoaderFunctionArgs) {
  const id = imageId(params.file);
  const game = id == null ? undefined : await getGame(String(id));
  if (!game?.id) return new Response('Not found', { status: 404 });

  const logoUris = [game.awayTeam?.logoUri, game.homeTeam?.logoUri].filter((u) => u != null);
  const logos = Object.fromEntries(
    await Promise.all(logoUris.map(async (u) => [u, await logoDataUri(u)] as const)),
  );
  const book = cardBook(game);
  // Only what the card draws, so the image is redrawn when one of these changes
  const drawn = {
    v: 1,
    id: game.id,
    date: game.gameDate,
    type: game.gameType,
    played: game.hasBeenPlayed,
    winner: game.winner,
    teams: [game.awayTeam, game.homeTeam].map((t) => t && [t.id, t.goals, t.modelOdds]),
    book: book && [book.bookmakerName, book.awayOdds, book.homeOdds],
    logos: logoUris.map((u) => logos[u] != null),
  };
  // Finished games don't change; upcoming ones get new odds through the day
  const maxAge = game.hasBeenPlayed ? 24 * 60 * 60 : 15 * 60;
  return imageResponse(request, drawn, () => <GameCard game={game} logos={logos} />, maxAge);
}
