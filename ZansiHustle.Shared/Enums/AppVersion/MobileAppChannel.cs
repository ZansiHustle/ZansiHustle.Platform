namespace ZansiHustle.Shared.Enums.AppVersion;

/// <summary>
/// Distribution channel (app store) a build was installed from. Wire parsing
/// accepts lowercase strings ("google" / "huawei" / "apple" / "generic"); an
/// unknown/missing value defaults to <see cref="Google"/>.
/// </summary>
public enum MobileAppChannel
{
    Google = 0,
    Huawei = 1,
    Apple = 2,
    Generic = 3
}
