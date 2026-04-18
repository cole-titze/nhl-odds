#!/bin/sh
# Poll JobStatus table for 'requested' jobs and create a Kubernetes Job for each.

REQUESTED=$(psql "$NHL_DATABASE_URL" -Atc "SELECT \"JobName\" FROM \"JobStatus\" WHERE \"Status\" = 'requested' LIMIT 1" 2>/dev/null)

[ -z "$REQUESTED" ] && exit 0

echo "$(date -u '+%Y-%m-%d %H:%M:%S') Picked up requested job: $REQUESTED"

JOB_NAME="nhl-sched-$(echo "$REQUESTED" | tr '_' '-')-$(date +%s)"

case "$REQUESTED" in
    data-collection|odds-fetch|odds-backfill|kalshi-fetch|kalshi-backfill)
        case "$REQUESTED" in
            data-collection)  RUN_MODE=NhlAdd ;;
            odds-fetch)       RUN_MODE=NextDayOdds ;;
            odds-backfill)    RUN_MODE=BackfillOdds ;;
            kalshi-fetch)     RUN_MODE=KalshiFetch ;;
            kalshi-backfill)  RUN_MODE=BackfillKalshi ;;
        esac

        kubectl apply -f - <<EOF
apiVersion: batch/v1
kind: Job
metadata:
  name: $JOB_NAME
  namespace: nhl-odds
  labels:
    scheduler-job: "$REQUESTED"
spec:
  ttlSecondsAfterFinished: 3600
  template:
    spec:
      restartPolicy: OnFailure
      containers:
        - name: entry
          image: ghcr.io/cole-titze/nhl-odds/entry:latest
          imagePullPolicy: Always
          env:
            - name: TZ
              value: America/Chicago
            - name: RUN_MODE
              value: $RUN_MODE
            - name: POSTGRES_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: nhl-odds-secret
                  key: postgres-password
            - name: NHL_DATABASE
              value: "Host=nhl-odds-database-rw;Database=nhl;Username=postgres;Password=\$(POSTGRES_PASSWORD)"
            - name: ODDS_API_KEY
              valueFrom:
                secretKeyRef:
                  name: nhl-odds-secret
                  key: odds-api-key
            - name: API_BACKFILL_KEY
              valueFrom:
                secretKeyRef:
                  name: nhl-odds-secret
                  key: api-backfill-key
EOF
        ;;

    prediction|prediction-backfill)
        case "$REQUESTED" in
            prediction)          PREDICTOR_MODE=predict ;;
            prediction-backfill) PREDICTOR_MODE=backfill ;;
        esac

        kubectl apply -f - <<EOF
apiVersion: batch/v1
kind: Job
metadata:
  name: $JOB_NAME
  namespace: nhl-odds
  labels:
    scheduler-job: "$REQUESTED"
spec:
  ttlSecondsAfterFinished: 3600
  template:
    spec:
      restartPolicy: OnFailure
      containers:
        - name: predictor
          image: ghcr.io/cole-titze/nhl-odds/predictor:latest
          imagePullPolicy: Always
          env:
            - name: TZ
              value: America/Chicago
            - name: PREDICTOR_MODE
              value: $PREDICTOR_MODE
            - name: POSTGRES_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: nhl-odds-secret
                  key: postgres-password
            - name: NHL_DATABASE
              value: "Host=nhl-odds-database-rw;Database=nhl;Username=postgres;Password=\$(POSTGRES_PASSWORD)"
EOF
        ;;

    *)
        echo "Unknown job: $REQUESTED"
        exit 1
        ;;
esac

echo "$(date -u '+%Y-%m-%d %H:%M:%S') Created job $JOB_NAME for $REQUESTED"
