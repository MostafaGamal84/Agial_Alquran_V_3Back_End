# Frontend Integration Guide: Deleted Objects Page (Tabs)

This guide explains how to build a **Deleted Objects** page for admins using the new backend endpoints.

## Goal
Create one page that shows soft-deleted data in tabs:
- Students
- Teachers
- Managers
- Branch Leaders
- Circles
- Circle Reports

Each tab should load paged data and support searching.

---

## API Endpoints

Use these endpoints (all `GET`):

### Users (by role)
- `/api/UsersForGroups/DeletedStudents`
- `/api/UsersForGroups/DeletedTeachers`
- `/api/UsersForGroups/DeletedManagers`
- `/api/UsersForGroups/DeletedBranchLeaders`

### Circles
- `/api/Circle/Deleted`

### Circle Reports
- `/api/CircleReport/Deleted`

---

## Query Parameters (Paging/Search)

All endpoints accept `FilteredResultRequestDto` through query string.

At minimum, send:
- `skipCount` (number, default `0`)
- `maxResultCount` (number, e.g. `10` or `20`)
- `searchTerm` (string, optional)

Optional if your app already uses them:
- `sortBy`
- `filter`
- `residentGroup`

### Example request

```http
GET /api/UsersForGroups/DeletedStudents?skipCount=0&maxResultCount=10&searchTerm=ahmed
```

---

## Expected Response Shape

All endpoints return the standard response wrapper:

```json
{
  "isSuccess": true,
  "message": null,
  "errors": [],
  "data": {
    "items": [ ... ],
    "totalCount": 123
  }
}
```

Use:
- `data.items` for table rows
- `data.totalCount` for pagination controls

---

## Suggested UI Structure

## Page: `Deleted Objects`

### Tabs
1. Deleted Students
2. Deleted Teachers
3. Deleted Managers
4. Deleted Branch Leaders
5. Deleted Circles
6. Deleted Circle Reports

### Per-tab content
- Search input
- Data table
- Pagination (`page`, `pageSize`, `totalCount`)
- Loading state (spinner/skeleton)
- Empty state (`No deleted records found`)
- Error state (`Failed to load data` + retry)

---

## Recommended Frontend Behavior

1. **Lazy load each tab**
   - Call API when tab opens first time.
2. **Preserve state per tab**
   - Keep each tab’s `searchTerm`, `page`, `pageSize`, and loaded rows.
3. **Debounce search**
   - 300–500ms debounce before API call.
4. **Reset page on new search**
   - When search changes, set `skipCount=0`.
5. **Use server pagination only**
   - Do not paginate in-memory across all records.

---

## Data Columns (Suggested)

Because DTOs differ per endpoint, start with safe/common columns and expand based on actual API payload.

### Deleted Users tabs (Students/Teachers/Managers/Branch Leaders)
Suggested columns:
- Id
- Full Name
- Mobile
- Email
- Nationality
- Governorate
- BranchId

### Deleted Circles
Suggested columns:
- Id
- Name
- Teacher (if present)
- BranchId
- Days summary (if available)

### Deleted Circle Reports
Suggested columns:
- Id
- Student name
- Teacher name
- Circle name
- Minutes (if available)
- Date/Creation time
- Notes/Other

> Tip: log first response in dev tools and map exact fields used by your table components.

---

## Suggested Frontend Service Layer

Create dedicated methods:

- `getDeletedStudents(params)`
- `getDeletedTeachers(params)`
- `getDeletedManagers(params)`
- `getDeletedBranchLeaders(params)`
- `getDeletedCircles(params)`
- `getDeletedCircleReports(params)`

Where `params` includes:

```ts
{
  skipCount: number;
  maxResultCount: number;
  searchTerm?: string;
  sortBy?: string;
  filter?: string;
  residentGroup?: string;
}
```

---

## Acceptance Checklist

- [ ] New page exists: **Deleted Objects**
- [ ] 6 tabs implemented
- [ ] Each tab calls the correct endpoint
- [ ] Server-side pagination works (`skipCount`, `maxResultCount`, `totalCount`)
- [ ] Search works with `searchTerm`
- [ ] Loading, empty, and error states implemented
- [ ] State preserved when switching tabs

---

## Notes

- These are **soft-deleted** records (`IsDeleted = true`).
- This guide is for **viewing** deleted data. Restore/permanent-delete actions are not included in this change.
