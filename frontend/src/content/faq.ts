// The About page's FAQ (#167).
// Shown on the page and sent to search engines as FAQPage markup (one list, so
// they always match). Plain text: the markup can't carry links.
export const FAQ: { question: string; answer: string }[] = [
  {
    question: 'What is NHL Odds?',
    answer:
      'A free hobby site that predicts every NHL game with a machine learning model and compares its win probabilities with sportsbook and Kalshi odds. It is not affiliated with the NHL or any team.',
  },
  {
    question: 'How does the model predict NHL games?',
    answer:
      "Before each game it estimates the home team's chance of winning from both teams' form and history: season and recent results, goals, shots and expected goals, special teams, goaltending, rest and travel, Elo ratings and more, built from NHL play-by-play data since the 2009-10 season.",
  },
  {
    question: 'How often are predictions updated?',
    answer:
      'Every day. New game data is collected overnight, and the model makes fresh predictions for upcoming games each morning. Bookmaker odds are fetched each morning and again just before puck drop.',
  },
  {
    question: 'How accurate is the model?',
    answer:
      "The Teams page shows the model's accuracy and log loss for the season, overall and for every team, next to the same numbers for DraftKings and Kalshi. Each game's page shows how its prediction turned out. Hockey has a lot of randomness, so even a good model misses many games.",
  },
  {
    question: 'What is log loss?',
    answer:
      'Log loss measures how well predicted probabilities match what happened. Lower is better: a confident, correct prediction scores near 0, and always guessing 50/50 scores about 0.693. Unlike accuracy, it rewards being right about how likely something was, not just who won.',
  },
  {
    question: 'Where do the bookmaker odds come from?',
    answer:
      'Sportsbook odds such as DraftKings come from The Odds API, and prediction market prices from Kalshi. They are shown for information only.',
  },
  {
    question: 'What does removing the bookmaker margin mean?',
    answer:
      "A bookmaker's prices include a margin, so the two teams' implied probabilities add up to more than 100%. Removing it scales them to add up to 100%, which gives the market's own estimate to compare with the model's.",
  },
  {
    question: 'Is this betting advice?',
    answer:
      "No. The predictions, odds and backtested strategies are data shared for interest. Past results don't guarantee future ones.",
  },
  {
    question: 'Can I use this data with my own AI assistant?',
    answer:
      'Yes. The site runs an MCP server that AI assistants such as Claude can connect to for game odds, predictions and team stats. The setup is at the bottom of this page.',
  },
];
