# SentinelOps Enterprise Design System — Concept A: Precision Dark

## 1. Overview & Architectural Philosophy

SentinelOps is an enterprise-grade observability and telemetry platform designed for Site Reliability Engineers (SREs), DevOps architects, and infrastructure leads. 

The **Precision Dark** design system translates the mental model of high-performance developer workstations (Linear, modern Datadog, Apple Pro engineering consoles) into an ultra-fast, surgical web interface.

### Core Principles
1. **The Engineer's Canvas (Obsidian Layering)**: Deep void canvas (`#08090D`) with layered dark charcoal surfaces (`#0E1017`, `#10121A`, `#141722`) rather than generic muddy blue/slate.
2. **Hairline Structural Precision**: Clean 1px division rules (`#1E2333`) replace floating drop shadows and blur gradients.
3. **Cold Electric Cyan Accent (`#00E5FF`)**: High-visibility, crisp signal color that provides surgical focus without the generic purple/indigo AI cliché.
4. **Information Density & Scannability**: Tight horizontal rhythm, compact 4px micro-radii, monospace tabular telemetry figures (`tabular-nums`), and low-fatigue contrast ratios.
5. **Multi-Modal Accessibility (WCAG AAA/AA)**: Every status and alert pairs distinct hues with geometric indicators (diamonds, triangles, circles, chevrons) to ensure full accessibility for color-blind operators.

---

## 2. Color Palette & Semantic Tokens

### Canvas & Surface Hierarchy
| Token | Hex | HSL | Semantic Role | WCAG Contrast |
| :--- | :--- | :--- | :--- | :--- |
| `void` / `canvas` | `#08090D` | `hsl(228, 24%, 4.1%)` | Deepest root canvas, background behind layouts | Baseline (1.0:1) |
| `surface-0` | `#0E1017` | `hsl(227, 24%, 7.1%)` | Sidebar, top header, status ribbons, filter bars | Layer 1 |
| `surface-1` / `card` | `#10121A` | `hsl(228, 24%, 8.2%)` | Cards, metric panels, data tables, inspectors | Layer 2 |
| `surface-elevated` | `#141722` | `hsl(227, 26%, 10.6%)` | Modals, popovers, dropdown menus, hover states | Layer 3 |
| `surface-highlight` | `#1A1E2C` | `hsl(227, 26%, 13.7%)` | Active row selection, pressed button state | Layer 4 |
| `border-hairline` | `#1E2333` | `hsl(226, 26%, 15.9%)` | Primary 1px structural container and divider line | AA UI Element |
| `border-subtle` | `#2A324B` | `hsl(226, 28%, 23.1%)` | Secondary borders, active tab indicators, hover borders | AA UI Element |

### Primary Brand & Interactive
| Token | Hex | HSL | Semantic Role | Notes |
| :--- | :--- | :--- | :--- | :--- |
| `primary` / `cyan-400` | `#00E5FF` | `hsl(186, 100%, 50%)` | Primary interactive buttons, active indicators, focus rings | Cold Electric Cyan |
| `primary-hover` | `#33EBFF` | `hsl(186, 100%, 60%)` | Interactive hover state | High clarity |
| `primary-active` | `#00B4CC` | `hsl(186, 100%, 40%)` | Interactive pressed state | Deepened cyan |
| `primary-foreground` | `#08090D` | `hsl(228, 24%, 4.1%)` | Text/icons on primary cyan background | 13.8:1 (AAA) |
| `primary-glow` | `rgba(0, 229, 255, 0.12)` | - | Subtle focused highlight rim | Non-distracting |

### Typography & Readability
| Token | Hex | HSL | Semantic Role | Contrast on `#08090D` |
| :--- | :--- | :--- | :--- | :--- |
| `text-primary` | `#F0F3F9` | `hsl(220, 47%, 96%)` | Headers, metric values, primary labels | 17.2:1 (AAA) |
| `text-secondary` | `#A6B4C9` | `hsl(217, 26%, 72%)` | Field titles, descriptions, tab labels | 9.8:1 (AAA) |
| `text-muted` | `#68778D` | `hsl(215, 15%, 48%)` | Timestamps, table column headers, helper text | 4.8:1 (AA) |
| `text-subtle` | `#414D60` | `hsl(217, 19%, 31%)` | Inactive pagination, watermarks, disabled icons | 2.5:1 |

