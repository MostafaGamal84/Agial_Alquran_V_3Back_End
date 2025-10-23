# Front-End Integration Notes: User Profile API

Hi team,

The backend now exposes a pair of endpoints to retrieve and update the authenticated user's profile. Below is everything you need in order to hook them up on the front end.

## 1. Authentication
All profile operations are tied to the currently logged-in user. Make sure every request includes the existing Bearer token header:

```
Authorization: Bearer <JWT token from login>
```

## 2. View Profile
- **Method / Route:** `GET /api/User/Profile`
- **Alternative route (kept for backward compatibility):** `GET /api/User/GetProfile`
- **Response shape:**

```json
{
  "isSuccess": true,
  "messageCode": 1000,
  "data": {
    "id": 1,
    "fullName": "User Name",
    "email": "user@example.com",
    "mobile": "500000000",
    "secondMobile": null,
    "nationalityId": 4,
    "governorateId": 2,
    "branchId": 7
  }
}
```

`data` matches the `ProfileDto` contract. Use these fields to populate the profile screen form.

## 3. Update Profile
- **Method / Route:** `PUT /api/User/Profile`
- **Body contract (`UpdateProfileDto`):**

```json
{
  "fullName": "Updated Name",
  "email": "updated@example.com",
  "mobile": "512345678",
  "secondMobile": "598765432",
  "nationalityId": 4,
  "governorateId": 2,
  "branchId": 7
}
```

Only include the fields the user is allowed to edit. Empty strings/nulls clear the corresponding value.

- **Success response:**

```json
{
  "isSuccess": true,
  "messageCode": 1000,
  "data": true
}
```

- **Validation errors:**
  - Duplicate email → `messageCode` = `7008` (`EmailAlreadyExists`).
  - Duplicate mobile → `messageCode` = `7038` (`PhoneNumberAlreadyExisted`).

Handle these by showing the translated error message returned from the API.

## 4. UX Recommendations
1. **Initial load:** call `GET /api/User/Profile` after login (or when entering the profile page) and populate the form.
2. **Submit:** send `PUT /api/User/Profile` with the edited values. On success, refresh the local profile state with another `GET` call or reuse the submitted values.
3. **Form state:** disable the save button while the update call is in-flight to avoid double submissions.

Let me know if you need anything else!
