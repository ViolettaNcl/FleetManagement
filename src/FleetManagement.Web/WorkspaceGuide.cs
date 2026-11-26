namespace FleetManagement.Web;

internal sealed record WorkspaceGuide(string Section,string Title,string Description,string Hint,string Next,string Action);
internal static class WorkspaceGuides
{
    internal static readonly string[] Essentials=["Overview","Vehicles","Drivers","Dispatch","Calendar","Maintenance","Tracking","Analytics","Notifications","Help & settings"];
    private static readonly WorkspaceGuide[] All=[
        new("Overview","Daily workspace","Start with today’s vehicle availability, trips and alerts.","Choose a task below or review the alerts.","Dispatch","Plan a trip"),
        new("Vehicles","Fleet register","Register vehicles and review their service intervals.","Open a vehicle passport to see its history.","Vehicle Passport","View vehicle history"),
        new("Drivers","Driver register","Maintain driver profiles and licences.","Set working hours before allocating trips.","Resource Planning","Plan driver shifts"),
        new("Vehicle Passport","Vehicle history","Review one vehicle’s trips, mileage and maintenance records.","Check upcoming service before assigning a trip.","Maintenance","Review service"),
        new("Dispatch","Trip planning","Choose a route, dates, vehicle and driver. Save only after reviewing the assignment.","Use the assignment suggestion if you are unsure which resources are free.","Calendar","Review the calendar"),
        new("Batch Dispatch","Multiple trip requests","Prepare several requests and review proposed assignments before applying.","Review each rejected or conflicting request.","Dispatch","Plan an individual trip"),
        new("Calendar","Fleet calendar","See departures, shifts and reservations by date.","Select a date to review the agenda or create a trip.","Dispatch","Plan a trip"),
        new("Resource Planning","Driver shifts","Set working intervals and inspect trips without shift coverage.","Check the calendar for conflicting resource commitments.","Calendar","Review the calendar"),
        new("Maintenance","Vehicle servicing","Review remaining service mileage and dates, then record completed work.","Check vehicle history before assigning resources.","Vehicle Passport","View vehicle history"),
        new("Tracking","Trip simulation","Advance the demonstration clock to follow registered trips.","The moving markers are simulated, not live GPS.","Geo Map","View simulated routes"),
        new("Geo Map","Route map","Inspect simulated route positions and geographic areas.","Compare the movement with the trip schedule.","Tracking","Review trip simulation"),
        new("Analytics","Reports and activity","Choose a period to review completed trips, vehicle activity and export reports.","Use a report template for Excel or printable PDF.","Cost Intelligence","Review costs"),
        new("Cost Intelligence","Fleet costs","Review estimated expenses and compare budget scenarios.","These calculations use demo assumptions, not invoices.","Analytics","Open activity reports"),
        new("Assistant","Operational help","Ask supported questions about available vehicles, maintenance, trips or alerts.","Review the cited operational evidence before acting.","Notifications","Review alerts"),
        new("Optimization","Route comparison","Compare route algorithms against the same example stops.","These laboratory results are not live traffic instructions.","Dispatch","Plan a trip"),
        new("Multi Dispatch","Multi-vehicle routing","Compare simulated assignments with capacity and time constraints.","Review unassigned orders and route feasibility.","Batch Dispatch","Review trip requests"),
        new("Digital Twin","Vehicle simulation","Initialize a synthetic scenario and inspect simulated sensor readings.","Use the 3D view as an illustration, not a live vehicle reconstruction.","Predictions","Explore prediction tools"),
        new("What-if","Scenario comparison","Change assumptions and compare simulated outcomes without changing fleet records.","Review the baseline before interpreting a scenario.","Cost Intelligence","Review cost scenarios"),
        new("Intelligence","Maintenance models","Train and compare classifiers using isolated synthetic telemetry.","Compare test metrics before interpreting model scores.","Model Registry","Review saved experiments"),
        new("Predictions","Prediction laboratory","Compare arrival estimates and synthetic sensor anomaly models.","Read the evaluation limitations before relying on predictions.","Data Quality","Inspect training data"),
        new("Demand Forecast","Demand estimates","Explore forecast models using generated demand patterns.","Compare assumptions; forecasts are not confirmed bookings.","Analytics","Review activity reports"),
        new("Model Registry","Model experiment history","Inspect imported manifests, metrics and model artifact fingerprints.","Review training and test cohorts before comparing models.","Intelligence","Compare models"),
        new("Data Quality","Training data checks","Validate CSV telemetry and review errors before local model training.","Correct rejected rows before using the data.","Intelligence","Open model comparison"),
        new("Model Drift","Model monitoring","Compare synthetic reference and current feature distributions.","Investigate changes rather than automatically replacing a model.","Model Registry","Review saved experiments"),
        new("Notifications","Operational alerts","Review high-priority items and open the relevant record.","Use the review history to track previous local checks.","Maintenance","Review service"),
        new("Help & settings","Workspace settings","Choose language, appearance and navigation preferences; export or restore demo records.","Back up your demo before resetting or replacing data.","Overview","Return to overview"),
    ];
    internal static WorkspaceGuide For(string section)=>All.FirstOrDefault(g=>g.Section==section)??All[0];
    internal static bool IsEssential(string section)=>Essentials.Contains(section,StringComparer.Ordinal);
}
