# Fleet economics and demand forecasting

## Cost estimates
The selected inclusive date range defines a departure cohort of completed trips as of the demo clock. Full trip distance and duration enter the estimate. Fuel litres = distance × assumed L/100 km / 100. Fuel cost = litres × price. Maintenance allowance = distance × rate. Driver cost = full trip hours × hourly rate. Decimal arithmetic, rounding only for display. Defaults (28 L/100 km, EUR 1.60/L, EUR 0.15/km, EUR 18/hour) are illustrative inputs, not local market prices. Same rates for every vehicle; excludes fixed costs, taxes, tolls, and actual receipts. No currency conversion, actual fuel sensor data or financial ledger.

Occupancy is a different metric: per vehicle, merge overlapping trip intervals clipped to the selected days and now. Divide occupied hours by registered vehicles × observed calendar hours. Includes active trips to now, excludes future time. No work shift, holiday, maintenance downtime or historical register membership adjustments. Null if zero denominator. This is recorded clock-time occupancy, not staffed fleet utilization.

## Demand experiment
Seeded series of 112 daily synthetic order counts with weekly effects, trend and bounded noise. First 84 days train both candidates; next 28 are a chronological holdout. Seasonal naive repeats the last training week. Regression fits a shared within-weekday least-squares slope and weekday-specific intercepts using training only. Both predict nonnegative counts. MAE measured on the holdout. Separate future forecasts (1–28 days) refit on all available history after evaluation; future outcomes unknown. Not trained on the register and not evidence of actual demand accuracy. Repeated test-driven tuning compromises independence. No external services.
