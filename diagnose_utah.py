"""Diagnostic script to investigate Utah team ID mismatch."""

from game_predictor.config import get_db_config
from game_predictor.db.connection import get_connection


def run_query(conn, label, sql):
    print(f"\n{'=' * 60}")
    print(f"  {label}")
    print(f"{'=' * 60}")
    with conn.cursor() as cur:
        cur.execute(sql)
        cols = [d[0] for d in cur.description]
        rows = cur.fetchall()
    if not rows:
        print("  (no results)")
        return
    widths = [max(len(str(c)), max(len(str(r[i])) for r in rows)) for i, c in enumerate(cols)]
    header = "  ".join(str(c).ljust(w) for c, w in zip(cols, widths))
    print(f"  {header}")
    print(f"  {'-' * len(header)}")
    for row in rows:
        print(f"  {'  '.join(str(v).ljust(w) for v, w in zip(row, widths))}")


def main():
    config = get_db_config()
    conn = get_connection(config)

    run_query(conn, "Utah entries in SeasonTeam", """
        SELECT TeamId, SeasonStartYear, Name, Abbreviation, PlaceName
        FROM SeasonTeam
        WHERE Name LIKE '%Utah%' OR PlaceName LIKE '%Utah%' OR Abbreviation = 'UTA'
        ORDER BY SeasonStartYear
    """)

    run_query(conn, "Utah/Arizona entries in Team table", """
        SELECT Id, Abbreviation, FranchiseId
        FROM Team
        WHERE Abbreviation IN ('UTA', 'ARI', 'PHX')
    """)

    run_query(conn, "Current season TeamIds in GameRaw not in SeasonTeam", """
        SELECT DISTINCT gr.HomeTeamId AS MissingTeamId
        FROM GameRaw gr
        WHERE gr.SeasonStartYear = (SELECT MAX(SeasonStartYear) FROM GameRaw)
          AND gr.HomeTeamId NOT IN (
              SELECT TeamId FROM SeasonTeam
              WHERE SeasonStartYear = (SELECT MAX(SeasonStartYear) FROM GameRaw)
          )
        UNION
        SELECT DISTINCT gr.AwayTeamId
        FROM GameRaw gr
        WHERE gr.SeasonStartYear = (SELECT MAX(SeasonStartYear) FROM GameRaw)
          AND gr.AwayTeamId NOT IN (
              SELECT TeamId FROM SeasonTeam
              WHERE SeasonStartYear = (SELECT MAX(SeasonStartYear) FROM GameRaw)
          )
    """)

    run_query(conn, "Current season games involving Utah-related team IDs", """
        SELECT TOP 5 gr.Id, gr.HomeTeamId, gr.AwayTeamId, gr.GameDateUTC
        FROM GameRaw gr
        WHERE gr.SeasonStartYear = (SELECT MAX(SeasonStartYear) FROM GameRaw)
          AND (gr.HomeTeamId IN (SELECT Id FROM Team WHERE Abbreviation IN ('UTA', 'ARI'))
               OR gr.AwayTeamId IN (SELECT Id FROM Team WHERE Abbreviation IN ('UTA', 'ARI')))
        ORDER BY gr.GameDateUTC DESC
    """)

    run_query(conn, "GameOdds count per SeasonTeam for current season", """
        WITH CurrentSeason AS (SELECT MAX(SeasonStartYear) AS yr FROM GameRaw)
        SELECT st.TeamId, st.Name,
               (SELECT COUNT(*) FROM GameOdds go2
                JOIN GameRaw gr ON go2.GameId = gr.Id
                WHERE gr.SeasonStartYear = cs.yr
                  AND (gr.HomeTeamId = st.TeamId OR gr.AwayTeamId = st.TeamId)) AS GameOddsCount
        FROM SeasonTeam st
        CROSS JOIN CurrentSeason cs
        WHERE st.SeasonStartYear = cs.yr
          AND st.Name LIKE '%Utah%'
    """)

    conn.close()


if __name__ == "__main__":
    main()
