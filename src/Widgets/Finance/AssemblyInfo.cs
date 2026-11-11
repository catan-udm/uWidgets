using System.Reflection;
using Finance.Locales;
using Finance.Models;
using Finance.Views;
using Finance.Views.Settings;
using uWidgets.Core.Models.Attributes;

[assembly: AssemblyCompany("creewick")]
[assembly: AssemblyVersion("1.0.0")]

[assembly: WidgetInfo(typeof(Crypto), typeof(CryptoModel), typeof(CryptoSettings), "Finance_Crypto_Title", "Finance_Crypto_Subtitle")]
[assembly: WidgetInfo(typeof(Stocks), typeof(StocksModel), typeof(StocksSettings), "Finance_Stocks_Title", "Finance_Stocks_Subtitle")]
[assembly: WidgetInfo(typeof(Converter), typeof(ConverterModel), typeof(ConverterSettings), "Finance_Converter_Title", "Finance_Converter_Subtitle")]
[assembly: Locale(typeof(Locale), "Finance", "M1 3a1 1 0 0 1 1-1h12a1 1 0 0 1 1 1H1zm7 8a2 2 0 1 0 0-4 2 2 0 0 0 0 4zM0 5a1 1 0 0 1 1-1h14a1 1 0 0 1 1 1v8a1 1 0 0 1-1 1H1a1 1 0 0 1-1-1V5zm3 0a2 2 0 0 1-2 2v4a2 2 0 0 1 2 2h10a2 2 0 0 1 2-2V7a2 2 0 0 1-2-2H3z")]
