#!/bin/bash
# start.sh — Quick start for ERP-SYSTEM local Docker
set -e

echo "🚀 ERP-SYSTEM — Local Docker"
echo ""

if ! command -v docker &> /dev/null; then
    echo "❌ Docker not found. Install: https://www.docker.com/products/docker-desktop/"
    exit 1
fi

if ! docker info &> /dev/null; then
    echo "❌ Docker daemon not running."
    exit 1
fi
echo "✅ Docker OK"
echo ""

cd "$(dirname "$0")"
docker compose up -d

sleep 10
echo ""
echo "✅ Services started"
echo ""
echo "🌐 Access:"
echo "  - Frontend:  http://localhost:3000"
echo "  - Backend:   http://localhost:5000"
echo "  - Health:    http://localhost:5000/api/health/ready"
echo "  - Swagger:   http://localhost:5000/swagger"
echo "  - DB:        localhost:5432 (erp / erp_local_password)"
echo ""
