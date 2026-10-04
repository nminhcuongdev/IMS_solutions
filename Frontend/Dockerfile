FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY . .
ARG VITE_API_BASE=
ARG VITE_API_PORT=8091
ENV VITE_API_BASE=$VITE_API_BASE VITE_API_PORT=$VITE_API_PORT
RUN npx tsc --noEmit && npx vite build

FROM nginx:1.27-alpine AS runtime
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
