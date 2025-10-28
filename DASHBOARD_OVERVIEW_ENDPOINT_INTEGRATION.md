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
  "data": {
    "role": "Admin | BranchManager | Supervisor | Teacher",
    "rangeStart": "2024-05-01T00:00:00Z",
    "rangeEnd": "2024-05-30T23:59:59.9999999Z",
    "rangeLabel": "Last 30 Days",
    "metrics": {
      "earnings": 12500.75,
      "newStudents": 42,
      "circleReports": 58,
      "netIncome": 8300.10,
      "branchManagersCount": 7,
      "supervisorsCount": 18,
      "teachersCount": 145,
      "studentsCount": 2100,
      "circlesCount": 320,
      "reportsCount": 1590
    },
    "charts": {
      "monthlyRevenue": [
        {
          "month": "Jan 2024",
          "earnings": 9800.50,
          "teacherPayout": 2100.00,
          "managerPayout": 900.00,
          "netIncome": 6800.50
        },
        {
          "month": "Feb 2024",
          "earnings": 11250.25,
          "teacherPayout": 2500.00,
          "managerPayout": 1000.00,
          "netIncome": 7750.25
        }
      ],
      "projectOverview": {
        "totalCircles": 320,
        "activeCircles": 245,
        "teachers": 145,
        "students": 2100,
        "reports": 1590
      },
      "transactions": [
        {
          "id": 5812,
          "student": "Omar Ali",
          "amount": 250.00,
          "currency": "SAR",
          "date": "2024-05-29T16:12:00Z",
          "status": "Paid"
        },
        {
          "id": 5804,
          "student": "Student #5804",
          "amount": 150.00,
          "currency": "EGP",
          "date": "2024-05-28T08:45:00Z",
          "status": "Pending"
        }
      ]
    }
  },
  "errors": null
}
```

### Metrics by Role
| Role | What you get |
| --- | --- |
| **Admin** | Global counts for branch managers, supervisors, teachers, students, circles, reports + monetary metrics for the selected window. |
| **BranchManager** | All metrics scoped to the manager's branch only. |
| **Supervisor** | Counts/financials limited to the supervisor's assigned teachers, students, circles, and their reports. `branchManagersCount` is omitted. |
| **Teacher** | Teacher-specific counts for their circles, students, and submitted reports. Only their own earnings/net income are returned. |

Missing/irrelevant fields are serialized as `null` so the UI can safely hide unused widgets.

### Transactions Feed
- Maximum of 10 most recent student payments for the visible scope.
- `currency` maps to `EGP`, `SAR`, or `USD`; if an unknown ID arrives you'll get `"N/A"`.
- `status` is either `"Paid"` (`payStatue == true`) or `"Pending"`.

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
