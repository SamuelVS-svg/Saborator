# ── Etapa 1: Compilar Frontend React (Vite + Three.js + Tailwind v4) ─────────
FROM node:22-alpine AS frontend-build
WORKDIR /src/frontend
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npx vite build --base=/smash-lab/

# ── Etapa 2: Compilar Backend ASP.NET Core MVC (.NET 10.0) ───────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src
COPY ControlClientesRestaurante/ControlClientesRestaurante.csproj ControlClientesRestaurante/
RUN dotnet restore ControlClientesRestaurante/ControlClientesRestaurante.csproj

COPY ControlClientesRestaurante/ ControlClientesRestaurante/
RUN dotnet publish ControlClientesRestaurante/ControlClientesRestaurante.csproj -c Release -o /app/publish /p:UseAppHost=false

# Integrar el build de React dentro de wwwroot/smash-lab
COPY --from=frontend-build /src/frontend/dist /app/publish/wwwroot/smash-lab

# ── Etapa 3: Imagen final de ejecución (Runtime ligero) ──────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Crear carpeta para subida de avatares
RUN mkdir -p /app/wwwroot/uploads/avatars

COPY --from=backend-build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=""
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ControlClientesRestaurante.dll"]
