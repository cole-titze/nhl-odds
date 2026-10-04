# nhl-odds

![Unit Tests](https://github.com/cole-titze/nhl-odds/actions/workflows/unit-tests.yml/badge.svg)
![Format Check](https://github.com/cole-titze/nhl-odds/actions/workflows/format-check.yml/badge.svg)
![Accessibility](https://github.com/cole-titze/nhl-odds/actions/workflows/accessibility.yml/badge.svg)
![Database Container](https://github.com/cole-titze/nhl-odds/actions/workflows/docker-build.yml/badge.svg)
![Web API Container](https://github.com/cole-titze/nhl-odds/actions/workflows/webapi-build.yml/badge.svg)
![Frontend Container](https://github.com/cole-titze/nhl-odds/actions/workflows/frontend-build.yml/badge.svg)
![Entry Container](https://github.com/cole-titze/nhl-odds/actions/workflows/entry-build.yml/badge.svg)
![Predictor Container](https://github.com/cole-titze/nhl-odds/actions/workflows/predictor-build.yml/badge.svg)
![Scheduler Container](https://github.com/cole-titze/nhl-odds/actions/workflows/scheduler-build.yml/badge.svg)
![Security Scan](https://github.com/cole-titze/nhl-odds/actions/workflows/security-scan.yml/badge.svg)

The nhl project. This repo collects nhl data from the nhl api, cleans it, and then runs machine learning models on the data to predict outcomes. There is also a full-stack web app to access the information.

# Deployment

| Target | Guide |
|---|---|
| Local development | [deployment/local](deployment/local/README.md) |
| Docker (production) | [deployment/docker](deployment/docker/README.md) |
| Kubernetes | [deployment/kubernetes](deployment/kubernetes/README.md) |

# Updates

Every container is rebuilt weekly (Sundays 08:00 UTC) to pick up base-image patches, as well as on pushes to `main`. A Trivy [security scan](.github/workflows/security-scan.yml) of the published images runs two hours later (Sundays 10:00 UTC). The Kubernetes deployment then picks up new images automatically: a nightly `nightly-image-updater` CronJob (04:00 America/Chicago) rolling-restarts the `nhl-odds` deployments.

Maintenance skills live in [claude-skills](https://github.com/cole-titze/claude-skills). That repo is private, but the descriptions below give the gist of what each one does:

| Skill | Purpose |
|---|---|
| [`update-cluster`](https://github.com/cole-titze/claude-skills/tree/main/update-cluster) | Rolling-restart deployments to pull the latest images |
| [`k8s-major-upgrade`](https://github.com/cole-titze/claude-skills/tree/main/k8s-major-upgrade) | Check for and apply cluster upgrades (K3s, Helm charts, CNPG, pinned images) |
| [`nhl-odds-security`](https://github.com/cole-titze/claude-skills/tree/main/nhl-odds-security) | Assess the security scan results and audit code and CI |
| [`nhl-db-healthcheck`](https://github.com/cole-titze/claude-skills/tree/main/nhl-db-healthcheck) | Spot-check the database against the live NHL API |
| [`nhl-error-triage`](https://github.com/cole-titze/claude-skills/tree/main/nhl-error-triage) | Triage the ErrorLog table and propose fixes |
| [`nhl-new-experiment`](https://github.com/cole-titze/claude-skills/tree/main/nhl-new-experiment) | Design and evaluate a new prediction model experiment |
| [`code-format-nhl`](https://github.com/cole-titze/claude-skills/tree/main/code-format-nhl) | Run all formatters (dotnet, Prettier, ruff) before committing |
