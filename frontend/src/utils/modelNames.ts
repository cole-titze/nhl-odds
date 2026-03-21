// Model name IDs from the GameOdds table.
// Keep in sync with game_predictor/config.py
const MODEL_NAMES: Record<number, string> = {
  1: 'In-House Model',
};

export function getModelName(modelId: number): string {
  return MODEL_NAMES[modelId] ?? `Model ${modelId}`;
}
