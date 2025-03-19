namespace PPMTool.Enums
{
    /// <summary>
    /// The available options for validity of an API key
    /// </summary>
    public enum ApiKeyExpirationOptions
    {
        [ExpirationInDays(0.000694)]
        OneMin,
        [ExpirationInDays(1)]
        OneDay,
        [ExpirationInDays(30)]
        ThirtyDays,
        [ExpirationInDays(90)]
        NintyDays
    }
}
