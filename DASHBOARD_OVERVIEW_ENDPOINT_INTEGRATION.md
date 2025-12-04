# Front-End Integration Notes: Dashboard Overview API

Hi team,

Here's how to wire the dashboard widgets to the new role-aware backend endpoint.

## 1. Authentication
The endpoint requires the standard JWT that the app already stores. Always send the bearer token header:

```
Authorization: Bearer <JWT token>
```

If the token is missing/expired the API responds with **401 Unauthorized** before any business logic runs.

## 2. Endpoint Overview
- **Method / Route:** `GET /api/Dashboard/overview`
- **Query parameters (`DashboardRangeInputDto`, all optional):**
  | Name | Type | Description |
  | --- | --- | --- |
  | `startDate` | ISO date string | Lower bound (UTC) for the reporting window. Defaults to `now - 29 days` when omitted. |
  | `endDate` | ISO date string | Upper bound (UTC) for the reporting window. Defaults to `now`. |
  | `range` | string | Free-form label (e.g., `"Last 30 Days"`). Echoed back as `rangeLabel` to help the UI choose which preset badge to highlight. |

> ⚠️ If `startDate > endDate`, the backend will silently swap them so you don't need to sanitize that edge case client-side.

## 3. Success Response
The payload adapts to the caller's role. Common envelope:

```json
{
  "isSuccess": true,
  "errors": [],
  "data": {
    "role": "Admin",
    "rangeStart": "2025-11-05T00:00:00Z",
    "rangeEnd": "2025-12-04T23:59:59.9999999Z",
    "rangeLabel": null,
    "metrics": {
      "currencyCode": "SAR",
      "earnings": 70,
      "earningsCurrencyCode": "SAR",
      "earningsPercentChange": null,
      "newStudents": 28,
      "newStudentsPercentChange": null,
      "circleReports": 2,
      "circleReportsPercentChange": null,
      "netIncome": 70,
      "netIncomeCurrencyCode": "SAR",
      "netIncomePercentChange": null,
      "branchManagersCount": 2,
      "supervisorsCount": 10,
      "teachersCount": 108,
      "studentsCount": 1085,
      "circlesCount": 113,
      "reportsCount": 2,
      "outgoing": 0,
      "incomingEgp": 0,
      "incomingSar": 70,
      "incomingUsd": 0,
      "netProfit": 70
    },
    "charts": {
      "monthlyRevenue": [
        {"month": "Jul 2025", "earnings": 0, "teacherPayout": 0, "managerPayout": 0, "netIncome": 0},
        {"month": "Aug 2025", "earnings": 0, "teacherPayout": 0, "managerPayout": 0, "netIncome": 0},
        {"month": "Sep 2025", "earnings": 0, "teacherPayout": 0, "managerPayout": 0, "netIncome": 0},
        {"month": "Oct 2025", "earnings": 0, "teacherPayout": 0, "managerPayout": 0, "netIncome": 0},
        {"month": "Nov 2025", "earnings": 0, "teacherPayout": 0, "managerPayout": 0, "netIncome": 0},
        {"month": "Dec 2025", "earnings": 70, "teacherPayout": 0, "managerPayout": 0, "netIncome": 70}
      ],
      "projectOverview": {
        "totalCircles": 113,
        "activeCircles": 2,
        "teachers": 108,
        "students": 1085,
        "reports": 2
      },
      "transactions": [
        {
          "id": 2639,
          "student": "حسناء إبراهيم بدر",
          "amount": 70,
          "currency": "SAR",
          "date": "2025-12-04T01:58:37.863",
          "status": "Paid"
        }
      ]
    }
  }
}
```

### What each section means (front-end binding)
- **Top-level envelope:** `isSuccess` + `errors` + `data` keeps the API contract consistent with other endpoints.
- **Range metadata:** `rangeStart`/`rangeEnd` are ISO-8601 and build the header subtitle. `rangeLabel` echoes any preset name passed as `range`.
- **Role label:** `role` is an English role key (Admin/BranchManager/Supervisor/Teacher/...) that the UI translates to an Arabic chip.

#### Metrics contract
These keys are referenced directly by the dashboard cards and financial chart:

| Key | Purpose |
| --- | --- |
| `earnings` + `earningsCurrencyCode` + `earningsPercentChange` | Main revenue card (value + change arrow). |
| `newStudents` + `newStudentsPercentChange` | New students card. |
| `circleReports` + `circleReportsPercentChange` | Circle reports card. |
| `netIncome` + `netIncomeCurrencyCode` + `netIncomePercentChange` | Net income card. |
| `branchManagersCount`, `supervisorsCount`, `teachersCount`, `studentsCount`, `circlesCount`, `reportsCount` | Role-scoped counters; hidden if `null`. |
| `outgoing` + `outgoingCurrencyCode` | Outgoing bar on the financial chart. |
| `incomingEgp`, `incomingSar`, `incomingUsd` (+ matching currency codes) | Incoming bars per currency. |
| `netProfit` + `netProfitCurrencyCode` (or `netIncomeCurrencyCode`) | Net profit bar. |
| `currencyCode` | Default currency fallback if a specific code is missing. |

> نسبة التغير يمكن أن تعاد كرقم (`-5.2`) أو نص (`"+5%"`)، والواجهة تلون السهم بناءً على الإشارة فقط.

#### Charts contract
- `monthlyRevenue`: array of points with `month`, `earnings`, `teacherPayout`, `managerPayout`, `netIncome`. Any series disappears if all its values are `null`.
- `projectOverview`: `totalCircles`, `activeCircles`, `teachers`, `students`, `reports`; any `null` entry hides its widget.
- `transactions`: up to 10 recent student payments. Each item exposes `id`, `student`, `amount`, `currency`, `date` (ISO), and `status` (`paid`, `pending`, `failed`, or `cancelled`).

### Metrics by Role
| Role | What you get |
| --- | --- |
| **Admin** | Global counts for branch managers, supervisors, teachers, students, circles, reports + monetary metrics for the selected window. |
| **BranchManager** | All metrics scoped to the manager's branch only. |
| **Supervisor** | Counts/financials limited to the supervisor's assigned teachers, students, circles, and their reports. `branchManagersCount` is omitted. |
| **Teacher** | Teacher-specific counts for their circles, students, and submitted reports. Only their own earnings/net income are returned. |

Missing/irrelevant fields are serialized as `null` so the UI can safely hide unused widgets.

## 4. Wiring Tips
1. **Dashboard cards** (`earnings`, `newStudents`, `circleReports`, `netIncome`): bind directly to `data.metrics` using the same keys already expected in the UI.
2. **Role-aware hiding:** if a value comes back `null`, hide or gray out that widget (e.g., supervisors won't have `branchManagersCount`).
3. **Charts:**
   - `monthlyRevenue` already contains rounded currency values for stacked/line charts.
   - `projectOverview` is a simple aggregate block that can be displayed as counters or progress bars.
   - `transactions` fits neatly into the existing table; format `date` using the user's locale.
4. **Date pickers / presets:** when the user selects a custom window, send `startDate` and `endDate`. When a preset is chosen (e.g., "This Month"), also pass the matching `range` string so it echoes back and you can highlight the active filter without extra state.

## 5. Error Handling
- On validation/business issues the API responds with `isSuccess = false` and a populated `errors` array. Surface those messages inline or via toast.
- Standard HTTP errors (401/403/500) follow the API's existing global error contract.

Ping me if you need mock data or sample hooks!
