# Chainguard (Wolfi) Python: glibc, so the manylinux wheels install as on Debian, but only the
# packages Python needs (no Debian util-linux/perl/systemd CVEs). Alpine/musl isn't an option:
# scikit-learn, lightgbm, xgboost and numba (via shap) ship no musl arm64 wheels.
# Build stage: the -dev image has a shell, apk and pip; the runtime image has none of them.
FROM cgr.dev/chainguard/python:latest-dev AS build
USER root
# lightgbm and xgboost load the system OpenMP runtime, and the cluster sets TZ=America/Chicago;
# the runtime image has neither, so both are copied in below
RUN apk add --no-cache libgomp tzdata
WORKDIR /app
COPY requirements.txt .
RUN python -m venv /app/venv \
    && /app/venv/bin/pip install --no-cache-dir -r requirements.txt \
    && /app/venv/bin/pip uninstall -y pip

FROM cgr.dev/chainguard/python:latest
WORKDIR /app
COPY --from=build /usr/lib/libgomp.so.1* /usr/lib/
COPY --from=build /usr/share/zoneinfo /usr/share/zoneinfo
COPY --from=build /app/venv /app/venv
COPY . game_predictor/
ENV PATH=/app/venv/bin:$PATH
ENV PREDICTOR_MODE=predict
# Print progress as it happens, so log lines can be timed
ENV PYTHONUNBUFFERED=1
# Chainguard's nonroot user, numeric so runAsNonRoot can verify it
USER 65532
ENTRYPOINT ["python", "-m", "game_predictor"]
