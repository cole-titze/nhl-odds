#!/bin/sh
# Poll JobStatus table for 'requested' jobs and create a Kubernetes Job for each.

# A load test's Job writes its own result, but one that never got to (pod not scheduled, k6 killed)
# would stay 'running' and block the next one; the longest preset takes under 5 minutes
psql "$NHL_DATABASE_URL" -qAtc "UPDATE \"JobStatus\" SET \"Status\" = 'failed', \"FinishedAt\" = now() AT TIME ZONE 'UTC', \"Error\" = 'No result after 20 minutes; see the Job''s pod logs.' WHERE \"JobName\" LIKE 'load-test-%' AND \"Status\" = 'running' AND \"StartedAt\" < (now() AT TIME ZONE 'UTC') - interval '20 minutes'" >/dev/null 2>&1

REQUESTED=$(psql "$NHL_DATABASE_URL" -Atc "SELECT \"JobName\" FROM \"JobStatus\" WHERE \"Status\" = 'requested' LIMIT 1" 2>/dev/null)

[ -z "$REQUESTED" ] && exit 0

echo "$(date -u '+%Y-%m-%d %H:%M:%S') Picked up requested job: $REQUESTED"

JOB_NAME="nhl-sched-$(echo "$REQUESTED" | tr '_' '-')-$(date +%s)"

case "$REQUESTED" in
    data-collection|odds-fetch|odds-backfill|next-day-kalshi|kalshi-backfill)
        case "$REQUESTED" in
            data-collection)  RUN_MODE=NhlAdd ;;
            odds-fetch)       RUN_MODE=NextDayOdds ;;
            odds-backfill)    RUN_MODE=BackfillOdds ;;
            next-day-kalshi)  RUN_MODE=NextDayKalshi ;;
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
      securityContext:
        runAsNonRoot: true
      containers:
        - name: entry
          image: ghcr.io/cole-titze/nhl-odds/entry:latest
          imagePullPolicy: Always
          securityContext:
            allowPrivilegeEscalation: false
          env:
            - name: TZ
              value: America/Chicago
            - name: RUN_MODE
              value: $RUN_MODE
            - name: POSTGRES_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: nhl-odds-app-credentials
                  key: password
            - name: NHL_DATABASE
              value: "Host=nhl-odds-database-rw;Database=nhl;Username=nhl_app;Password=\$(POSTGRES_PASSWORD)"
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
      securityContext:
        runAsNonRoot: true
      containers:
        - name: predictor
          image: ghcr.io/cole-titze/nhl-odds/predictor:latest
          imagePullPolicy: Always
          securityContext:
            allowPrivilegeEscalation: false
          env:
            - name: TZ
              value: America/Chicago
            - name: PREDICTOR_MODE
              value: $PREDICTOR_MODE
            - name: POSTGRES_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: nhl-odds-app-credentials
                  key: password
            - name: NHL_DATABASE
              value: "Host=nhl-odds-database-rw;Database=nhl;Username=nhl_app;Password=\$(POSTGRES_PASSWORD)"
EOF
        ;;

    load-test-*)
        PRESET="${REQUESTED#load-test-}"

        # k6 can't mark itself running like the other jobs do, so claim the row here; otherwise the
        # next poll would start a second load test on top of this one
        CLAIMED=$(psql "$NHL_DATABASE_URL" -qAtc "UPDATE \"JobStatus\" SET \"Status\" = 'running', \"StartedAt\" = now() AT TIME ZONE 'UTC', \"FinishedAt\" = NULL, \"Error\" = NULL WHERE \"JobName\" = '$REQUESTED' AND \"Status\" = 'requested' RETURNING 1")
        [ "$CLAIMED" = 1 ] || exit 0

        # k6 runs on node01 (the control plane, kept free of app pods), so the load it generates
        # doesn't compete with the webapi and frontend pods on node02/node03. Its CPU is capped
        # to leave node01's cores for the Kubernetes API server
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
  backoffLimit: 0
  activeDeadlineSeconds: 900
  template:
    spec:
      restartPolicy: Never
      # node01's pod subnet (10.43.0.0/24) overlaps the service range that holds the cluster DNS
      # and API addresses, so its pods can't reach them; on the host network they can
      hostNetwork: true
      dnsPolicy: ClusterFirstWithHostNet
      nodeSelector:
        kubernetes.io/hostname: node01
      tolerations:
        - key: node-role.kubernetes.io/control-plane
          operator: Exists
        - key: node.kubernetes.io/unschedulable
          operator: Exists
          effect: NoSchedule
      securityContext:
        runAsNonRoot: true
      volumes:
        - name: out
          emptyDir: {}
      initContainers:
        # Always exits 0 so save-results runs; k6's exit code goes in /out/rc
        - name: k6
          image: grafana/k6:latest
          imagePullPolicy: Always
          securityContext:
            allowPrivilegeEscalation: false
          command: ["sh", "-c"]
          args:
            - |
              echo "\$SCRIPT" | base64 -d > /out/load-test.js && k6 run --quiet /out/load-test.js
              echo \$? > /out/rc
              exit 0
          env:
            - name: PRESET
              value: "$PRESET"
            - name: SCRIPT
              value: "$(base64 -w0 /app/load-test.js)"
          resources:
            requests:
              cpu: "500m"
              memory: "128Mi"
            limits:
              cpu: "2"
              memory: "1Gi"
          volumeMounts:
            - name: out
              mountPath: /out
      containers:
        - name: save-results
          image: ghcr.io/cole-titze/nhl-odds/scheduler-k8s:latest
          imagePullPolicy: Always
          securityContext:
            allowPrivilegeEscalation: false
          command: ["/app/save-load-test.sh", "$REQUESTED"]
          env:
            - name: POSTGRES_PASSWORD
              valueFrom:
                secretKeyRef:
                  name: nhl-odds-app-credentials
                  key: password
            - name: NHL_DATABASE_URL
              value: "postgresql://nhl_app:\$(POSTGRES_PASSWORD)@nhl-odds-database-rw:5432/nhl"
          resources:
            requests:
              cpu: "10m"
              memory: "32Mi"
            limits:
              memory: "64Mi"
          volumeMounts:
            - name: out
              mountPath: /out
EOF
        if [ $? -ne 0 ]; then
            psql "$NHL_DATABASE_URL" -qAtc "UPDATE \"JobStatus\" SET \"Status\" = 'failed', \"FinishedAt\" = now() AT TIME ZONE 'UTC', \"Error\" = 'Could not create the load test Job.' WHERE \"JobName\" = '$REQUESTED'"
            exit 1
        fi
        ;;

    *)
        echo "Unknown job: $REQUESTED"
        exit 1
        ;;
esac

echo "$(date -u '+%Y-%m-%d %H:%M:%S') Created job $JOB_NAME for $REQUESTED"
