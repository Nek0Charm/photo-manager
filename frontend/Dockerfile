# syntax=docker/dockerfile:1

ARG NODE_BASE_IMAGE=docker.io/library/node:20-alpine
ARG NGINX_BASE_IMAGE=docker.io/library/nginx:1.27-alpine

FROM ${NODE_BASE_IMAGE} AS build
WORKDIR /app

ENV NODE_ENV=production

RUN apk add --no-cache libc6-compat
RUN corepack enable

COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY . .
RUN pnpm run build

FROM ${NGINX_BASE_IMAGE} AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
