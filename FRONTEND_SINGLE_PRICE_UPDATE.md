# Single-price subscription update

The backend now exposes a single `price` value for each subscription instead of the previous per-currency fields (`leprice`, `sarprice`, `usdprice`). Student invoices automatically pick a `CurrencyId` based on the subscription type group:

- Egyptian subscriptions → `CurrencyId = 1` (LE)
- Arab subscriptions → `CurrencyId = 2` (SAR)
- Foreign subscriptions → `CurrencyId = 3` (USD)

## Front-end actions
- Update subscription forms and views to read/write the `price` field only.
- Remove any bindings, validators, or labels that referenced `leprice`, `sarprice`, or `usdprice`.
- When showing invoices, rely on the `currencyId` coming from the API instead of inferring it from the old per-currency price fields.
- If the UI displays currency labels, map them using the IDs above.

Refer to the subscription DTOs in `OrbitsGeneralProject.DTO/SubscribeDtos` to see the updated shape returned by the API.
