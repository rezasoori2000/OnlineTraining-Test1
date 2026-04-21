# ============================================================
#  Admin Frontend — Multi-stage Dockerfile
#  Build context: ./frontend
# ============================================================

# ── Stage 1: Build React app ──────────────────────────────────
FROM node:20-alpine AS build
WORKDIR /app

# Install dependencies (cached layer — only re-runs when package.json changes)
COPY package*.json ./
RUN npm ci

# Copy source and build
# VITE_API_BASE_URL defaults to /api so Nginx proxies to admin-api
# Override at build time: docker build --build-arg VITE_API_BASE_URL=https://api.yourdomain.com/api
ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

COPY . .
RUN npm run build

# ── Stage 2: Serve with Nginx ──────────────────────────────────
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
