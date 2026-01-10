# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Hatch A Name (hatchaname.com) is a cozy web app for couples to discover baby names together. Two users link via a unique code, swipe through names, and when both love a name—it "hatches" as a match.

## Tech Stack

- **Backend:** .NET 8 Web API + ASP.NET Core Identity + Entity Framework Core + PostgreSQL
- **Frontend:** Vue 3 + TypeScript + Vite + Pinia + Tailwind CSS
- **Auth:** JWT tokens (email/password only for MVP)

## Commands

### Docker (Recommended - from project root)
```bash
docker compose up --build           # Start all services (PostgreSQL, backend, frontend)
docker compose down                 # Stop all containers
docker compose down -v              # Stop and reset database
docker compose logs -f backend      # View backend logs

# Development with hot reload
docker compose -f docker-compose.yml -f docker-compose.dev.yml up
```

### Backend (from `backend/` directory)
```bash
dotnet build NameMatch.sln          # Build solution
dotnet run --project NameMatch.Api  # Run API (default: http://localhost:5001)
dotnet test                         # Run all unit tests
dotnet ef migrations add <Name> --project NameMatch.Infrastructure --startup-project NameMatch.Api
dotnet ef database update --project NameMatch.Infrastructure --startup-project NameMatch.Api
```

### Frontend (from `frontend/` directory)
```bash
npm install        # Install dependencies
npm run dev        # Start dev server (localhost:5173)
npm run build      # Type-check and build for production
npm run preview    # Preview production build
npm run test       # Run unit tests in watch mode
npm run test:run   # Run unit tests once
npm run e2e        # Run Playwright E2E tests
npm run e2e:ui     # Run Playwright with interactive UI
```

## Architecture

### Backend - Clean Architecture (4 layers)

- **NameMatch.Api** - Controllers, Program.cs (DI setup, middleware). References Application + Infrastructure.
- **NameMatch.Application** - DTOs, service interfaces, business logic. References Domain.
- **NameMatch.Domain** - Entities (`Name`, `Session`, `Vote`), Enums (`Gender`, `VoteType`, `SessionStatus`). No dependencies.
- **NameMatch.Infrastructure** - EF Core `ApplicationDbContext`, `ApplicationUser` (Identity), services. References Application + Domain.

### Frontend Structure

- `src/stores/` - Pinia stores (`auth.ts` for JWT, `session.ts` for session/voting state)
- `src/services/` - API client with Axios interceptors
- `src/router/` - Vue Router with auth guards (`requiresAuth`, `guest` meta)
- `src/views/` - Page components:
  - Auth: `LoginView`, `RegisterView`
  - Session: `DashboardView`, `CreateSessionView`, `JoinSessionView`, `SessionView`
  - Voting: `SwipeView`, `MatchesView`, `ConflictsView`
- `src/components/` - Reusable components (`NameCard`, `MatchCelebration`, `AppHeader`)
- `src/types/` - TypeScript interfaces (`auth.ts`, `session.ts`, `vote.ts`)

## API Endpoints

### Auth
- `POST /api/auth/register` - Register with email/password/displayName
- `POST /api/auth/login` - Login, returns JWT token
- `GET /api/auth/me` - Get current user (requires auth)

### Sessions
- `POST /api/sessions` - Create session with targetGender (0=Male, 1=Female, 2=Neutral)
- `POST /api/sessions/join` - Join via JoinCode
- `GET /api/sessions/join/{partnerLink}` - Join via partner link
- `GET /api/sessions/current` - Get current active session
- `GET /api/sessions/{id}` - Get session by ID

### Names
- `GET /api/names/next?count=N` - Fetch N random unvoted names for current session
- `GET /api/names/batch?count=N` - Alias for next

### Votes
- `POST /api/votes` - Submit vote (NameId, VoteType: 0=Like, 1=Dislike)
- `GET /api/votes/matches` - Get mutual likes for current session
- `GET /api/votes/stats` - Get voting statistics (total votes, likes, matches)

### Conflicts
- `GET /api/conflicts` - Get voting conflicts (names one liked, other disliked)
- `POST /api/conflicts/{nameId}/clear` - Clear your dislike on a name

### Health
- `GET /health` - Full health check with database status
- `GET /health/ready` - Readiness probe (checks dependencies)
- `GET /health/live` - Liveness probe (always healthy)

## Key Patterns

- API responses use `ApiResponse<T>` wrapper with `Success`, `Data`, `Errors` fields
- Frontend proxies `/api` requests to backend via Vite config (port 5001)
- Auth tokens stored in localStorage, attached via Axios interceptor
- Router guards redirect unauthenticated users to `/login`

## Database

PostgreSQL with tables:
- **AspNetUsers** - ASP.NET Identity (includes DisplayName, CreatedAt)
- **Sessions** - Links two users with JoinCode/PartnerLink, stores TargetGender
- **Names** - Baby names with Gender, PopularityScore, Origin
- **Votes** - User votes (Like/Dislike) on names within a session

## Testing

### Backend Tests (xUnit)
- **Project:** `NameMatch.Tests`
- **Location:** `backend/NameMatch.Tests/`
- Uses in-memory database (`Microsoft.EntityFrameworkCore.InMemory`) and Moq for mocking
- `Services/SessionServiceTests.cs` - Session creation, joining, validation
- `Services/NameServiceTests.cs` - Name fetching, gender filtering, vote exclusion
- `Services/VoteServiceTests.cs` - Vote submission, matches, conflicts detection
- `Helpers/TestDbContextFactory.cs` - Test database setup helper

### Frontend Unit Tests (Vitest)
- **Location:** `frontend/src/stores/__tests__/`
- `auth.test.ts` - Auth store tests (login, register, logout, token management)
- `session.test.ts` - Session store tests (create, join, state management)

### E2E Tests (Playwright)
- **Location:** `frontend/e2e/`
- `auth.setup.ts` - Authentication fixture (creates test user, saves storageState)
- `auth.spec.ts` - Authentication flows, form validation, protected routes
- `session.spec.ts` - Unauthenticated session redirects
- `session.authenticated.ts` - Authenticated session tests (create, join, dashboard)
- **Config:** `frontend/playwright.config.ts` - Multi-project setup (setup, chromium, chromium-authenticated)

## Configuration

Backend config in `appsettings.json`:
- `ConnectionStrings:DefaultConnection` - PostgreSQL (default: localhost:5432)
- `Jwt:Key/Issuer/Audience/ExpiryInMinutes` - JWT settings
- `Cors:AllowedOrigins` - Allowed frontend origins
