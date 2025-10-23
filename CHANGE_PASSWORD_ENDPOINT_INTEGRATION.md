# Front-End Integration Notes: Change Password API

Hi team,

Here's everything you need to hook up the new change-password flow on the front end.

## 1. Authentication
The endpoint is protected. Include the existing Bearer token with every request:

```
Authorization: Bearer <JWT token from login>
```

If the token is missing or expired the API will return a 401 before the action executes.

## 2. Endpoint Overview
- **Method / Route:** `POST /api/Account/ChangePassword`
- **Body contract (`ChangePasswordDto`):**

```json
{
  "currentPassword": "OldP@ssw0rd",
  "newPassword": "NewP@ssw0rd1",
  "confirmPassword": "NewP@ssw0rd1"
}
```

All three fields are required.

### Validation Rules
The backend enforces the following rules. Mirror them in the UI so users get instant feedback:
1. `currentPassword`, `newPassword`, and `confirmPassword` must be provided (non-empty).
2. `newPassword` must be at least 8 characters and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.
3. `confirmPassword` must match `newPassword` exactly.

If any rule fails, the API responds with `isSuccess = false` and the `errors` array will contain localized messages you can display directly.

## 3. Success Response
When the password is changed successfully the API returns:

```json
{
  "isSuccess": true,
  "data": "تم تغيير كلمة السر بنجاح",
  "errors": null
}
```

You can show the `data` message in a toast/snackbar and redirect the user as needed.

## 4. Error Handling
Key error codes/messages to handle:

| Scenario | Response shape |
| --- | --- |
| Current password is wrong | `errors[0].code = "7069"`, `errors[0].message = "كلمة المرور الحالية غير صحيحة"` |
| User account has no password yet | `errors[0].code = "7040"`, `errors[0].message = "حسابك لم يتم تفعيله بعد. يرجى التحقق من بريدك الإلكتروني وإنشاء كلمة مرور لتتمكن من تسجيل الدخول"` |
| Account inactive/deleted | `errors[0].code = "7015"`, `errors[0].message = "الحساب موقوف"` |
| Token references a user that no longer exists | `errors[0].code = "7002"`, message will indicate the user was not found |
| Generic validation issues | `errors` contains field-level messages (e.g., password mismatch, complexity violations) |

If you receive a validation error, highlight the offending field. For business errors (like invalid current password) show the localized message returned.

## 5. UX Recommendations
1. **Form state:** disable the submit button while awaiting the response to prevent double submissions.
2. **Password confirmation:** provide inline validation so users know when the confirm password matches.
3. **Post-success:** clear password inputs and, if appropriate, redirect the user back to profile settings.

Let me know if anything else is needed!
