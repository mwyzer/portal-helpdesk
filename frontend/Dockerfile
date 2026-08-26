FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine
# Pull latest Alpine package patches at build time rather than trusting the base image tag's
# own rebuild cadence -- caught real CRITICAL/HIGH CVEs (libssl3, libcrypto3, libxml2, etc.)
# still present in the currently-published nginx:1.27-alpine layer as of a Trivy scan.
RUN apk update && apk upgrade --no-cache
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
