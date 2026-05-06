import json
import os
from pathlib import Path

_REPO_ROOT = Path(__file__).parent.parent

# Model name IDs used in the GameOdds table.
# Keep in sync with frontend/src/utils/modelNames.ts
MODEL_IDS = {
    1: "Homegrown",
    2: "Spread",
    3: "Total",
}

HOMEGROWN_MODEL_ID = 1
SPREAD_MODEL_ID = 2
TOTAL_MODEL_ID = 3


def get_db_config() -> dict:
    conn_str = os.environ.get("NHL_DATABASE")
    if conn_str:
        return _parse_connection_string(conn_str)

    appsettings_path = _REPO_ROOT / "src" / "Entry" / "appsettings.Local.json"
    if appsettings_path.exists():
        with open(appsettings_path, encoding="utf-8-sig") as f:
            data = json.load(f)
        conn_str = data.get("ConnectionStrings", {}).get("NHL_DATABASE", "")
        if conn_str:
            return _parse_connection_string(conn_str)

    raise RuntimeError(
        "No database config found. Set NHL_DATABASE env var or add a connection string to src/Entry/appsettings.Local.json."
    )


def _parse_connection_string(conn_str: str) -> dict:
    parts = {}
    for part in conn_str.split(";"):
        part = part.strip()
        if "=" not in part:
            continue
        key, value = part.split("=", 1)
        parts[key.strip().lower()] = value.strip()

    key_map = {
        "host": ["host", "server", "data source"],
        "dbname": ["database", "dbname", "initial catalog"],
        "user": ["username", "user", "user id", "uid"],
        "password": ["password", "pwd"],
    }

    config = {}
    for config_key, possible_keys in key_map.items():
        for k in possible_keys:
            if k in parts:
                config[config_key] = parts[k]
                break

    return config
