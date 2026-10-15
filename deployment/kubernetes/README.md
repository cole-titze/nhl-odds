# Kubernetes Deployment

For deploying to a Kubernetes cluster, see the [ansible-setup](https://github.com/cole-titze/ansible-setup) repo. The playbook at `raspberry-pi-playbooks/cluster/kubernetes/nhl-odds/nhl-odds.yml` deploys the full stack to a K3s cluster, including:

- PostgreSQL database managed by CloudNativePG
- Web API and frontend (with Traefik ingress at `nhl-odds.kubecluster`)
- CronJobs replacing the scheduler container (NHL data collection at 3 AM, odds fetch + prediction at 6 AM CT)

## PostgreSQL: CloudNativePG Operator

PostgreSQL is managed by [CloudNativePG (CNPG)](https://cloudnative-pg.io/), a CNCF-backed Kubernetes-native operator. It replaces the custom `database` container used in Docker Compose with a proper `Cluster` resource:

- Uses `ghcr.io/cloudnative-pg/postgresql:18` directly — no custom Dockerfile needed
- Schema is initialized via `postInitApplicationSQLRefs`, pulling `CreateTables.sql` from the nhl-odds repo at deploy time
- CNPG automatically creates a `nhl-odds-database-rw` service pointing to the primary instance
- The operator is installed cluster-wide by the Ansible playbook before applying manifests

> **Note:** The `ghcr.io/cole-titze/nhl-odds/database` container image is used only by Docker Compose (dev and prod). It is not deployed to Kubernetes.

The following variables must be set in `~/source/ansible-files/vars/nhl_vars.yml` (loaded by the playbook) before running:

| Variable | Description |
|---|---|
| `nhl_odds_postgres_password` | Database password (CNPG) |
| `nhl_odds_api_key` | The Odds API key for daily odds fetching |
| `nhl_odds_api_backfill_key` | The Odds API key for historical odds backfill |
| `nhl_odds_cloudflare_tunnel_token` | Cloudflare tunnel token for external access |
| `nhl_odds_admin_api_key` | Key required in the `X-Admin-Key` header by every `/api/Admin` endpoint (e.g. `openssl rand -hex 32`). Enter it on the `/admin` page to unlock it |

> **Note:** Jobs run on CronJob schedules. On-demand runs go through the scheduler: `POST /api/Admin/Start<Job>` with the `X-Admin-Key` header marks the job `requested` in `JobStatus`, and `scheduler/k8s/poll-requests.sh` creates a Kubernetes Job for it within ~60 seconds (or use the buttons on the `/admin` page once it's unlocked with the key).

> **Load tests:** the `/admin` page's Light, Heavy and Stress load tests (`load-test-light`, `load-test-heavy`, `load-test-stress`) run `scheduler/k8s/load-test.js` with [k6](https://k6.io) in a Job on node01. It browses the frontend service with one `CF-Connecting-IP` per virtual user, so the API's per-IP rate limit treats each one as a separate visitor. `scheduler/k8s/save-load-test.sh` then saves the inputs and results to `LoadTestRun`. Only one runs at a time, and none while another job is running. A weekly CronJob (`nhl-odds-weekly-load-test`, Sunday 12:30am CT) requests a Light run with `scheduler/k8s/request-load-test.sh`; a Light run that fails, has failed requests, or has a page whose p95 is over 1.5× its recent median writes to `ErrorLog` with Source `LoadTest`.

> **Uptime:** the ansible-setup Prometheus playbook runs a blackbox exporter that probes `https://nhl.half-built.com/` every 60 seconds. The `nhl-odds-uptime-rollup` CronJob (every 15 minutes) runs `scheduler/k8s/save-uptime.sh`, which totals the probes for each Central Time day in Prometheus's 15-day retention and saves them to `UptimeDay`, so history lasts past that. The `/admin` page's Uptime chart shows the last 30 days by default (90 days or a year on request). Time with no probes, e.g. while the cluster was down, shows as no data, not as up.
