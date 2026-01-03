# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NameMatch is a collaborative web app for couples to discover and agree on baby names through a "Like/Dislike" voting system with matching. Two users link via a unique code, swipe through names, and see mutual likes as "matches."

## Tech Stack

- **Backend:** .NET 8 Web API + ASP.NET Core Identity + Entity Framework Core + PostgreSQL
- **Frontend:** Vue 3 + TypeScript + Vite + Pinia + Tailwind CSS
- **Auth:** JWT tokens (email/password only for MVP)

## Commands

### Backend (from `backend/` directory)
```bash
dotnet build NameMatch.sln          # Build solution
dotnet run --project NameMatch.Api  # Run API (default: http://localhost:5001)
dotnet ef migrations add <Name> --project NameMatch.Infrastructure --startup-project NameMatch.Api
dotnet ef database update --project NameMatch.Infrastructure --startup-project NameMatch.Api
```

### Frontend (from `frontend/` directory)
```bash
npm install        # Install dependencies
npm run dev        # Start dev server (localhost:5173)
npm run build      # Type-check and build for production
npm run preview    # Preview production build
```

## Architecture

### Backend - Clean Architecture (4 layers)

- **NameMatch.Api** - Controllers, Program.cs (DI setup, middleware). References Application + Infrastructure.
- **NameMatch.Application** - DTOs, service interfaces, business logic. References Domain.
- **NameMatch.Domain** - Entities (`Name`, `Session`, `Vote`), Enums (`Gender`, `VoteType`, `SessionStatus`). No dependencies.
- **NameMatch.Infrastructure** - EF Core `ApplicationDbContext`, `ApplicationUser` (Identity), services. References Application + Domain.

### Frontend Structure

- `src/stores/` - Pinia stores (`auth.ts` for JWT, `session.ts` for session state)
- `src/services/` - API client with Axios interceptors
- `src/router/` - Vue Router with auth guards (`requiresAuth`, `guest` meta)
- `src/views/` - Page components (Login, Register, Dashboard, Session views)
- `src/components/` - Reusable components
- `src/types/` - TypeScript interfaces (`auth.ts`, `session.ts`)

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

### Health
- `GET /api/health` - Health check

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

## Configuration

Backend config in `appsettings.json`:
- `ConnectionStrings:DefaultConnection` - PostgreSQL (default: localhost:5432)
- `Jwt:Key/Issuer/Audience/ExpiryInMinutes` - JWT settings
- `Cors:AllowedOrigins` - Allowed frontend origins
