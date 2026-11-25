# FleetManagement — online demo (second vertical slice)

The original WPF project remains unchanged. The browser application uses the original XAML's lavender background (#F0F0FF), purple buttons (#6A5ACD, #7B68EE, #9370DB), indigo headings (#4B0082), white panels and alternating grid rows (#EEEAF8). No new brand name was introduced.

## Run locally
Install the .NET 10 SDK.
```sh
dotnet run --project src/FleetManagement.Web
dotnet run --project tests/FleetManagement.Core.Checks
dotnet publish src/FleetManagement.Web -c Release -o publish
```
The published static content is under publish/wwwroot. The relative base URL supports a repository subpath. No API, SQL Server, keys or paid hosting is required for the demo. GitHub Pages needs to be configured separately.

## Demo capabilities
- Overview and editable vehicle/driver registers with search, sorting, validation, and protected trip history.
- Interactive tracking on a schematic route network; simulated clock and progress, not GPS. Custom trips do not have map geometry.
- Dispatch recommendation, overlapping vehicle/driver conflict detection, trip creation.
- Explainable service interval assessment and recording service.
- Browser local storage for register, trip and service changes, and reset.
- C# business rules separated from UI and executable regression checks.

## Boundaries / next steps
The desktop app, SQL schema and EF6 remain unchanged; its known authentication, persistence and legacy test issues are not resolved by this prototype. The public demo has no authentication or production data, uses simulated tracking instead of SignalR, and maintenance uses deterministic rules instead of a trained ML model. Next: production API/EF Core/Identity, migration, real integration tests, desktop MVVM refactoring, trip editing and accessibility/browser QA.
