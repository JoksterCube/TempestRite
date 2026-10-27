namespace JoksterCube.TempestRite.Domain;

// Who may change the weather.
internal enum WeatherPermission
{
    // Everyone with the mod.
    Anyone = 0,
    // Only server admins (and the host).
    AdminsOnly = 1
}

internal static class WeatherPermissions
{
    internal static bool Allowed(WeatherPermission permission, bool isAdmin) =>
        isAdmin || permission == WeatherPermission.Anyone;
}
