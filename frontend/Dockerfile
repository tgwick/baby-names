# ============================================
# Stage 1: Build
# ============================================
FROM node:22-alpine AS build
WORKDIR /app

# Copy package files for layer caching
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Build production bundle
RUN npm run build

# ============================================
# Stage 2: Production
# ============================================
FROM nginx:alpine AS runtime

# Install envsubst for runtime config
RUN apk add --no-cache gettext

# Copy custom nginx configuration template
COPY nginx.conf /etc/nginx/nginx.conf.template

# Copy built assets
COPY --from=build /app/dist /usr/share/nginx/html

# Create startup script that substitutes environment variables
RUN echo '#!/bin/sh' > /docker-entrypoint.sh && \
    echo 'envsubst "\$API_URL" < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf' >> /docker-entrypoint.sh && \
    echo 'exec nginx -g "daemon off;"' >> /docker-entrypoint.sh && \
    chmod +x /docker-entrypoint.sh

# Create non-root user and set permissions
RUN adduser -D -u 1000 nginxuser && \
    chown -R nginxuser:nginxuser /usr/share/nginx/html && \
    chown -R nginxuser:nginxuser /var/cache/nginx && \
    chown -R nginxuser:nginxuser /var/log/nginx && \
    touch /var/run/nginx.pid && \
    chown nginxuser:nginxuser /var/run/nginx.pid && \
    mkdir -p /tmp/client_temp /tmp/proxy_temp /tmp/fastcgi_temp /tmp/uwsgi_temp /tmp/scgi_temp && \
    chown -R nginxuser:nginxuser /tmp && \
    chown nginxuser:nginxuser /etc/nginx/nginx.conf.template

USER nginxuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

EXPOSE 8080

# Default API URL (override with environment variable)
ENV API_URL=http://localhost:5001

ENTRYPOINT ["/docker-entrypoint.sh"]
