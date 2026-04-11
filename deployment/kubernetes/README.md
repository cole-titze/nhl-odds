# Kubernetes Deployment

For deploying to a Kubernetes cluster, see the [ansible-setup](https://github.com/cole-titze/ansible-setup) repo. The playbook at `raspberry-pi-playbooks/cluster/kubernetes/nhl-odds/nhl-odds.yml` deploys the full stack to a K3s cluster, including:

- PostgreSQL database managed by CloudNativePG
- Web API and frontend (with Traefik ingress at `nhl-odds.kubecluster`)
- CronJobs replacing the scheduler container (NHL data collection at 3 AM, odds fetch + prediction at 6 AM CT)

## PostgreSQL: CloudNativePG Operator

PostgreSQL is managed by [CloudNativePG (CNPG)](https://cloudnative-pg.io/), a CNCF-backed Kubernetes-native operator. It replaces the custom `database` container used in Docker Compose with a proper `Cluster` resource:

- Uses `ghcr.io/cloudnative-pg/postgresql:17` directly — no custom Dockerfile needed
- Schema is initialized via `postInitApplicationSQLRefs`, pulling `CreateTables.sql` from the nhl-odds repo at deploy time
- CNPG automatically creates a `nhl-odds-database-rw` service pointing to the primary instance
- The operator is installed cluster-wide by the Ansible playbook before applying manifests

> **Note:** The `ghcr.io/cole-titze/nhl-odds/database` container image is used only by Docker Compose (dev and prod). It is not deployed to Kubernetes.

The following variables must be set in `group_vars` or ansible-vault before running:

| Variable | Description |
|---|---|
| `nhl_odds_postgres_password` | Database password for the postgres user |
| `nhl_odds_odds_api_key` | The Odds API key for daily odds fetching |
| `nhl_odds_api_backfill_key` | The Odds API key for historical odds backfill |

> **Note:** The on-demand job dispatch (Admin page buttons) is not supported in the Kubernetes deployment — jobs run on their cron schedules only.
