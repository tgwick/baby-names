# Project Name Match - Development Plan

## Overview
A collaborative web app for couples to discover and agree on baby names through a "Like/Dislike" voting system with matching.

**Tech Stack:**
- Frontend: Vue.js 3 + Pinia + TypeScript
- Backend: .NET 8 Core Web API + ASP.NET Core Identity
- Database: PostgreSQL
- Infrastructure: Azure App Service + Static Web Apps

**Confirmed Decisions:**
- Database: PostgreSQL
- Authentication: Email/Password only (JWT) - OAuth deferred to Phase II
- Real-time: Polling/manual refresh for MVP - SignalR deferred to Phase II
- Development approach: Sequential phases

---

## Phase 1: Project Foundation & Infrastructure ✅

### 1.1 Backend Setup
- [x] Create .NET 8 Web API project structure
- [x] Configure solution with proper layering (API, Application, Domain, Infrastructure)
- [x] Set up Entity Framework Core with PostgreSQL
- [x] Configure ASP.NET Core Identity for authentication
- [x] Add JWT token authentication
- [x] Set up CORS for Vue frontend
- [x] Create base API response models and error handling

### 1.2 Frontend Setup
- [x] Initialize Vue 3 project with Vite + TypeScript
- [x] Configure Pinia for state management
- [x] Set up Vue Router
- [x] Install and configure Tailwind CSS (mobile-first responsive)
- [x] Create base layout components
- [x] Set up Axios for API communication with interceptors
- [x] Configure authentication state management

### 1.3 Database Schema (Initial)
```
Tables:
- Users (ASP.NET Identity tables)
- Sessions (SessionId, InitiatorId, PartnerId, Gender, JoinCode, PartnerLink, Status, CreatedAt)
- Names (NameId, NameText, Gender, PopularityScore, Origin)
- Votes (VoteId, UserId, NameId, SessionId, VoteType, VotedAt)
```

**Deliverable:** Running backend API with health endpoint, Vue app with routing, database migrations applied ✅

---

## Phase 2: Authentication & User Management ✅

### 2.1 Backend - Auth Endpoints
- [x] POST `/api/auth/register` - Email/password registration
- [x] POST `/api/auth/login` - Login with JWT response
- [ ] POST `/api/auth/refresh` - Token refresh (deferred)
- [x] GET `/api/auth/me` - Current user info

### 2.2 Frontend - Auth Flow
- [x] Registration page with form validation
- [x] Login page
- [x] Auth guards for protected routes
- [x] Token storage and auto-refresh logic
- [x] Logout functionality

**Deliverable:** Users can register, login, and maintain authenticated sessions ✅

---

## Phase 3: Session & Partner Linking

### 3.1 Backend - Session Management
- [ ] POST `/api/sessions` - Create session (Initiator selects gender, generates JoinCode + PartnerLink)
- [ ] POST `/api/sessions/join` - Join via JoinCode
- [ ] GET `/api/sessions/join/{partnerLink}` - Join via URL
- [ ] GET `/api/sessions/current` - Get active session for user
- [ ] Domain logic: Generate unique 6-digit codes and UUIDs for links

### 3.2 Frontend - Session Flow
- [ ] "Create Session" page (gender selection: Male/Female/Neutral)
- [ ] Display generated Join Code + shareable link
- [ ] "Join Session" page (code entry)
- [ ] Partner link landing page
- [ ] Session status dashboard (waiting for partner / linked)

**Deliverable:** Initiator can create session, share code/link, Partner can join and both are locked into 1-to-1 session

---

## Phase 4: Name Data Import

### 4.1 Data Preparation
- [ ] Obtain SSA baby names dataset (last 100 years)
- [ ] Create data transformation script (normalize, assign gender, calculate popularity)
- [ ] Create seed script for database population

### 4.2 Backend - Names API
- [ ] GET `/api/names/next` - Fetch random unvoted name for current user's session
- [ ] Include gender filtering based on session settings
- [ ] Track "seen" state per user

**Deliverable:** Database seeded with ~10,000+ names, API returns random unvoted names

---