### Operational Telemetry Status Tokens
| State | Color (Hex) | HSL | Shape / Cue | Usage |
| :--- | :--- | :--- | :--- | :--- |
| **Healthy / Normal** | `#34D399` (Emerald 400) | `hsl(158, 64%, 52%)` | Solid Circle (`●`), Checkmark | Services up, SLO within budget, tests passing |
| **Warning / Degraded** | `#FBBF24` (Amber 400) | `hsl(42, 96%, 56%)` | Filled Triangle (`▲`), Exclamation | Elevated latency, high memory pressure, P2 alerts |
| **Critical / Incident** | `#F43F5E` (Rose 500) | `hsl(347, 89%, 60%)` | Filled Diamond (`◆`), Octagon | Service outage, fatal errors, P1/P0 firing incidents |
| **Info / Telemetry** | `#00E5FF` (Electric Cyan) | `hsl(186, 100%, 50%)` | Square (`■`), Pulse Beacon | Trace span, live stream ingestion, AI diagnostic insight |
| **Unknown / Neutral** | `#64748B` (Slate 500) | `hsl(215, 16%, 47%)` | Hollow Ring (`○`), Dash | Unmonitored, pending evaluation, maintenance mode |

---

## 3. Typography Scale & Conventions

### Font Stacks
- **UI Font**: `Inter, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif`
  - High legibility, crisp rendering at 11px–14px, tightened tracking (`-0.015em`).
- **Telemetry & Monospace**: `"JetBrains Mono", "Fira Code", ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace`
  - Strict tabular numbers (`font-variant-numeric: tabular-nums`).
  - Used for: Trace IDs, Span IDs, timestamps, HTTP status codes, latencies, memory/CPU figures, JSON attributes, SQL queries, log payloads.

### Scale & Hierarchy
| Level | Font Size | Line Height | Tracking | Weight | Target Usage |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Display / Metric** | `28px` (`1.75rem`) | `32px` (`2.0rem`) | `-0.03em` | 700 Bold | Big metric readout, dashboard hero indicators |
| **Heading 1** | `20px` (`1.25rem`) | `26px` (`1.625rem`) | `-0.02em` | 600 SemiBold | Page titles (e.g., "Log Explorer", "Incident #INC-402") |
| **Heading 2** | `15px` (`0.9375rem`)| `20px` (`1.25rem`) | `-0.01em` | 600 SemiBold | Panel & card titles, modal headers |
| **Heading 3 / Sub**| `13px` (`0.8125rem`)| `18px` (`1.125rem`) | `0.02em` | 500 Medium | Table headers, section sub-headers, uppercase |
| **Body (Default)** | `13px` (`0.8125rem`)| `19px` (`1.1875rem`) | `normal` | 400 Regular | Standard UI text, table cell contents, form labels |
| **Body Small** | `12px` (`0.75rem`) | `16px` (`1.0rem`) | `normal` | 400 Regular | Form helper text, secondary badges, timeline notes |
| **Micro / Code** | `11px` (`0.6875rem`)| `15px` (`0.9375rem`) | `0.04em` | 500 Medium | Monospace timestamps, badges, HTTP methods, tags |

---

## 4. Spacing, Geometry & Elevation

### Spacing Scale
- SentinelOps adheres to a strict 4px grid:
  - `space-1`: 4px (micro gaps, badge paddings)
  - `space-2`: 8px (icon-to-text gaps, compact row paddings)
  - `space-3`: 12px (standard container padding, input padding)
  - `space-4`: 16px (card inner padding, panel headers)
  - `space-5`: 20px (grid column gaps)
  - `space-6`: 24px (major section gutters)
  - `space-8`: 32px (top page margins)

### Geometry & Corner Radii
- All primary containers, inputs, buttons, and cards use **`4px` (`rounded`)** corner radius.
- Modals, drawers, and popovers use **`6px` (`rounded-md`)**.
- Status dots and avatar badges use **`9999px` (`rounded-full`)**.
- Bulky `rounded-xl` and `rounded-2xl` radii are strictly prohibited in the precision dark theme to preserve technical compactness.

