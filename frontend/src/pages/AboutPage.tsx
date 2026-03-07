export function AboutPage() {
  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <h1 className="text-2xl font-bold">About NHL Odds</h1>

      <section className="space-y-2">
        <h2 className="text-lg font-semibold">What is this?</h2>
        <p className="text-gray-700 dark:text-gray-300">
          NHL Odds is a machine learning project that predicts the outcome of NHL games. The model
          is trained on historical game data from the 2009 season onwards, using play-by-play
          statistics including shots, goals, hits, penalties, and more.
        </p>
      </section>

      <section className="space-y-2">
        <h2 className="text-lg font-semibold">How are predictions made?</h2>
        <p className="text-gray-700 dark:text-gray-300">
          Before each game, the model calculates the probability that the home team will win based
          on each team's historical performance metrics. These probabilities are displayed as
          percentages on the Games page.
        </p>
      </section>

      <section className="space-y-2">
        <h2 className="text-lg font-semibold">What is log loss?</h2>
        <p className="text-gray-700 dark:text-gray-300">
          Log loss (logarithmic loss) measures how well the model's predicted probabilities match
          actual outcomes. A lower log loss indicates better calibrated predictions. A perfect
          prediction (100% confidence, correct outcome) has a log loss of 0. Random guessing (50/50)
          gives a log loss of about 0.693.
        </p>
      </section>

      <section className="space-y-2">
        <h2 className="text-lg font-semibold">Color coding</h2>
        <ul className="text-gray-700 dark:text-gray-300 space-y-1">
          <li>
            <span className="inline-block w-3 h-3 rounded bg-green-500 mr-2 align-middle" />
            Green: Model correctly predicted the winner
          </li>
          <li>
            <span className="inline-block w-3 h-3 rounded bg-red-500 mr-2 align-middle" />
            Red: Model predicted incorrectly
          </li>
          <li>
            <span className="inline-block w-3 h-3 rounded bg-blue-400 mr-2 align-middle" />
            Blue: Game has not been played yet
          </li>
        </ul>
      </section>

      <section className="space-y-2">
        <h2 className="text-lg font-semibold">Data source</h2>
        <p className="text-gray-700 dark:text-gray-300">
          All game data is collected from the NHL's public API. Data is available from the 2009-10
          season onwards (the first season with modern play-by-play statistics).
        </p>
      </section>
    </div>
  );
}
