// Model name IDs from the GameOdds table.
// Keep in sync with game_predictor/config.py
const MODEL_NAMES: Record<number, string> = {
  1: 'In-House Model',
};

export function getModelName(modelId: number | null | undefined): string {
  if (modelId == null) return 'No model';
  return MODEL_NAMES[modelId] ?? `Model ${modelId}`;
}
