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
              <span className="w-3 h-3 rounded-sm bg-emerald-500 shadow-lg shadow-emerald-500/30" />
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
      </div>
    </div>
  );
}
