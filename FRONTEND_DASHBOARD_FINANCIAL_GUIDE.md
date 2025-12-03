# Front-End Guide: Dashboard Financial Metrics API

This note explains how to consume the dashboard overview endpoint so the new financial cards and chart render correctly without UI code changes.

## Endpoint
- **Method / Route:** `GET /api/Dashboard/overview`
- **Authentication:** Bearer JWT (same token used elsewhere).
- **Query (optional):** `startDate`, `endDate` (ISO) or `range` label. The backend auto-swaps inverted dates and echoes `range` back as `rangeLabel`.

## Response Shape
Common envelope:
```json
{
  "isSuccess": true,
  "data": {
    "role": "Admin | BranchManager | Supervisor | Teacher",
    "rangeStart": "2024-01-01T00:00:00Z",
    "rangeEnd": "2024-01-31T23:59:59.9999999Z",
    "rangeLabel": "Last 30 Days",
    "metrics": { /* see below */ },
    "charts": { /* see below */ }
  },
  "errors": null
}
```

## Metrics Contract
Key names match the dashboard cards. Missing fields serialize as `null`; the UI can hide those cards.

| Field | Type | Notes |
| --- | --- | --- |
| `currencyCode` | string | Default fallback when a currency-specific code is missing. Currently `"EGP"`. |
| `outgoing` / `outgoingCurrencyCode` | decimal / string | Sum of teacher + manager payouts for the range. |
| `incomingEgp` / `incomingEgpCurrencyCode` | decimal / string | Payments tagged as EGP. |
| `incomingSar` / `incomingSarCurrencyCode` | decimal / string | Payments tagged as SAR. |
| `incomingUsd` / `incomingUsdCurrencyCode` | decimal / string | Payments tagged as USD. |
| `netProfit` / `netProfitCurrencyCode` | decimal / string | `incoming total - outgoing` for the range. Uses fallback when null. |
| `earnings` / `earningsCurrencyCode` | decimal / string | Legacy card, same as incoming total. |
| `earningsPercentChange` | decimal | Change vs. previous range. |
| `netIncome` / `netIncomeCurrencyCode` | decimal / string | Legacy net figure; still returned for compatibility. |
| `netIncomePercentChange` | decimal | Change vs. previous range. |
| `newStudents` / `newStudentsPercentChange` | int / decimal | Count and delta vs. previous range. |
| `circleReports` / `circleReportsPercentChange` | int / decimal | Count and delta vs. previous range. |
| `branchManagersCount`, `supervisorsCount`, `teachersCount`, `studentsCount`, `circlesCount`, `reportsCount` | int | Role-scoped totals; some omit for narrower roles. |

### Percent Change Formula
All `%` fields compare the current range to the immediately preceding range of the same length.

## Charts Contract
- `charts.monthlyRevenue`: array of `{ month, earnings, teacherPayout, managerPayout, netIncome }` already rounded for display.
- `charts.projectOverview`: aggregate counts for circles, teachers, students, and reports (role-scoped).
- `charts.transactions`: up to 10 latest student payments with `id`, `student`, `amount`, `currency`, `date`, `status`.

## Rendering Tips
1. Prefer the currency-specific codes; fall back to `currencyCode` when any code is missing.
2. Treat `null` numeric fields as `0` in the UI to match current behavior.
3. When the user switches date presets, pass the matching `range` so the backend echoes it in `rangeLabel` for highlighting.
4. Use `rangeStart`/`rangeEnd` to show the active window to the user; they are returned in UTC.
