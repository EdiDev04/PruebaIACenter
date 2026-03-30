# Cotizador de Seguros de Daños

> Reto IA Center — ASDD (Agent Spec-Driven Development)

Sistema de cotización de seguros para pólizas de daños (incendio, CAT, equipo electrónico, falla de maquinaria, etc.). Permite crear folios, registrar información general del riesgo asegurado y calcular primas en base a tarifas del core externo.

---

## Arquitectura

```
 cotizador-webapp
 React 18 + TypeScript + Vite
 Puerto: 5173
       |
       | REST (Basic Auth)
       ↓
 cotizador-backend
 .NET 8 + ASP.NET Core + MongoDB
 Puerto: 5000
       |
       | HTTP (HttpClient)
       ↓
 cotizador-core-mock
 Node.js + Express + TypeScript
 Puerto: 3001
```

---

## Stack tecnológico

| Componente | Tecnología |
|------------|-----------|
| Frontend | React 18 · TypeScript · Redux Toolkit · TanStack Query · Vite |
| Backend | C# · .NET 8 · ASP.NET Core · MongoDB.Driver · FluentValidation |
| Core mock | Node.js · Express · TypeScript · fixtures JSON |
| Testing BE | xUnit · Moq · FluentAssertions |
| Testing FE | Vitest · Testing Library |

---

## Prerrequisitos

- Node.js 20+
- .NET SDK 8
- MongoDB 7+ (local o Docker)
- npm 9+

---

## Variables de entorno

### cotizador-webapp — `.env.local`

Crea el archivo copiando `.env.example`:

```bash
cp cotizador-webapp/.env.example cotizador-webapp/.env.local
```

| Variable | Valor por defecto (desarrollo) | Descripción |
|----------|-------------------------------|-------------|
| `VITE_API_URL` | `http://localhost:5000` | URL base del backend |
| `VITE_API_USER` | `admin` | Usuario para Basic Auth |
| `VITE_API_PASSWORD` | `cotizador2026` | Contraseña para Basic Auth |
| `VITE_CORE_MOCK_URL` | `http://localhost:3001` | URL del core mock (referencia) |

### cotizador-backend — `appsettings.Development.json`

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
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

> **Importante:** el archivo `appsettings.Development.json` incluido en el repositorio tiene `AllowedOrigins: ["http://localhost:3000"]`. Actualiza ese valor a `http://localhost:5173` para que el webapp pueda llamar al backend sin errores CORS.

### cotizador-core-mock

| Variable | Valor por defecto | Descripción |
|----------|------------------|-------------|
| `PORT` | `3001` | Puerto HTTP del mock |

---

## Arranque del proyecto

Se necesitan **3 terminales** abiertas en la raíz del repositorio.

### Terminal 1 — cotizador-core-mock

```bash
cd cotizador-core-mock
npm install
npm run dev
# Servicio disponible en: http://localhost:3001
# Verificar: curl http://localhost:3001/health
```

### Terminal 2 — cotizador-backend

```bash
cd cotizador-backend
dotnet restore
dotnet run --project src/Cotizador.API/Cotizador.API.csproj
# Servicio disponible en: http://localhost:5000
```

> Asegúrate de que `appsettings.Development.json` tenga las credenciales y la URL de CORS correctas antes de iniciar.

### Terminal 3 — cotizador-webapp

```bash
cd cotizador-webapp
cp .env.example .env.local   # solo la primera vez
npm install
npm run dev
# Aplicación disponible en: http://localhost:5173
```

### Opción alternativa — MongoDB con Docker

Si no tienes MongoDB instalado localmente:

```bash
docker run -d --name mongo-cotizador -p 27017:27017 mongo:7
```

---

## Ejecutar tests

### Backend

```bash
cd cotizador-backend
dotnet test src/Cotizador.Tests/Cotizador.Tests.csproj
```

### Frontend

```bash
cd cotizador-webapp
npm test
```

---

## Endpoints disponibles

Todos los endpoints requieren `Authorization: Basic <base64(user:password)>`.

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `POST` | `/v1/folios` | Crea un nuevo folio de cotización. Requiere header `Idempotency-Key` |
| `GET` | `/v1/quotes/{folio}` | Obtiene el resumen completo de una cotización |
| `GET` | `/v1/quotes/{folio}/general-info` | Obtiene la información general de una cotización |
| `PUT` | `/v1/quotes/{folio}/general-info` | Actualiza la información general de una cotización |
| `GET` | `/v1/quotes/{folio}/state` | Estado y progreso de la cotización — pasos completados, alertas de ubicación y cobertura activa (SPEC-008) |

**Formato de folio:** `DAN-YYYY-NNNNN` (ejemplo: `DAN-2026-00001`)

**Autenticación:** Basic Auth — `admin` / `cotizador2026`

---

## Estructura del proyecto

```
cotizador-backend/
├── src/
│   ├── Cotizador.API/           # Controllers, middleware, auth
│   ├── Cotizador.Application/   # Casos de uso, DTOs, interfaces
│   ├── Cotizador.Domain/        # Entidades, value objects, reglas de dominio
│   ├── Cotizador.Infrastructure/
│   │   ├── Persistence/         # Repositorios MongoDB
│   │   └── ExternalServices/    # Cliente HTTP para core-ohs
│   └── Cotizador.Tests/         # Tests unitarios

cotizador-core-mock/
└── src/
    ├── fixtures/                # Datos JSON estáticos (tarifas, catálogos)
    └── routes/                  # Endpoints del mock

cotizador-webapp/
└── src/
    ├── app/                     # Store, router, query client
    ├── entities/                # Entidades de dominio FE (folio, catalog)
    ├── features/                # Flujos de usuario (creación, búsqueda)
    ├── pages/                   # Páginas enrutadas
    └── shared/                  # httpClient, componentes compartidos
```
