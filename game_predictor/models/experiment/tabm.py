from .types import ModelConfig


def tabm(
    tabm_k: int | None = None,
    n_blocks: int | None = None,
    d_block: int | None = None,
    dropout: float | None = None,
    lr: float | None = None,
    weight_decay: float | None = None,
    patience: int | None = None,
    batch_size: int | None = None,
) -> ModelConfig:
    """Create a TabM (parameter-efficient MLP ensemble) model config.

    Unset args fall back to pytabkit's TabM_D defaults. Needs pytabkit
    (game_predictor/requirements-experimental.txt), imported lazily so the production image
    doesn't require PyTorch. TabM early-stops on an internal 20% validation split and its
    fit() has no sample_weight, so experiment decay is ignored.
    """
    from pytabkit import TabM_D_Classifier

    overrides = {
        "tabm_k": tabm_k,
        "n_blocks": n_blocks,
        "d_block": d_block,
        "dropout": dropout,
        "lr": lr,
        "weight_decay": weight_decay,
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
