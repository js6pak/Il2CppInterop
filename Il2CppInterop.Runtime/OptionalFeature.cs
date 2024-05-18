namespace Il2CppInterop.Runtime;

public sealed class OptionalFeature
{
    public OptionalFeature(OptionalFeatureStatus defaultStatus)
    {
        Status = defaultStatus;
    }

    public OptionalFeatureStatus Status { get; internal set; }

    public bool IsEnabled => Status == OptionalFeatureStatus.Enabled;

    public void Disable()
    {
        if (Status is OptionalFeatureStatus.Unknown or OptionalFeatureStatus.Enabled)
        {
            Status = OptionalFeatureStatus.Disabled;
        }
    }
}

public enum OptionalFeatureStatus
{
    /// <summary>
    /// Feature's status has not been determined yet, usually because hooks weren't setup yet.
    /// </summary>
    Unknown,

    /// <summary>
    /// Feature is enabled.
    /// </summary>
    Enabled,

    /// <summary>
    /// Feature was explicitly disabled by the user.
    /// </summary>
    Disabled,

    /// <summary>
    /// Feature doesn't work on this specific game build, probably because a required internal function couldn't be found.
    /// </summary>
    UnsupportedGameBuild,

    /// <summary>
    /// Feature isn't supported on this unity version.
    /// </summary>
    UnsupportedUnityVersion,
}

public static class OptionalFeatures
{
    public static OptionalFeature TypeInjection { get; } = new(OptionalFeatureStatus.Unknown);

    public static OptionalFeature Il2CppObjectCaching { get; } = new(OptionalFeatureStatus.Unknown);
    public static OptionalFeature Il2CppObjectPooling { get; } = new(OptionalFeatureStatus.Unknown);
}
