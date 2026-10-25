FROM node:24-alpine AS build
WORKDIR /workspace/Admin
ENV NEXT_TELEMETRY_DISABLED=1
COPY package.json package-lock.json ./
COPY apps/api/package.json apps/api/package.json
COPY apps/web/package.json apps/web/package.json
COPY packages/contracts/package.json packages/contracts/package.json
RUN npm ci --no-audit --no-fund
COPY . .
ENV API_INTERNAL_URL=http://api:4000
RUN npm run build

FROM node:24-alpine AS api
WORKDIR /workspace/Admin
ENV NODE_ENV=production API_HOST=0.0.0.0 ADMIN_ROOT=/workspace/Admin
COPY --from=build --chown=node:node /workspace/Admin/node_modules ./node_modules
COPY --from=build --chown=node:node /workspace/Admin/apps/api ./apps/api
COPY --from=build --chown=node:node /workspace/Admin/packages/contracts ./packages/contracts
USER node
EXPOSE 4000
CMD ["node", "apps/api/dist/main.js"]

FROM node:24-alpine AS web
WORKDIR /workspace/Admin
ENV NODE_ENV=production HOSTNAME=0.0.0.0 PORT=3000 NEXT_TELEMETRY_DISABLED=1
COPY --from=build --chown=node:node /workspace/Admin/apps/web/.next/standalone ./
COPY --from=build --chown=node:node /workspace/Admin/apps/web/.next/static ./apps/web/.next/static
COPY --from=build --chown=node:node /workspace/Admin/apps/web/public ./apps/web/public
USER node
EXPOSE 3000
CMD ["node", "apps/web/server.js"]
