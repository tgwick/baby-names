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
dotnet run --project NameMatch.Api  # Run API (default: https://localhost:5001)
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
- **NameMatch.Infrastructure** - EF Core `ApplicationDbContext`, `ApplicationUser` (Identity), external services. References Application + Domain.

### Frontend Structure

- `src/stores/` - Pinia stores (e.g., `auth.ts` for JWT handling)
- `src/services/` - API client with Axios interceptors
- `src/router/` - Vue Router with auth guards (`requiresAuth`, `guest` meta)
- `src/views/` - Page components
- `src/components/` - Reusable components
- `src/types/` - TypeScript interfaces

### Key Patterns

- API responses use `ApiResponse<T>` wrapper with `Success`, `Data`, `Errors` fields
- Frontend proxies `/api` requests to backend via Vite config
- Auth tokens stored in localStorage, attached via Axios interceptor
- Router guards redirect unauthenticated users to `/login`

## Database Schema

- **Users** - ASP.NET Identity tables
- **Sessions** - Links two users with JoinCode/PartnerLink, stores target Gender
- **Names** - Baby names with Gender, PopularityScore, Origin
- **Votes** - User votes (Like/Dislike) on names within a session

## Configuration

Backend config in `appsettings.json`:
- `ConnectionStrings:DefaultConnection` - PostgreSQL connection
- `Jwt:Key/Issuer/Audience` - JWT settings
- `Cors:AllowedOrigins` - Allowed frontend origins
