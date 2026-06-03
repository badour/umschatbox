#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="${ROOT_DIR}/dev/docker-compose.yml"
SA_PASSWORD="${MSSQL_SA_PASSWORD:-YourStrong@Passw0rd}"

cd "${ROOT_DIR}"

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker is required. Install Docker and rerun this script." >&2
  exit 1
fi

DOCKER=(docker)
if ! docker info >/dev/null 2>&1; then
  DOCKER=(sudo docker)
fi

"${DOCKER[@]}" compose -f "${COMPOSE_FILE}" up -d

echo "Waiting for SQL Server to become healthy..."
for _ in $(seq 1 60); do
  if "${DOCKER[@]}" inspect --format='{{.State.Health.Status}}' umschatbox-sql 2>/dev/null | grep -q healthy; then
    break
  fi
  sleep 2
done

INIT_DIR="/tmp/umschatbox-sql-init"
"${DOCKER[@]}" exec umschatbox-sql mkdir -p "${INIT_DIR}"
for sql_file in "${ROOT_DIR}"/dev/sql/*.sql; do
  "${DOCKER[@]}" cp "${sql_file}" "umschatbox-sql:${INIT_DIR}/$(basename "${sql_file}")"
  "${DOCKER[@]}" exec umschatbox-sql /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "${SA_PASSWORD}" -C -b \
    -i "${INIT_DIR}/$(basename "${sql_file}")"
done

echo "ExpensesDev database is ready on localhost:1433"
