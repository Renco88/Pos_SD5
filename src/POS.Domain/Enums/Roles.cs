namespace POS.Domain.Enums;

public static class Roles
{
    public const string Employer = "Employer";
    public const string Worker = "Worker";

    public static readonly IReadOnlyList<string> All = [Employer, Worker];
}
