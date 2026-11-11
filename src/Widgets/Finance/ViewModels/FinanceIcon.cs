using Avalonia.Media;

namespace Finance.ViewModels;

public static class FinanceIcon
{
    public static Lazy<StreamGeometry> Category = new(() => StreamGeometry.Parse(
        "M1 3a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1H1zm7 8a2 2 0 1 0 0-4 2 2 0 0 0 0 4zM0 5a1 1 0 0 1 1-1h14a1 1 0 0 1 1 1v8a1 1 0 0 1-1 1H1a1 1 0 0 1-1-1V5zm3 0a2 2 0 0 1-2 2v4a2 2 0 0 1 2 2h10a2 2 0 0 1 2-2V7a2 2 0 0 1-2-2H3z"));

    public static Lazy<StreamGeometry> Crypto = new(() => StreamGeometry.Parse(
        "M8 1a7 7 0 1 0 0 14A7 7 0 0 0 8 1zm1.5 4c.8 0 1.5.5 1.5 1.2 0 .5-.3 1-.8 1.1.7.2 1.1.7 1.1 1.4 0 .9-.8 1.5-1.8 1.5H7.5V11H6.5V9.2H5.5V8.2h1V6.2h-1V5.2h1V4h1v1.2h2zm-2 1.2v1.5h1.8c.4 0 .8-.2.8-.7s-.4-.8-.8-.8H7.5zm0 2.5v1.8h2c.5 0 .9-.3.9-.9 0-.5-.4-.9-.9-.9H7.5z"));

    public static Lazy<StreamGeometry> Stocks = new(() => StreamGeometry.Parse(
        "M0 0h1v15h15v1H0V0zm10 3.5a.5.5 0 0 1 .5-.5h4a.5.5 0 0 1 .5.5v4a.5.5 0 0 1-1 0V4.7l-4.1 4.1a.5.5 0 0 1-.7 0L7 6.6 3.4 10.1a.5.5 0 0 1-.7-.7l4-4a.5.5 0 0 1 .7 0l2.1 2.1 3.4-3.4H10.5a.5.5 0 0 1-.5-.5z"));

    public static Lazy<StreamGeometry> Converter = new(() => StreamGeometry.Parse(
        "M11.5 2a.5.5 0 0 1 .5.5v2.8l2.1-2.1a.5.5 0 0 1 .7.7l-3 3a.5.5 0 0 1-.7 0l-3-3a.5.5 0 1 1 .7-.7l2.2 2.1V2.5a.5.5 0 0 1 .5-.5zm-7 12a.5.5 0 0 1-.5-.5v-2.8L1.9 12.8a.5.5 0 0 1-.7-.7l3-3a.5.5 0 0 1 .7 0l3 3a.5.5 0 0 1-.7.7L5 10.7v2.8a.5.5 0 0 1-.5.5z"));

    public static Lazy<StreamGeometry> ArrowUp = new(() => StreamGeometry.Parse(
        "M8 3.5a.5.5 0 0 1 .5.5v8a.5.5 0 0 1-1 0V4l-2.6 2.6a.5.5 0 1 1-.7-.7l3.5-3.5a.5.5 0 0 1 .3-.1z"));

    public static Lazy<StreamGeometry> ArrowDown = new(() => StreamGeometry.Parse(
        "M8 12.5a.5.5 0 0 1-.5-.5V4a.5.5 0 0 1 1 0v8l2.6-2.6a.5.5 0 0 1 .7.7l-3.5 3.5a.5.5 0 0 1-.3.1z"));

    public static StreamGeometry CategoryGeometry => Category.Value;
    public static StreamGeometry CryptoGeometry => Crypto.Value;
    public static StreamGeometry StocksGeometry => Stocks.Value;
    public static StreamGeometry ConverterGeometry => Converter.Value;
    public static StreamGeometry ArrowUpGeometry => ArrowUp.Value;
    public static StreamGeometry ArrowDownGeometry => ArrowDown.Value;
}
