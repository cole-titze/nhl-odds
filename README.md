# nhl-odds

![Unit Tests](https://github.com/cole-titze/nhl-odds/actions/workflows/unit-tests.yml/badge.svg)
![Format Check](https://github.com/cole-titze/nhl-odds/actions/workflows/format-check.yml/badge.svg)
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
