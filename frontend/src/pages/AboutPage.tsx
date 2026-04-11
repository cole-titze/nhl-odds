export function AboutPage() {
  return (
    <div className="max-w-2xl mx-auto">
      <h1 className="font-display text-3xl font-bold tracking-tight mb-8">About NHL Odds</h1>

      <div className="space-y-6">
        <section className="glass rounded-xl p-6 space-y-3">
          <h2 className="font-display text-lg font-bold tracking-tight">What is this?</h2>
          <p className="text-surface-600 dark:text-surface-400 leading-relaxed">
            NHL Odds is a machine learning project that predicts the outcome of NHL games. The model
            is trained on historical game data from the 2009 season onwards, using play-by-play
            statistics including shots, goals, hits, penalties, and more.
          </p>
        </section>

        <section className="glass rounded-xl p-6 space-y-3">
          <h2 className="font-display text-lg font-bold tracking-tight">
            How are predictions made?
          </h2>
          <p className="text-surface-600 dark:text-surface-400 leading-relaxed">
            Before each game, the model calculates the probability that the home team will win based
            on each team's historical performance metrics. These probabilities are displayed as
            percentages on the Games page.
          </p>
        </section>

        <section className="glass rounded-xl p-6 space-y-3">
          <h2 className="font-display text-lg font-bold tracking-tight">What is log loss?</h2>
          <p className="text-surface-600 dark:text-surface-400 leading-relaxed">
            Log loss (logarithmic loss) measures how well the model's predicted probabilities match
            actual outcomes. A lower log loss indicates better calibrated predictions. A perfect
            prediction (100% confidence, correct outcome) has a log loss of{' '}
            <span className="stat-number text-accent-500">0</span>. Random guessing (50/50) gives a
            log loss of about{' '}
            <span className="stat-number text-surface-900 dark:text-white">0.693</span>.
          </p>
        </section>

        <section className="glass rounded-xl p-6 space-y-3">
          <h2 className="font-display text-lg font-bold tracking-tight">Color coding</h2>
          <ul className="text-surface-600 dark:text-surface-400 space-y-3">
            <li className="flex items-center gap-3">
              <span className="w-3 h-3 rounded-sm bg-blue-500 shadow-lg shadow-blue-500/30" />
              <span>Model correctly predicted the winner</span>
            </li>
            <li className="flex items-center gap-3">
              <span className="w-3 h-3 rounded-sm bg-red-500 shadow-lg shadow-red-500/30" />
              <span>Model predicted incorrectly</span>
            </li>
            <li className="flex items-center gap-3">
              <span className="w-3 h-3 rounded-sm bg-surface-400 dark:bg-surface-500 shadow-lg shadow-surface-400/20" />
              <span>Game has not been played yet</span>
            </li>
          </ul>
        </section>

        <section className="glass rounded-xl p-6 space-y-3">
          <h2 className="font-display text-lg font-bold tracking-tight">Data source</h2>
          <p className="text-surface-600 dark:text-surface-400 leading-relaxed">
            All game data is collected from the NHL's public API. Data is available from the 2009-10
            season onwards (the first season with modern play-by-play statistics).
          </p>
        </section>

        <section className="glass rounded-xl p-6 space-y-3">
          <h2 className="font-display text-lg font-bold tracking-tight">
            Using MCP tools with Claude
          </h2>
          <p className="text-surface-600 dark:text-surface-400 leading-relaxed">
            This API exposes an{' '}
            <a
              href="https://modelcontextprotocol.io"
              target="_blank"
              rel="noopener noreferrer"
              className="text-accent-500 hover:underline"
            >
              MCP (Model Context Protocol)
            </a>{' '}
            server that lets AI agents like Claude query NHL game data directly. To connect, add the
            following to your Claude Desktop config:
          </p>
          <pre className="bg-surface-100 dark:bg-surface-800 rounded-lg p-4 text-sm font-mono text-surface-700 dark:text-surface-300 overflow-x-auto whitespace-pre">
            {`{
  "mcpServers": {
    "nhl-odds": {
      "url": "https://odds.nhl-wager.com/mcp"
    }
  }
}`}
          </pre>
          <p className="text-surface-600 dark:text-surface-400 text-sm">
            Once connected, Claude can use the following tools:
          </p>
          <ul className="text-surface-600 dark:text-surface-400 text-sm space-y-2">
            <li>
              <span className="font-mono text-accent-500">GetTodaysGames</span> — today's matchups
              with model odds and bookmaker lines
            </li>
            <li>
              <span className="font-mono text-accent-500">GetGamesInDateRange</span> — game odds and
              predictions for any date range
            </li>
            <li>
              <span className="font-mono text-accent-500">GetAllTeams</span> — all team stats,
              win/loss records, and model accuracy for a season
            </li>
            <li>
              <span className="font-mono text-accent-500">GetTeamStats</span> — detailed stats and
              game history for a specific team
            </li>
            <li>
              <span className="font-mono text-accent-500">GetHealthChecks</span> — data completeness
              status per season
            </li>
          </ul>
        </section>
      </div>
    </div>
  );
}
