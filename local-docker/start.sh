#!/bin/bash
# start.sh — Quick start script for ERP-SYSTEM local Docker
# Usage: ./start.sh

set -e

echo "🚀 ERP-SYSTEM — Local Docker Setup"
echo ""
echo "1. Checking Docker..."
if ! command -v docker &> /dev/null; then
    echo "❌ Docker not found. Install: https://www.docker.com/products/docker-desktop/"
    exit 1
fi

if ! docker info &> /dev/null; then
    echo "❌ Docker daemon not running. Start Docker Desktop."
    exit 1
fi
echo "✅ Docker OK"
echo ""

echo "2. Starting services..."
cd "$(dirname "$0")"
docker compose up -d

echo ""
echo "3. Waiting for services to be ready..."
sleep 10

echo ""
echo "✅ Done!"
echo ""
echo "🌐 Access points:"
echo "  - Frontend:  http://localhost:3000"
echo "  - Backend:   http://localhost:5000"
echo "  - Health:    http://localhost:5000/api/health/ready"
echo "  - Swagger:   http://localhost:5000/swagger"
echo "  - Database:  localhost:5432 (user: erp / pass: erp_local_password)"
echo ""
echo "📋 Useful commands:"
echo "  - View logs:    docker compose logs -f"
echo "  - Stop:         docker compose stop"
echo "  - Reset:        docker compose down -v && docker compose up"
echo ""
