namespace FuelControl.Infrastructure.Identity;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Dispatcher = "Dispatcher";
    public const string Moderator = "Moderator";

    public const string ModeratorOrHigher =
        $"{Moderator},{Admin}";

    public const string DispatcherOrHigher =
        $"{Dispatcher},{Moderator},{Admin}";

    public static readonly string[] All =
    [
        Admin,
        Dispatcher,
        Moderator
    ];
}