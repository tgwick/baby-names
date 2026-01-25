# Hatch A Name

🐣 A cozy way for couples to discover baby names together. Swipe through names—when you both love one, it hatches as a match!

**Website:** [hatchaname.com](https://hatchaname.com)

## Features

- **Multi-Session Support** - Create unlimited sessions for different name searches (e.g., boy names, girl names, backup options)
- **Partner Linking** - Create a session and share a code/link with your partner
- **Preference Filtering** - Set your name style preferences (classic, trendy, unique) and length to filter names
- **Swipe Interface** - Like or dislike names one at a time
- **Watch Names Hatch** - See names you both loved as they hatch into matches
- **Conflict Resolution** - Review names where you disagreed
- **Archive Sessions** - Archive completed sessions to keep your list tidy

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
hatchaname/
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
| GET | `/api/sessions` | List all user sessions |
| POST | `/api/sessions` | Create session |
| GET | `/api/sessions/{id}` | Get session by ID |
| POST | `/api/sessions/join` | Join via code |
| GET | `/api/sessions/join/{link}` | Join via partner link |
| PATCH | `/api/sessions/{id}/archive` | Archive a session |
| PATCH | `/api/sessions/{id}/unarchive` | Unarchive a session |

### Names & Voting
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/names/next?sessionId={id}&count=N` | Get N unvoted names for session |
| POST | `/api/votes?sessionId={id}` | Submit vote |
| GET | `/api/votes/matches?sessionId={id}` | Get mutual likes |
| GET | `/api/votes/stats?sessionId={id}` | Voting statistics |

### Conflicts
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/conflicts?sessionId={id}` | Get voting conflicts |
| POST | `/api/conflicts/{nameId}/clear?sessionId={id}` | Clear dislike |

### Filters (Preferences)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/filters/questions` | Get filter questions |
| GET | `/api/filters?sessionId={id}` | Get user's saved filters |
| POST | `/api/filters?sessionId={id}` | Save filter responses |
| GET | `/api/filters/status?sessionId={id}` | Check if both partners completed |

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

## Deploying to Production

Production deployments are done via **GitHub Releases**. This provides version history, release notes, and automatic deployment.

### Prerequisites
- Changes must be merged to `main`
- Dev deployment must have run successfully (verify at dev.hatchaname.com)

### Creating a Release (GitHub UI)

1. Go to [Releases](https://github.com/tgwick/baby-names/releases)
2. Click **"Draft a new release"**
3. Click **"Choose a tag"** → type version (e.g., `v1.0.0`) → **"Create new tag"**
4. Leave **Target** as `main`
5. Add a **title** (e.g., `v1.0.0 - Feature Name`)
6. Add **release notes** describing the changes
7. Click **"Publish release"**

This automatically triggers the production deployment workflow.

### Creating a Release (CLI)

```bash
gh release create v1.0.0 \
  --title "v1.0.0 - Feature Name" \
  --notes "- Change 1
- Change 2"
```

### Versioning

Use [semantic versioning](https://semver.org/):
- **v1.0.0** → Major release (breaking changes)
- **v1.1.0** → Minor release (new features)
- **v1.1.1** → Patch release (bug fixes)

### Troubleshooting

If a release deployment fails:
1. Check if the dev deployment ran after merging (images are built during dev deploy)
2. Verify the tag points to the correct commit: `git rev-parse v1.0.0`
3. If the tag is wrong, delete and recreate:
   ```bash
   gh release delete v1.0.0 --yes
   git push --delete origin v1.0.0
   # Then create a new release
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