## Phase 5: Voting Engine (Core Feature)

### 5.1 Backend - Voting System
- [ ] POST `/api/votes` - Submit vote (Like/Dislike)
- [ ] Prevent duplicate votes on same name
- [ ] Update vote state tracking
- [ ] Real-time match detection on vote submission

### 5.2 Frontend - Swipe Interface
- [ ] Name card component (large, mobile-friendly)
- [ ] Like button (green/heart)
- [ ] Dislike button (red/X)
- [ ] Swipe gesture support (optional enhancement)
- [ ] Loading state between names
- [ ] "No more names" state
- [ ] Vote counter/progress indicator

**Deliverable:** Users can vote Like/Dislike on names, votes are persisted, names don't repeat

---

## Phase 6: Matching & Results

### 6.1 Backend - Match Detection
- [ ] GET `/api/matches` - Retrieve all mutual likes for session
- [ ] Match detection logic (both users liked same NameId)
- [ ] Sort by match date

### 6.2 Frontend - Match Display
- [ ] Match list page
- [ ] Match notification/celebration animation when new match occurs
- [ ] Match counter badge in navigation

**Deliverable:** Matched names displayed, users see mutual likes

---

## Phase 7: Conflict Resolution

### 7.1 Backend - Conflicts
- [ ] GET `/api/conflicts` - Names I liked that partner disliked
- [ ] POST `/api/conflicts/{nameId}/clear` - Partner clears their dislike

### 7.2 Frontend - Conflict View
- [ ] Conflict list page (names with conflict status)
- [ ] "Clear Dislike" action for the partner who disliked
- [ ] Visual distinction between my-likes-their-dislikes vs their-likes-my-dislikes

**Deliverable:** Users can see and resolve conflicts, cleared names return to voting pool

---

## Phase 8: Polish & Production Readiness

### 8.1 UX Enhancements
- [ ] Responsive design audit
- [ ] Loading states and error handling
- [ ] Empty states
- [ ] Accessibility (a11y) review

### 8.2 Backend Hardening
- [ ] Rate limiting
- [ ] Input validation
- [ ] Logging and monitoring setup
- [ ] API documentation (Swagger/OpenAPI)

### 8.3 Deployment
- [ ] Azure App Service setup for backend
- [ ] Azure Static Web Apps for frontend
- [ ] Database provisioning (Azure PostgreSQL)
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] Environment configuration (dev/staging/prod)

**Deliverable:** Production-ready application deployed to Azure

---

## Project Structure

```
/baby-names
├── /backend
│   ├── NameMatch.Api/            # Web API project
│   ├── NameMatch.Application/    # Business logic, DTOs, interfaces
│   ├── NameMatch.Domain/         # Entities, value objects
│   ├── NameMatch.Infrastructure/ # EF Core, Identity, external services
│   └── NameMatch.sln
├── /frontend
│   ├── src/
│   │   ├── components/
│   │   ├── views/
│   │   ├── stores/               # Pinia stores
│   │   ├── services/             # API clients
│   │   ├── router/
│   │   └── types/
│   ├── package.json
│   └── vite.config.ts
├── /data
│   └── ssa-names/                # SSA data files and import scripts
├── DEVELOPMENT_PLAN.md
└── README.md
```

---

## Phase Dependencies

```
Phase 1 (Foundation)
    ↓
Phase 2 (Auth) ←────────┐
    ↓                   │
Phase 3 (Sessions) ─────┤
    ↓                   │
Phase 4 (Names Data) ───┘
    ↓
Phase 5 (Voting)
    ↓
Phase 6 (Matching)
    ↓
Phase 7 (Conflicts)
    ↓
Phase 8 (Polish & Deploy)
```

---

## Future Considerations (Phase II)

From PRD:
- Meanings & Etymology: Display name meanings on cards
- Surname Preview: Input last name to preview full name
- Exclusion List: Upload names to auto-dislike (family members, ex-partners)

Technical enhancements:
- OAuth providers (Google/Apple Sign-In)
- Real-time updates with SignalR
- Push notifications for new matches
- Native mobile app (Vue + Capacitor or React Native)
