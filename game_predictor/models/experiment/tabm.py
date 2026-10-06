from .folds import log_loss_score, mean_fold_score
from .types import ModelConfig


def tabm(
    arch_type: str | None = None,
    tabm_k: int | None = None,
    n_blocks: int | None = None,
    d_block: int | None = None,
    dropout: float | None = None,
    lr: float | None = None,
    weight_decay: float | None = None,
    num_emb_type: str | None = None,
    d_embedding: int | None = None,
    num_emb_n_bins: int | None = None,
    patience: int | None = None,
    batch_size: int | None = None,
) -> ModelConfig:
    """Create a TabM (parameter-efficient MLP ensemble) model config.

    Unset args fall back to pytabkit's TabM_D defaults. Needs pytabkit
    (requirements-experimental.txt), imported lazily so the production image doesn't require
    PyTorch. TabM early-stops on an internal 20% validation split and its fit() has no
    sample_weight, so experiment decay is ignored.
    """
    from ...libomp import FIX_COMMAND, needs_link

    if needs_link():
        # Loading torch's own OpenMP runtime would hang/segfault lightgbm and xgboost
        raise RuntimeError(f"torch isn't using Homebrew's libomp on this Mac — run `{FIX_COMMAND}` first")
    from pytabkit import TabM_D_Classifier

    overrides = {
        "arch_type": arch_type,
        "tabm_k": tabm_k,
        "n_blocks": n_blocks,
        "d_block": d_block,
        "dropout": dropout,
        "lr": lr,
        "weight_decay": weight_decay,
        "num_emb_type": num_emb_type,
        "d_embedding": d_embedding,
        "num_emb_n_bins": num_emb_n_bins,
        "patience": patience,
        "batch_size": batch_size,
    }
    return ModelConfig(
        cls=TabM_D_Classifier,
        params={
            **{k: v for k, v in overrides.items() if v is not None},
            "device": "cpu",
            "random_state": 42,
            "verbosity": 0,
        },
    )


def tune_tabm(folds, n_trials, progress_callback):
    """Search space follows pytabkit's "tabarena" TabM HPO space (from the TabM authors).

    Trials run one at a time: a single TabM fit already uses every core.
    """
    import optuna
    from pytabkit import TabM_D_Classifier

    def objective(trial):
        params = {
            "arch_type": trial.suggest_categorical("arch_type", ["tabm", "tabm-mini"]),
            "n_blocks": trial.suggest_int("n_blocks", 2, 5),
            "d_block": trial.suggest_int("d_block", 128, 1024, step=16),
            "dropout": trial.suggest_float("dropout", 0.0, 0.5),
            "lr": trial.suggest_float("lr", 1e-4, 3e-3, log=True),
            "weight_decay": trial.suggest_float("weight_decay", 1e-5, 1e-1, log=True),
            "num_emb_type": trial.suggest_categorical("num_emb_type", ["none", "pwl"]),
            "d_embedding": trial.suggest_int("d_embedding", 8, 32, step=4),
            "num_emb_n_bins": trial.suggest_int("num_emb_n_bins", 2, 128),
            "device": "cpu",
            "random_state": 42,
            "verbosity": 0,
        }
        return mean_fold_score(lambda: TabM_D_Classifier(**params), folds, log_loss_score)

    study = optuna.create_study(direction="minimize", study_name="tabm-tuning")
    study.optimize(objective, n_trials=n_trials, n_jobs=1, callbacks=[progress_callback])
    return study
