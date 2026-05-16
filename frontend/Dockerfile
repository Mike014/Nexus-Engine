FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json .
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 3000

# Il frontend non esiste ancora come progetto React. Questo Dockerfile e' un placeholder che compila quando il progetto sara' inizializzato. 
# Per ora ti serve solo per validare che il Docker Compose sia sintatticamente corretto.