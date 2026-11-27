using System.Text.RegularExpressions;
namespace FleetManagement.Web;
public static class AssetRegistry
{
    public static string? Module(string art) => art switch
    {
        "hero" => "assets/heroes/hero.webp",
        "command-center" => "assets/heroes/command-center.webp",
        "abstract" => "assets/heroes/abstract.webp",
        "vehicle-management" => "assets/operations/vehicle-management.webp",
        "drivers" => "assets/operations/drivers.webp",
        "dispatch" => "assets/operations/dispatch.webp",
        "calendar" => "assets/operations/calendar.webp",
        "tracking" => "assets/operations/tracking.webp",
        "twin" => "assets/operations/twin.webp",
        "maintenance" => "assets/operations/maintenance.webp",
        "routes" => "assets/operations/routes.webp",
        "resource-planning" => "assets/operations/resource-planning.webp",
        "vehicle-passport" => "assets/operations/vehicle-passport.webp",
        "notifications" => "assets/operations/notifications.webp",
        "assistant" => "assets/intelligence/assistant.webp",
        "machine-learning" => "assets/intelligence/machine-learning.webp",
        "predictions" => "assets/intelligence/predictions.webp",
        "demand-forecast" => "assets/intelligence/demand-forecast.webp",
        "model-drift" => "assets/intelligence/model-drift.webp",
        "model-registry" => "assets/intelligence/model-registry.webp",
        "analytics" => "assets/analytics/analytics.webp",
        "cost-intelligence" => "assets/analytics/cost-intelligence.webp",
        "reports" => "assets/analytics/reports.webp",
        _ => null
    };

    public static string Vehicle(string? model)
    {
        var name = Regex.Replace((model ?? "").ToLowerInvariant(), @"[^\p{L}\p{N}]+", " ").Trim();
        string key = name switch
        {
            _ when Regex.IsMatch(name, @"\b(mercedes|мерседес)\b") && Regex.IsMatch(name, @"\b(actros|актрос)\b") => "mercedes-actros",
            _ when Regex.IsMatch(name, @"\b(volvo|вольво)\b") && Regex.IsMatch(name, @"\b(fh\d*|фш)\b") => "volvo-fh",
            _ when Regex.IsMatch(name, @"\b(man|ман)\b") && Regex.IsMatch(name, @"\b(tgx|тгх)\b") => "man-tgx",
            _ when Regex.IsMatch(name, @"\b(ford|форд)\b") && Regex.IsMatch(name, @"\b(transit|транзит)\b") => "ford-transit",
            _ => "default"
        };
        return $"assets/vehicles/vehicle-{key}.webp";
    }
}
