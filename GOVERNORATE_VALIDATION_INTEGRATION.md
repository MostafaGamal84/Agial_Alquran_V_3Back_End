# Governorate requirement for Egyptian nationality

This backend release introduces a conditional validation that requires a governorate to be supplied whenever a user is created or updated with the Egyptian nationality.

## API behaviour

- The validation is enforced on the following endpoints:
  - `POST /api/User/Create`
  - `POST /api/User/Update`
  - `PUT  /api/User/Profile`
- When the nationality resolves to Egypt (detected by `TelCode = 20` or the name containing "Egypt"/"مصر") **and** the payload omits `GovernorateId`, the API now responds with:
  - `MessageCodes.InputValidationError`
  - `fieldName = "GovernorateId"`
  - `message = "يجب اختيار المحافظة عند اختيار الجنسية المصرية"`

## Front-end integration guidance

1. Ensure nationality selectors provide both the `id` and the `telCode` (or the localized name) so the client can determine whether the chosen item represents Egypt.
2. When Egypt is selected:
   - Require users to pick a governorate before submitting the form.
   - Prevent submission or display inline validation referencing the Arabic message above.
3. Non-Egyptian nationalities may submit without a governorate as before.
4. If the client sends a governorate id of `0` or `null` for Egyptian users, the API will reject the request, so keep client-side validation in sync.

Adapting the UI accordingly will avoid new validation errors and keeps the UX consistent with the backend rules.
