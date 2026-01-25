# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Hatch A Name (hatchaname.com) is a cozy web app for couples to discover baby names together. Two users link via a unique code, swipe through names, and when both love a name—it "hatches" as a match.

## Tech Stack

- **Backend:** .NET 8 Web API + ASP.NET Core Identity + Entity Framework Core + PostgreSQL
- **Frontend:** Vue 3 + TypeScript + Vite + Pinia + Tailwind CSS
- **Auth:** JWT tokens (email/password only for MVP)

## Git Workflow

This project uses **Gitflow** branching strategy:

- **`main`** - Production-ready code. Only receives merges from `develop` (releases) or hotfix branches.
- **`develop`** - Integration branch for features. All feature branches are created from and merged back into `develop`.
- **Feature branches** - Created from `develop` with naming convention `feature/<description>`. Merged back to `develop` via PR.
- **Hotfix branches** - Created from `main` for urgent production fixes. Merged to both `main` and `develop`.

### Branch Rules
- Never commit directly to `main` or `develop`
- All changes require a PR with passing CI checks
- Feature PRs target `develop`
- Release PRs merge `develop` → `main`

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
- **NameMatch.Domain** - Entities (`Name`, `Session`, `Vote`, `NameCategory`, `UserPreference`), Enums (`Gender`, `VoteType`, `SessionStatus`, `PreferenceLevel`). No dependencies.
- **NameMatch.Infrastructure** - EF Core `ApplicationDbContext`, `ApplicationUser` (Identity), services. References Application + Domain.

### Frontend Structure

- `src/stores/` - Pinia stores (`auth.ts` for JWT, `session.ts` for multi-session state, `filters.ts` for filter questionnaire)
- `src/services/` - API client with Axios interceptors
- `src/router/` - Vue Router with auth guards (`requiresAuth`, `guest` meta)
- `src/views/` - Page components:
  - Auth: `LoginView`, `RegisterView`
  - Sessions: `SessionListView` (default after login), `SessionDetailView`, `CreateSessionView`, `JoinSessionView`, `JoinLinkView`
  - Filters: `PreferencesView` (at `/sessions/:sessionId/preferences`)
  - Voting: `SwipeView`, `MatchesView`, `ConflictsView` (all at `/sessions/:sessionId/*`)
- `src/components/` - Reusable components (`NameCard`, `MatchCelebration`, `AppHeader`, `FilterQuestionnaire`)
- `src/types/` - TypeScript interfaces (`auth.ts`, `session.ts`, `vote.ts`, `filters.ts`)

## API Endpoints

### Auth
- `POST /api/auth/register` - Register with email/password/displayName
- `POST /api/auth/login` - Login, returns JWT token
- `GET /api/auth/me` - Get current user (requires auth)

### Sessions (Multi-Session Architecture)
- `GET /api/sessions?includeArchived=false` - List all user sessions
- `POST /api/sessions` - Create session with targetGender (0=Male, 1=Female, 2=Neutral)
- `GET /api/sessions/{id}` - Get session by ID
- `POST /api/sessions/join` - Join via JoinCode
- `GET /api/sessions/join/{partnerLink}` - Join via partner link
- `PATCH /api/sessions/{id}/archive` - Archive a session
- `PATCH /api/sessions/{id}/unarchive` - Unarchive a session

### Names
- `GET /api/names/next?sessionId={id}&count=N` - Fetch N random unvoted names for session
- `GET /api/names/batch?sessionId={id}&count=N` - Alias for next

### Votes (all require sessionId query param)
- `POST /api/votes?sessionId={id}` - Submit vote (NameId, VoteType: 0=Like, 1=Dislike)
- `GET /api/votes/matches?sessionId={id}` - Get mutual likes for session
- `GET /api/votes/stats?sessionId={id}` - Get voting statistics (total votes, likes, matches)

### Conflicts (all require sessionId query param)
- `GET /api/conflicts?sessionId={id}` - Get voting conflicts (names one liked, other disliked)
- `POST /api/conflicts/{nameId}/clear?sessionId={id}` - Clear your dislike on a name

### Filters (session-specific preferences)
- `GET /api/filters/questions` - Get filter questions (name style, syllable length)
- `GET /api/filters?sessionId={id}` - Get user's saved filters for session
- `POST /api/filters?sessionId={id}` - Save filter responses (nameStyle, minSyllables, maxSyllables)
- `GET /api/filters/status?sessionId={id}` - Check if both partners have completed filters

### Health
- `GET /health` - Full health check with database status
- `GET /health/ready` - Readiness probe (checks dependencies)
- `GET /health/live` - Liveness probe (always healthy)

## Key Patterns

