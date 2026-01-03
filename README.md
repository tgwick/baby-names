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

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [PostgreSQL](https://www.postgresql.org/) (or Docker)

### Database Setup

Start PostgreSQL with Docker:
```bash
docker run -d --name namematch-db \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=namematch_dev \
  -p 5432:5432 \
  postgres:16
```

### Backend

```bash
cd backend

# Apply database migrations
dotnet ef database update --project NameMatch.Infrastructure --startup-project NameMatch.Api

# Run the API
dotnet run --project NameMatch.Api
```

API runs at http://localhost:5001. Swagger UI available at http://localhost:5001/swagger.

### Frontend

```bash
cd frontend

# Install dependencies
npm install

# Start dev server
npm run dev
```

App runs at http://localhost:5173.

## Project Structure

```
baby-names/
├── backend/
│   ├── NameMatch.Api/            # Web API, controllers
│   ├── NameMatch.Application/    # DTOs, interfaces, business logic
│   ├── NameMatch.Domain/         # Entities, enums
│   ├── NameMatch.Infrastructure/ # EF Core, Identity, services
│   └── NameMatch.sln
├── frontend/
│   └── src/
│       ├── components/           # Reusable Vue components
│       ├── views/                # Page components
│       ├── stores/               # Pinia state management
│       ├── services/             # API client
│       ├── router/               # Vue Router config
│       └── types/                # TypeScript types
└── data/
    └── ssa-names/                # SSA name data (future)
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
| GET | `/api/sessions/{id}` | Get session by ID |

### Health
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/health` | Health check |

## Development Phases

See [DEVELOPMENT_PLAN.md](./DEVELOPMENT_PLAN.md) for the full roadmap.

- [x] Phase 1: Project Foundation
- [x] Phase 2: Authentication
- [x] Phase 3: Session & Partner Linking
- [ ] Phase 4: Name Data Import
- [ ] Phase 5: Voting Engine
- [ ] Phase 6: Matching
- [ ] Phase 7: Conflict Resolution
- [ ] Phase 8: Polish & Deploy

## License

Private project.
