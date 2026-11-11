namespace Finance.Models;

public enum ConverterMode
{
    Currency,
    CssUnits,
    DigitalStorage
}

public record ConverterModel(
    ConverterMode Mode = ConverterMode.Currency,
    string SourceCurrency = "USD",
    string TargetCurrency = "EUR",
    double RootFontSizePx = 16.0);