- API responses use `ApiResponse<T>` wrapper with `Success`, `Data`, `Errors` fields
- Frontend proxies `/api` requests to backend via Vite config (port 5001)
- Auth tokens stored in localStorage, attached via Axios interceptor
- Router guards redirect unauthenticated users to `/login`
- **Multi-session routing:** All session-specific routes use `/sessions/:sessionId/*` pattern
- **Session context:** Frontend stores pass `sessionId` to all API calls; backend validates user belongs to session

## Database

PostgreSQL with tables:
- **AspNetUsers** - ASP.NET Identity (includes DisplayName, CreatedAt)
- **Sessions** - Links two users with JoinCode/PartnerLink, stores TargetGender, IsArchived, ArchivedAt
- **Names** - Baby names with Gender, PopularityScore, Origin, Syllables, TrendScore, StabilityScore
- **Votes** - User votes (Like/Dislike) on names within a session
- **NameCategories** - Categories for filtering (e.g., Biblical, Nature, Classic, Modern)
- **NameCategoryMappings** - Many-to-many link between Names and Categories with confidence score
- **UserFilters** - User filter settings per session (NameStyle, MinSyllables, MaxSyllables)

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
- `auth.setup.ts` - Authentication fixture (creates test user, saves storageState, redirects to `/sessions`)
- `auth.spec.ts` - Authentication flows, form validation, protected routes
- `session.spec.ts` - Unauthenticated session redirects (tests `/sessions` route)
- `session.authenticated.ts` - Authenticated session tests (create, session list, session detail)
- `preferences.authenticated.ts` - Filter questionnaire flow at `/sessions/:sessionId/preferences`
- **Config:** `frontend/playwright.config.ts` - Multi-project setup (setup, chromium, chromium-authenticated)

## Configuration

Backend config in `appsettings.json`:
- `ConnectionStrings:DefaultConnection` - PostgreSQL (default: localhost:5432)
- `Jwt:Key/Issuer/Audience/ExpiryInMinutes` - JWT settings
- `Cors:AllowedOrigins` - Allowed frontend origins

## Environments

### DEV
- **URL:** https://dev.hatchaname.com
- **Test Credentials:**
  - Email: `test@hatchaname.com`
  - Password: `TestPassword123!`
  - Display Name: `Test User`
- **Database:**
  - Host: `namematch-dev-pgsql.postgres.database.azure.com`
  - Database: `namematch`
  - Username: `pgadmin`
  - Password: Stored in Azure Key Vault (`namematch-dev-kv` → `postgres-password`)
  - SSL Mode: Require

### Production
- **URL:** https://hatchaname.com
- **Database:**
  - Host: `namematch-prod-pgsql.postgres.database.azure.com`
  - Database: `namematch`
  - Username: `pgadmin`
  - Password: Stored in Azure Key Vault (`namematch-prod-kv` → `postgres-password`)
  - SSL Mode: Require

## Reseeding Data

The backend auto-seeds names on startup if the `Names` table is empty. To reseed with fresh data (e.g., after adding new columns):

### 1. Connect to Database
```bash
# Get password from Key Vault (DEV example)
az keyvault secret show --vault-name namematch-dev-kv --name postgres-password --query value -o tsv

# Connect via psql
psql "host=namematch-dev-pgsql.postgres.database.azure.com dbname=namematch user=pgadmin sslmode=require"
```

### 2. Clear Data (allows re-seeding)
```sql
-- Clear votes first (has foreign key to Names)
TRUNCATE TABLE "Votes" CASCADE;

-- Clear names (triggers re-seed on next app restart)
TRUNCATE TABLE "Names" CASCADE;

-- Optional: Clear sessions to start fresh
TRUNCATE TABLE "Sessions" CASCADE;
```

### 3. Restart the Backend
The Container App will re-seed automatically on startup. To force a restart:
```bash
# DEV
az containerapp revision restart \
  --resource-group namematch-dev-rg \
  --name namematch-dev-api

# Or trigger a new deployment via GitHub Actions
```

### When to Reseed
- After adding new columns to the `Names` table (e.g., `TrendScore`, `StabilityScore`)
- After updating `processed-names.json` with new data
- When filter features return zero results due to missing data

## Pre-Push Checklist

**IMPORTANT:** Before pushing changes or creating a PR, always run the full test suite to avoid CI failures:

```bash
# 1. Backend build and tests (from project root, uses Docker)
docker run --rm -v "$(pwd)/backend:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet build NameMatch.sln
docker run --rm -v "$(pwd)/backend:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test

# 2. Frontend build and tests (from frontend/ directory)
cd frontend
npm run build      # Type-check and production build
npm run test:run   # Unit tests

# 3. E2E tests (requires running containers)
npm run e2e        # Playwright tests
```

All tests must pass before pushing. The CI pipeline runs these same checks.
