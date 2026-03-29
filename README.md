# Cotizador de Seguros de Daños

Sistema de cotización de seguros para pólizas de daños (incendio, CAT, equipo electrónico, etc.).

## Componentes

| Componente | Puerto | Tech |
|------------|--------|------|
| cotizador-core-mock | 3001 | Node.js + Express + TypeScript |
| cotizador-backend | 5000 | .NET 8 ASP.NET Core |
| cotizador-webapp | 3000 | React + TypeScript (pendiente) |

## Prerequisitos

- Node.js 18+
- .NET SDK 8
- MongoDB 7 (local o Docker)
- npm o yarn

## Instalación y arranque

### 1. MongoDB (Docker)
```bash
docker run -d --name mongo-cotizador -p 27017:27017 mongo:7
```

### 2. cotizador-core-mock
```bash
cd cotizador-core-mock
npm install
npm run dev
# Verificar: GET http://localhost:3001/v1/subscribers
```

### 3. cotizador-backend
```bash
cd cotizador-backend/src/Cotizador.API
# Configurar credenciales en appsettings.Development.json
dotnet run
# Verificar: GET http://localhost:5000/ (con Authorization: Basic <base64>)
```

## Variables de entorno — cotizador-core-mock

| Variable | Default | Descripción |
|----------|---------|-------------|
| PORT | 3001 | Puerto HTTP |
| FOLIO_START | 1 | Contador inicial de folios |

## Configuración — cotizador-backend

En `appsettings.Development.json`:
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "cotizador_db"
  },
  "CoreOhs": {
    "BaseUrl": "http://localhost:3001"
  },
  "Auth": {
    "Username": "admin",
    "Password": "cotizador2026"
  }
}
```

## Ejecutar tests

```bash
# Backend unit tests
cd cotizador-backend
dotnet test src/Cotizador.Tests/Cotizador.Tests.csproj
```

## Folio format
`DAN-YYYY-NNNNN` (e.g., `DAN-2026-00001`)

## Autenticación
Basic Auth — header: `Authorization: Basic <base64(user:pass)>`
