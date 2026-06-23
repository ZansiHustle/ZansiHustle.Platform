namespace ZansiHustle.Shared.Enums.AppVersion;

/// <summary>
/// Mobile OS platform a build targets. Wire parsing accepts lowercase strings
/// ("android" / "ios"); an unknown/missing value defaults to <see cref="Android"/>.
/// </summary>
public enum MobileAppPlatform
{
    Android = 0,
    iOS = 1
}
