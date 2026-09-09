# syntax=docker/dockerfile:1
#
# VITE_API_BASE_URL is a build-time value (Vite bakes it into the static bundle — there is no
# runtime env substitution here), so it's supplied via --build-arg / docker-compose.yml's
# build.args, not baked in as a Dockerfile default pointing at a real environment.

FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

ARG VITE_API_BASE_URL=http://localhost:8080/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
