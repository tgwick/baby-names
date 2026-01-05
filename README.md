# NameMatch

A collaborative web app for couples to discover and agree on baby names through a "Like/Dislike" voting system.

## Features

- **Partner Linking** - Create a session and share a code/link with your partner
- **Swipe Interface** - Like or dislike names one at a time
- **Matching** - See names you both liked
- **Conflict Resolution** - Review names where you disagreed

## Tech Stack

| Layer | Technology |
|-------|------------|
| Frontend | Vue 3, TypeScript, Vite, Pinia, Tailwind CSS |
| Backend | .NET 8, ASP.NET Core Identity, Entity Framework Core |
| Database | PostgreSQL |
| Auth | JWT tokens |
| Infrastructure | Azure Container Apps, Bicep IaC |

## Quick Start (Docker)

The easiest way to run the full stack:

```bash
# Start all services (PostgreSQL, backend, frontend)
docker compose up --build

# Access the app
# Frontend: http://localhost:5173
# Backend:  http://localhost:5001
# Swagger:  http://localhost:5001/swagger
```

### Development with Hot Reload

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up
```

### Stop Services

```bash
docker compose down        # Stop containers
docker compose down -v     # Stop and reset database
```

## Manual Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [PostgreSQL](https://www.postgresql.org/) (or use Docker)

### Database

Start PostgreSQL with Docker:
```bash
docker compose up postgres
```

### Backend

```bash
cd backend
dotnet run --project NameMatch.Api
```

API runs at http://localhost:5001. Swagger UI available at http://localhost:5001/swagger.

### Frontend

```bash
cd frontend
npm install
npm run dev
```

App runs at http://localhost:5173.

## Project Structure

```
namematch/
├── backend/
│   ├── NameMatch.Api/            # Web API, controllers
│   ├── NameMatch.Application/    # DTOs, interfaces, business logic
│   ├── NameMatch.Domain/         # Entities, enums
│   ├── NameMatch.Infrastructure/ # EF Core, Identity, services
│   └── NameMatch.Tests/          # Unit tests
├── frontend/
│   └── src/
│       ├── components/           # Reusable Vue components
│       ├── views/                # Page components
│       ├── stores/               # Pinia state management
│       ├── services/             # API client
│       ├── router/               # Vue Router config
│       └── types/                # TypeScript types
├── infra/
│   ├── bicep/                    # Azure Bicep IaC
│   └── scripts/                  # Deployment scripts
├── .github/workflows/            # CI/CD pipelines
├── docker-compose.yml            # Production-like Docker setup
└── docker-compose.dev.yml        # Development with hot reload
```

## API Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Create account |
| POST | `/api/auth/login` | Login, get JWT |
| GET | `/api/auth/me` | Get current user |

### Sessions
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/sessions` | Create session |
| POST | `/api/sessions/join` | Join via code |
| GET | `/api/sessions/join/{link}` | Join via partner link |
| GET | `/api/sessions/current` | Get active session |

### Names & Voting
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/names/next?count=N` | Get N unvoted names |
| POST | `/api/votes` | Submit vote |
| GET | `/api/votes/matches` | Get mutual likes |
| GET | `/api/votes/stats` | Voting statistics |

### Conflicts
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/conflicts` | Get voting conflicts |
| POST | `/api/conflicts/{nameId}/clear` | Clear dislike |

### Health
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Full health check |
| GET | `/health/live` | Liveness probe |
| GET | `/health/ready` | Readiness probe |

## Testing

```bash
# Backend unit tests
cd backend && dotnet test

# Frontend unit tests
cd frontend && npm run test:run

# E2E tests (requires running backend)
cd frontend && npm run e2e
```

## Infrastructure

See [INFRASTRUCTURE.md](./INFRASTRUCTURE.md) for:
- Architecture overview
- CI/CD pipeline documentation
- Bicep module reference
- Troubleshooting guide

See [DEPLOYMENT.md](./DEPLOYMENT.md) for:
- Step-by-step Azure deployment instructions
- Service principal and GitHub secrets setup
- Cost management tips

## License

Private project.
