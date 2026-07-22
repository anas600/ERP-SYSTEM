# DEC-062: CI/CD Pipeline to HF Space

**Date**: 2026-07-22
**Status**: P1 implementation complete, awaiting first E2E deploy
**Defense Layers**: DL-144 to DL-147

## Overview

CI/CD pipeline that automatically builds the ERP-SYSTEM Docker image and deploys it to Hugging Face Space on every push to `develop` branch.

## Architecture

```
PR merged to develop
       ↓
GitHub Action triggers (.github/workflows/build-and-deploy-hf.yml)
       ↓
[1] Checkout + setup (.NET 9, Node 20)
       ↓
[2] Docker buildx build (with layer cache)
       ↓
[3] Verify image size (< 2GB)
       ↓
[4] Clone HF Space repo
       ↓
[5] Copy Dockerfile + metadata
       ↓
[6] Git commit + push to HF Space main branch
       ↓
[7] HF auto-rebuilds (Docker SDK)
       ↓
[8] Health check: /api/health/ready
       ↓
[9] Telegram notification (success/failure)
```

## Files

| File | Purpose |
|---|---|
| `Dockerfile` | Multi-stage: backend (.NET 9) + frontend (Next.js 14) + Caddy |
| `.dockerignore` | Excludes from build context (git, bin/obj, node_modules, .git) |
| `.github/workflows/build-and-deploy-hf.yml` | GitHub Action (NEW) |

## Required GitHub Secrets

| Secret | Value | Source |
|---|---|---|
| `HF_TOKEN` | `hf_...` | DEC-055 (already exists) |
| `HF_USERNAME` | `Anas-Assaket` | Needs to be added |
| `HF_SPACE_NAME` | `erp-system` | Needs to be added |
| `TG_BOT_TOKEN` | (optional) Telegram | For notifications |
| `TG_CHAT_ID` | (optional) Telegram | For notifications |

## HF Space Configuration

| Setting | Value |
|---|---|
| **SDK** | Docker |
| **Hardware** | CPU basic (free tier) |
| **Visibility** | Public |
| **URL** | https://Anas-Assaket-erp-system.hf.space |
| **Port** | 7860 (HF standard) |

## Triggers

- **Push to develop** → auto-deploy
- **Manual** (`workflow_dispatch`) → on-demand deploy

## Pipeline Stages (Detailed)

### Stage 1: Build Environment
- `actions/checkout@v4` (depth 1 for speed)
- `actions/setup-dotnet@v4` (9.0.x)
- `actions/setup-node@v4` (20 with npm cache)
- `actions/cache@v4` (Docker layer cache)

### Stage 2: Docker Build
- `docker/buildx` with layer caching
- Multi-stage: backend → frontend → runtime
- Image size check (warn if > 2GB)

### Stage 3: Deploy to HF
- Clone HF Space repo (depth 1)
- Copy `Dockerfile` + `README.md`
- Generate `app.json` (Docker SDK metadata)
- Git commit + push to `main` branch
- HF auto-rebuilds on push

### Stage 4: Health Check + Notify
- Wait 60s for HF rebuild
- Ping `/api/health/ready` up to 5 times
- Telegram notify on success/failure

## Existing Workflows (No Changes)

| Workflow | Purpose |
|---|---|
| `ci-fast.yml` | Tests only (fast feedback on PR) |
| `ci-deploy.yml` | Full pipeline: tests + HF sync + health + auto-rollback |
| `nightly-backup.yml` | DEC-051 nightly backups |
| `retention-cleanup.yml` | DEC-052 nightly cleanup |
| `secrets-scan.yml` | TruffleHog secret scanning |
| `codeql.yml` | CodeQL security analysis |

## Comparison: ci-deploy.yml vs build-and-deploy-hf.yml

| Feature | ci-deploy.yml | build-and-deploy-hf.yml |
|---|---|---|
| Runs tests | ✅ | ❌ (faster) |
| Build Docker | ❌ (git sync) | ✅ |
| Health check + rollback | ✅ | ❌ (manual) |
| HF SDK | git push to space | git push to space |
| Trigger | develop | develop |
| Speed | ~10 min | ~5 min |

**When to use which**:
- `ci-deploy.yml`: full pipeline with safety nets
- `build-and-deploy-hf.yml`: when you want faster deploys and trust the tests in ci-fast.yml

For DEC-062 P1, we add the new workflow for cleaner separation. Both can coexist.

## Manual Deploy

```bash
# From GitHub UI:
# Actions → "Build and Deploy to HF" → "Run workflow" → "Run"

# Or via GitHub CLI:
gh workflow run build-and-deploy-hf.yml --ref develop
```

## Test Plan

1. **First deploy**: Trigger via `workflow_dispatch`
2. **Verify**: HF Space updates
3. **Health check**: `/api/health/ready` returns 200
4. **Auto-deploy test**: Push a small change to develop, verify auto-deploy

## Performance

| Stage | Time |
|---|---|
| Checkout + setup | ~30s |
| Docker build (with cache) | ~2-4 min |
| HF clone + commit | ~30s |
| HF rebuild | ~3-5 min |
| **Total** | **~6-9 min** |

## Defense Layers

- **DL-144**: `.dockerignore` (faster builds, smaller context)
- **DL-145**: `build-and-deploy-hf.yml` workflow
- **DL-146**: Image size check
- **DL-147**: Health check post-deploy

## Open Items (P2)

- Health check with auto-rollback (currently in ci-deploy.yml)
- Slack notifications (currently Telegram)
- Multi-arch builds (linux/amd64 + linux/arm64)
- Dependency caching for NuGet
