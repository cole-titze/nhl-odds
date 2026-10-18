FROM python:3.14-slim
WORKDIR /app
RUN apt-get update && apt-get upgrade -y --no-install-recommends && apt-get install -y --no-install-recommends libgomp1 tzdata && rm -rf /var/lib/apt/lists/*
COPY requirements.txt .
# Root is fine in a throwaway build layer; silences pip's warning about it
ENV PIP_ROOT_USER_ACTION=ignore
# pip is only needed to install; its vendored packages (urllib3, msgpack, ...) are what Trivy
# flags, so remove it and ensurepip's bundled wheel afterwards
RUN pip install --no-cache-dir -r requirements.txt \
    && python -m pip uninstall -y pip \
    && rm -rf /usr/local/lib/python3*/ensurepip/_bundled
COPY . game_predictor/
RUN useradd --uid 10001 --create-home app
USER 10001
ENV PREDICTOR_MODE=predict
# Print progress as it happens, so log lines can be timed
ENV PYTHONUNBUFFERED=1
ENTRYPOINT ["sh", "-c", "python -m game_predictor --mode $PREDICTOR_MODE"]