### Elevation & Borders
- Rather than fuzzy multi-layered box-shadows that wash out on dark screens, elevation is communicated through **fill luminosity layering** + **hairline borders**:
  - `Level 0 (Canvas)`: `#08090D`, no border
  - `Level 1 (Panels)`: `#10121A`, `border 1px solid #1E2333`
  - `Level 2 (Popovers / Modals)`: `#141722`, `border 1px solid #2A324B`, `shadow: 0 12px 24px -4px rgba(0,0,0,0.6)`
  - `Level 3 (Tooltips)`: `#0E1017`, `border 1px solid #33EBFF33`

---

## 5. Component Primitives Specification

### 1. Buttons (`<Button />`)
- **Primary**: Background `#00E5FF`, Text `#08090D`, Font Medium 13px, 4px radius, hover brightness 105%, active `#00B4CC`. Focus ring `2px #00E5FF` offset 2px.
- **Secondary / Outline**: Background `#141722`, Border `#1E2333`, Text `#F0F3F9`, hover background `#1A1E2C`, hover border `#2A324B`.
- **Ghost**: Background transparent, Text `#A6B4C9`, hover text `#F0F3F9`, hover background `#141722`.
- **Danger**: Background `#F43F5E`, Text `#FFFFFF`, hover background `#E11D48`.
- **Sizes**:
  - `sm`: Height 28px, px 10px, text 12px
  - `default`: Height 34px, px 14px, text 13px
  - `lg`: Height 40px, px 18px, text 14px
  - `icon`: Height 32px, width 32px, centered SVG

### 2. Badges & Pills (`<Badge />`)
- Micro-pill format with 11px monospace or tight sans typography.
- Structure: `[Icon/Indicator Shape] + [Label Text]`.
- Variants:
  - `default`: bg `#141722`, border `#1E2333`, text `#F0F3F9`
  - `cyan` / `info`: bg `rgba(0, 229, 255, 0.08)`, border `rgba(0, 229, 255, 0.3)`, text `#00E5FF`
  - `success`: bg `rgba(52, 211, 153, 0.08)`, border `rgba(52, 211, 153, 0.3)`, text `#34D399`
  - `warning`: bg `rgba(251, 191, 36, 0.08)`, border `rgba(251, 191, 36, 0.3)`, text `#FBBF24`
  - `destructive`: bg `rgba(244, 63, 94, 0.08)`, border `rgba(244, 63, 94, 0.3)`, text `#F43F5E`

### 3. Data Tables (`<Table />`)
- Headers: Height 32px, bg `#0E1017`, border-bottom 1px `#1E2333`, text 11px uppercase tracking-wider text `#68778D`, font-mono.
- Rows: Height 38px, border-bottom 1px `#10121A`, hover bg `#141722`.
- Alternating subtle rows or clean single-pixel hairline rows.
- Zero horizontal overflowing without scroll containment.

### 4. Input Fields (`<Input />`, `<Select />`)
- Background: `#0A0C11`, Border: 1px `#1E2333`, Text: `#F0F3F9`, Placeholder: `#414D60`.
- Focus state: Border color `#00E5FF`, subtle box-shadow `0 0 0 1px #00E5FF`.
- Height: 34px, padding 8px 12px, font-size 13px, radius 4px.

### 5. Metric Stat Ribbons (`<MetricTile />` / `<MetricRibbon />`)
- Replaces bulky generic cards.
- Layout: Horizontal strip or compact 4-column module grid.
- Contains:
  - Label in 11px uppercase monospace text `#68778D`
  - Numeric value in 22px bold tabular-nums `#F0F3F9`
  - Trend delta indicator (`+2.4%` in `#34D399` or `#F43F5E`)
  - Micro sparkline or health beacon

---

## 6. Implementation Checklist & Verification Gates
- [x] Concept A selected and approved by stakeholder.
- [x] Complete tokens defined in `DESIGN-SYSTEM.md`.
- [ ] CSS variables updated in `src/index.css`.
- [ ] Tailwind utility classes synchronized.
- [ ] Primitive components updated (`card`, `button`, `badge`, `input`, `table`, etc.).
- [ ] Component unit tests verified.
