# ERP-SYSTEM Frontend Dockerfile (Sprint 13)
# Multi-stage: deps → build → production runner
# For MVP container (Layer 2 of 3-Layer Model per Anas 2026-07-31 directive)
# NOT for dev — dev uses the volume mount in local-docker/docker-compose.yml

# ---- Stage 1: deps (cache layer) ----
FROM node:20-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm ci --prefer-offline --no-audit --no-fund

# ---- Stage 2: build ----
FROM node:20-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .

# Sprint 14 fix: NEXT_PUBLIC_API_URL must be set at BUILD time
# (Next.js inlines process.env.NEXT_PUBLIC_* at compile time, not runtime).
# Override with --build-arg NEXT_PUBLIC_API_URL=... in docker build.
ARG NEXT_PUBLIC_API_URL=http://localhost:5000
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL \
    NEXT_TELEMETRY_DISABLED=1

RUN npm run build

# ---- Stage 3: production runner ----
FROM node:20-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000 \
    HOSTNAME=0.0.0.0

# Create non-root user
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 --ingroup nodejs nextjs

# Copy built assets
COPY --from=build --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=build --chown=nextjs:nodejs /app/.next/static ./.next/static
COPY --from=build --chown=nextjs:nodejs /app/public ./public

USER nextjs

EXPOSE 3000

CMD ["node", "server.js"]
