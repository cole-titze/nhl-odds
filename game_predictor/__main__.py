import argparse
from datetime import datetime, timezone

from .config import get_db_config
from .db.connection import get_connection
from .workflow import run

_MODE_TO_JOB = {
    "predict": "prediction",
    "backfill": "prediction-backfill",
}

_UPDATE_SQL = """
INSERT INTO "JobStatus" ("JobName", "Status", "StartedAt", "FinishedAt", "Error")
VALUES (%(name)s, %(status)s,
        CASE WHEN %(status)s = 'running' THEN %(now)s ELSE NULL END,
        CASE WHEN %(status)s != 'running' THEN %(now)s ELSE NULL END,
        %(error)s)
ON CONFLICT ("JobName") DO UPDATE SET
    "Status" = %(status)s,
    "StartedAt" = CASE WHEN %(status)s = 'running' THEN %(now)s ELSE "JobStatus"."StartedAt" END,
    "FinishedAt" = CASE WHEN %(status)s != 'running' THEN %(now)s ELSE NULL END,
    "Error" = %(error)s
"""


def _update_job_status(job_name: str | None, status: str, error: str | None = None) -> None:
    if not job_name:
        return
    try:
        conn = get_connection(get_db_config())
        conn.autocommit = True
        with conn.cursor() as cur:
            cur.execute(
                _UPDATE_SQL,
                {"name": job_name, "status": status, "now": datetime.now(timezone.utc), "error": error},
            )
        conn.close()
    except Exception:
        pass


parser = argparse.ArgumentParser(description="NHL Game Predictor")
parser.add_argument(
    "--mode",
    choices=["predict", "backfill", "test"],
    default="predict",
    help="predict: upcoming games. backfill: walk-forward all history. test: experiment with all models.",
)
args = parser.parse_args()

job_name = _MODE_TO_JOB.get(args.mode)
_update_job_status(job_name, "running")
try:
    run(mode=args.mode)
    _update_job_status(job_name, "completed")
except Exception as e:
    _update_job_status(job_name, "failed", str(e))
    raise
