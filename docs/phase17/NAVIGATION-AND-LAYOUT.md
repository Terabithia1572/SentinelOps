# SentinelOps Application Shell, Navigation Hierarchy & Dead-End Elimination

## 1. Executive Summary

Checkpoint 17.2 addresses critical information architecture, navigation ergonomics, and usability gaps discovered during the product audit. The flat 23-item sidebar was restructured into a 4-tier functional domain model. The static header was transformed into a dynamic route-aware breadcrumb hierarchy. A responsive mobile drawer was implemented to eliminate layout crushing on mobile devices (< 768px). Finally, a dedicated HTTP 404 recovery page was introduced to eliminate dead-ends.

---

## 2. Information Architecture & Navigation Model

The application features 24 functional routes grouped into four distinct operational domains:

```
SENTINELOPS SHELL
│
├── OPERATIONS
│   ├── Overview (/)
│   ├── Dashboards (/dashboards)
│   ├── APM & Service Map (/apm)
│   ├── AI Observability (/ai-observability)
│   ├── Applications (/applications)
│   └── Services & Health (/services)
│
├── TELEMETRY
│   ├── Logs (/logs)
│   ├── Traces (/traces)
│   ├── Metrics (/metrics)
│   ├── Exceptions (/exceptions)
│   └── Analytics & Reports (/analytics)
│
├── INCIDENT RESPONSE
│   ├── Active Alerts (/alerts)
│   ├── Alert Rules (/alert-rules)
│   ├── Incidents (/incidents)
│   ├── Status Pages (/status-pages)
│   └── SLOs & Objectives (/slos)
│
└── ADMINISTRATION
    ├── Organizations (/organizations)
    ├── Team & Users (/team)
    ├── API Keys (/api-keys)
    ├── Billing & Plans (/billing)
    ├── Audit Logs (/audit-logs)
    └── Settings (/settings)
```

### 2.1 Benefits of the Grouped Navigation
1. **Cognitive Load Reduction**: SREs and operators can immediately locate telemetry inspection tools separate from administrative configurations.
2. **Context-Aware Visual Hierarchy**: Category headers (`OPERATIONS`, `TELEMETRY`, `INCIDENT RESPONSE`, `ADMINISTRATION`) provide clear sectioning with consistent typography.
3. **Active Link Indication**: Active routes receive bold high-contrast styling (`bg-blue-600 text-white shadow-sm`) with clear contrast against inactive links (`text-slate-400`).

---

## 3. Dynamic Breadcrumbs

Implemented in `src/components/ui/breadcrumbs.tsx` and integrated directly into the `MainLayout` top bar:
- Analyzes current route location (`window.location.pathname`).
- Automatically resolves route slugs and IDs into human-readable path segments.
- Renders accessible semantic `<nav aria-label="Breadcrumb">` markup.
- Injects `ChevronRight` separators and assigns `aria-current="page"` to the terminal active segment.

---

## 4. Mobile & Responsive Layout Architecture

### 4.1 Mobile Viewports (< 768px)
- The fixed desktop sidebar is hidden (`hidden md:flex`).
- A hamburger menu trigger button (`Menu` icon from Lucide) is displayed in the header.
- Activating the menu renders a smooth slide-out drawer sheet with an accessible backdrop overlay (`bg-black/75 backdrop-blur-sm`).
- Clicking any navigation item or clicking outside the drawer automatically dismisses the drawer, preventing trapped user interactions.

### 4.2 Desktop & Ultrawide Viewports (≥ 768px to 3840px)
- Fixed vertical navigation bar (64 Tailwind units / 256px width) with independent vertical scrollbar (`overflow-y-auto`).
- Main content container uses max-width constraints (`max-w-7xl mx-auto`) with responsive padding (`p-4 md:p-6`) to guarantee comfortable density across widescreen 1440p and 4K displays.

---

## 5. Dead-End Elimination: HTTP 404 Recovery (`not-found.tsx`)

Previously, invalid URL paths resulted in a blank main view without explanation. The new `NotFoundPage`:
1. Renders a distinct visual indicator (`Compass` icon in dark slate container).
2. Displays semantic badges (`HTTP 404`, `Resource Not Found`).
3. Provides a concise, professional diagnostic description ("Page Not Located in Topology").
4. Offers two primary recovery actions:
   - **Previous Screen**: Invokes `navigate(-1)` to return the user to their prior context.
   - **Operations Overview**: Navigates directly to the primary system overview (`/`).
5. Injects a "Quick Telemetry Navigation" card providing 1-click jumps to **Active Incidents** (`/incidents`) and **Log Explorer** (`/logs`).

---

## 6. Automated Test Coverage & Verification

Automated test suites verify all components of Checkpoint 17.2:
- `src/SentinelOps.Web/src/components/layout/__tests__/sidebar.test.tsx`:
  - Renders brand logo and all 4 structured navigation categories (`Operations`, `Telemetry`, `Incident Response`, `Administration`).
  - Renders valid navigation links (`/`, `/apm`, `/logs`, `/alerts`, `/billing`).
  - Displays user profile information, email, and role badge.
- `src/SentinelOps.Web/src/pages/__tests__/not-found.test.tsx`:
  - Renders HTTP 404 header and explanatory copy.
  - Verifies presence of recovery action buttons and quick telemetry links.
- Vitest suite status: **18 test files passed (100%), 69 tests passed**.
- TypeScript type check: **0 errors**.
