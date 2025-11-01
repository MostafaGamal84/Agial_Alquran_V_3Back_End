# Nationality Filters for Student-Facing Lists

The following endpoints now accept an optional `nationalityId` query parameter to
filter results by a student's nationality. Pass the numeric nationality identifier
returned by the lookup API (e.g. `/api/LookUp/GetAllNationality`).

| Page / Feature                    | Endpoint & Method                                        | Notes |
|----------------------------------|----------------------------------------------------------|-------|
| Online Course → Students list    | `GET /api/UsersForGroups/GetUsersForSelects`             | Pass the logged-in `UserTypeId` and include `nationalityId` when targeting students. |
| Online Course → Subscriptions    | `GET /api/StudentSubscrib/GetStudents`                   | Existing `studentId` filter still works. Combine with `nationalityId` to narrow the list. |
| Membership → Subscriptions list  | `GET /api/StudentSubscrib/GetStudentSubscribesWithPayment` | Filters the subscription records by the nationality of the linked student. |
| Reports → View report list       | `GET /api/CircleReport/GetResultsByFilter`               | Applies to the student on each report entry. |
| Finance → Invoices list          | `GET /api/StudentPayment/Invoices`                       | Works alongside the existing tab/date filters. |

## Usage

```http
GET /api/UsersForGroups/GetUsersForSelects?UserTypeId=5&managerId=0&teacherId=0&branchId=0&nationalityId=3
GET /api/StudentPayment/Invoices?tab=paid&nationalityId=3
GET /api/StudentSubscrib/GetStudents?nationalityId=7&studentId=0
GET /api/CircleReport/GetResultsByFilter?circleId=12&nationalityId=5
```

If `nationalityId` is omitted or set to `0`, all nationalities are returned (current
behaviour). When the parameter is present, only students whose `NationalityId`
matches the supplied value will be returned.

## Front-End Integration Checklist

1. Load the list of nationalities (if needed) using the existing lookup endpoint.
2. Add a select/drop-down for nationalities on the relevant pages.
3. When calling any of the endpoints above, append `&nationalityId=<selectedValue>`.
4. Clear the parameter (omit it) when the user chooses “All”.
5. No response schema changes were introduced—only the filtering behaviour is updated.

