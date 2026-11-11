namespace Finance.Services;

public static class UnitConverterService
{
    public static double PxToRem(double px, double basePx = 16.0)
    {
        if (basePx <= 0) basePx = 16.0;
        return px / basePx;
    }

    public static double RemToPx(double rem, double basePx = 16.0)
    {
        if (basePx <= 0) basePx = 16.0;
        return rem * basePx;
    }

    public static double ConvertStorageBytes(double value, string fromUnit, string toUnit)
    {
        double bytes = fromUnit.ToUpperInvariant() switch
        {
            "B" => value,
            "KB" => value * 1024,
            "MB" => value * 1024 * 1024,
            "GB" => value * 1024 * 1024 * 1024,
            "TB" => value * 1024L * 1024 * 1024 * 1024,
            _ => value
        };

        return toUnit.ToUpperInvariant() switch
        {
            "B" => bytes,
            "KB" => bytes / 1024,
            "MB" => bytes / (1024 * 1024),
            "GB" => bytes / (1024 * 1024 * 1024),
            "TB" => bytes / (1024L * 1024 * 1024 * 1024),
            _ => bytes
        };
    }
}
