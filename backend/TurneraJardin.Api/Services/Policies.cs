namespace TurneraJardin.Api.Services;

/// <summary>Authorization policy names (registered in Program.cs).</summary>
public static class Policies
{
    /// <summary>Admin or Coordinator: sees and manages every teacher's schedule.</summary>
    public const string Management = "Management";

    /// <summary>Any panel role (Admin, Coordinator or Teacher). A Teacher is limited to her own appointments.</summary>
    public const string Panel = "Panel";
}

/// <summary>Rate limiting policy names (registered in Program.cs).</summary>
public static class RateLimitPolicies
{
    public const string PublicWrite = "public";
    public const string PublicRead = "public-read";
    public const string Login = "login";
}

/// <summary>Output cache policies and tags for the public reads (registered in Program.cs).</summary>
public static class CachePolicies
{
    public const string Teachers = "teachers";
    public const string TeachersTag = "teachers";
    public const string Availability = "availability";
    public const string AvailabilityTag = "availability";
}
