# Developer Platform MVP

## Stack
- Backend: .NET 8 Web API + EF Core + PostgreSQL
- Frontend: React + TypeScript + Vite
- AI: OpenAI API via backend `AiService`

## Run
```bash
docker compose up --build
```

- Frontend: http://localhost:5173
- API: http://localhost:8080/swagger

## Features
- Three flows: Decision / Production / Integration
- WBS + Execution Packages + CPM activities and dependencies
- Demo mode on startup with demo project, open decisions and not-ready scenario
- Role switcher in UI without auth
