# ---- Build stage ----
FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
# VITE_API_URL is baked in at build time (Vite inlines import.meta.env.VITE_*).
# Railway passes the service variable as a build arg.
ARG VITE_API_URL
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# ---- Runtime stage ----
FROM node:22-alpine AS runtime
WORKDIR /app
RUN npm i -g serve
COPY --from=build /app/dist ./dist

# Railway injects PORT; serve the static SPA with history-API fallback.
ENV PORT=8080
CMD ["sh", "-c", "serve -s dist -l ${PORT}"]
