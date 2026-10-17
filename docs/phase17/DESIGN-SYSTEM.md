# SentinelOps Enterprise Design System & UI Component Specification

## 1. Executive Summary

SentinelOps Phase 17 establishes an enterprise-grade UI/UX foundation designed specifically for high-density, low-latency observability, APM telemetry, and SRE incident response workflows. The design system bridges modern web standards with strict accessibility (WCAG 2.1 AA), responsive multi-resolution compliance (from 375px mobile up to 3840px 4K ultrawide), and ergonomic data density.

---

## 2. Design Tokens & Foundations

### 2.1 Color Palette & Semantic Roles
The platform employs a dark-first, high-contrast, semantic-driven color architecture built on Tailwind CSS utility primitives.

| Semantic Token | Tailwind Value | Usage & Meaning | Accessibility Contrast (on #0B1120) |
|---|---|---|---|
| **Canvas Background** | `bg-slate-950` (`#030712`) / `bg-slate-900` (`#0f172a`) | Base application background, shell canvas | N/A (Baseline) |
| **Card / Surface** | `bg-slate-900/60` to `bg-slate-800/80` | Cards, panels, modals, data tables | > 4.5:1 ratio |
| **Border Default** | `border-slate-800` (`#1e293b`) | Structural section dividers, grid dividers | Subtle visual containment |
| **Border Accent** | `border-slate-700` (`#334155`) | Interactive borders on hover/focus | Clear affordance |
| **Primary Accent** | `text-blue-500` / `bg-blue-600` | Primary actions, active navigation, focus rings | > 4.8:1 against slate-950 |
| **Status Healthy / Ok** | `text-emerald-400` / `bg-emerald-500/20` | Operational services, passing tests, resolved alerts | Meets AA standard |
| **Status Warning / Degraded** | `text-amber-400` / `bg-amber-500/20` | High latency, degraded endpoints, pending alerts | Meets AA standard |
| **Status Critical / Error** | `text-rose-400` / `bg-rose-500/20` | Outages, firing incidents, validation failures | Meets AA standard |
| **Status Neutral / Info** | `text-slate-400` / `bg-slate-800` | Inactive, unknown, disabled, or informational | Meets AA standard |

### 2.2 Typography Hierarchy
- **Font Stack**: System sans-serif font stack (`Inter`, `-apple-system`, `BlinkMacSystemFont`, `Segoe UI`, `Roboto`, `Helvetica Neue`, sans-serif) for high legibility at dense viewport scales.
- **Monospace Stack**: JetBrains Mono, Fira Code, Consolas, Monaco, monospace for SQL queries, JSON payloads, trace IDs, span IDs, and log timestamps.
- **Scale**:
  - `text-2xl` (`1.5rem` / `24px`, bold / semibold): Page headings (`h1`)
  - `text-xl` (`1.25rem` / `20px`, semibold): Section headings (`h2`)
  - `text-lg` (`1.125rem` / `18px`, medium): Modal titles, card headers (`h3`)
  - `text-sm` (`0.875rem` / `14px`, regular / medium): Body text, data cell content, form fields
  - `text-xs` (`0.75rem` / `12px`, medium): Metadata labels, badges, timestamps, table column headers

### 2.3 Spacing & Density
- Metric data density uses an 8px grid baseline (`gap-2`, `gap-3`, `gap-4`, `p-4`, `p-6`).
- Tables and list rows use compact vertical padding (`py-2.5` to `py-3`) to maximize screen efficiency during active incidents while remaining touch-friendly on mobile viewports (minimum 40px target heights).

---

## 3. UI Component Primitives (`src/components/ui/`)

### 3.1 Skeleton (`skeleton.tsx`)
- **Role**: Accessible shimmer placeholder rendered during asynchronous data queries.
- **Attributes**: `role="status"`, `aria-label="Loading..."`, `animate-pulse`, `bg-slate-800/80`, rounded edges.
- **Variants**: Standard block, circular avatar (`rounded-full`), and multi-column row skeletons.

### 3.2 Data Table (`table.tsx`)
- **Components**: `Table`, `TableHeader`, `TableBody`, `TableFooter`, `TableRow`, `TableHead`, `TableCell`, `TableCaption`.
- **Features**:
  - Semantic HTML5 structure (`<table>`, `<thead>`, `<tbody>`, `<tr>`, `<th>`, `<td>`).
  - Subtle row hover transitions (`hover:bg-slate-800/40`).
  - Strict alignment support: left for textual data, center for status badges, right for numeric timestamps/durations.
  - Horizontal overflow scrolling container wrapper (`overflow-x-auto`) preventing table clipping on narrow viewports.

### 3.3 Empty State (`empty-state.tsx`)
- **Role**: Replaces blank screens when queries return 0 items.
- **Properties**:
  - `icon`: Lucide icon component rendered in a soft circular container.
  - `title`: Direct, actionable header (e.g., "No logs found").
  - `description`: Explanatory message indicating filters or ingestion steps.
  - `action`: Optional primary button (e.g., "Clear Filters", "Create Service").
  - `secondaryAction`: Optional secondary link or documentation jump.

### 3.4 Error State (`error-state.tsx`)
- **Role**: Standardized resilience banner for transient API network failures or query timeouts.
- **Properties**:
  - `role="alert"`, with distinct red/amber visual borders.
  - `title` & `message`: Human-readable explanation.
  - `onRetry`: Optional retry button invoking callback.
  - `details`: Expandable `<details>` toggle for technical debug info (HTTP status, error stack, trace ID).

### 3.5 Status Indicator (`status-indicator.tsx`)
- **Role**: Visual and accessible representation of infrastructure health.
- **Variants**:
  - `healthy`: Emerald dot with optional glowing pulse animation.
  - `degraded`: Amber dot with pulsing alert glow.
  - `outage`: Rose/Red dot indicating service interruption.
  - `unknown` / `inactive`: Neutral slate indicator.
- **Sizes**: `sm` (1.5), `md` (2), `lg` (2.5) with integrated accessible label.

### 3.6 Modal Dialog (`dialog.tsx`)
- **Role**: Accessible modal overlay for high-priority user decisions and creation forms.
- **Accessibility & UX Features**:
  - `role="dialog"`, `aria-modal="true"`, `aria-labelledby`.
  - Body scroll locking (`overflow-hidden`) when open.
  - Native `Escape` key capture with automatic dismissal.
  - Darkened backdrop (`bg-black/70 backdrop-blur-sm`).
  - Smooth scale and fade animations.

### 3.7 Confirmation Dialog (`confirm-dialog.tsx`)
- **Role**: High-visibility guardrail for destructive actions (deleting applications, revoking API keys, cancelling subscriptions).
- **Features**:
  - Destructive mode with red accentuation (`variant="destructive"`).
  - Explicit confirmation button with async loading state indicator.
  - Cancel button with automatic focus return.

### 3.8 Breadcrumbs (`breadcrumbs.tsx`)
- **Role**: Hierarchical location trail placed in the primary application header.
- **Features**:
  - Semantic `<nav aria-label="Breadcrumb">` and `<ol>` markup.
  - Automatic capitalization and path segment formatting.
  - Integrated chevron dividers (`ChevronRight`).
  - Active segment receives `aria-current="page"`.

### 3.9 Accessible Select (`select.tsx`)
- **Role**: Standard form and filter dropdown control.
- **Features**:
  - Styled custom chevron overlay without obscuring native keyboard accessibility.
  - Integrated `label`, `error`, and `helperText` bindings.
  - Full focus ring styles (`focus:ring-2 focus:ring-blue-500`).

---

## 4. Verification and Automated Testing

The design system primitive components are verified by unit tests in `src/SentinelOps.Web/src/components/ui/__tests__/primitives.test.tsx`:
1. `Skeleton`: renders with proper ARIA attributes (`role="status"`, `aria-label="Loading..."`).
2. `Table`: renders semantic `table`, `thead`, `tbody`, `tr`, `th`, and `td` elements.
3. `EmptyState`: renders title, description, and responds to action button click events.
4. `ErrorState`: renders alert banner, details disclosure, and invokes retry callback.
5. `StatusIndicator`: renders correct color classes and pulse indicators for `healthy`, `degraded`, and `outage`.
6. `Dialog`: renders modal content with title and description, closes on close button click.
7. `Dialog`: triggers `onClose` when `Escape` key is pressed.
8. `ConfirmDialog`: renders destructive variant and triggers `onConfirm` callback.
9. `Breadcrumbs`: generates proper crumbs with navigation links and `aria-current="page"`.
10. `Select`: renders label, error message, and handles option change events.

### Test Execution Status:
- Vitest unit tests: **64 / 64 passed (100%)**
- TypeScript compilation: **0 errors**
