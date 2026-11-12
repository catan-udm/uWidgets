using System.Reflection;
using Media.Locales;
using Media.Models;
using Media.Views;
using Media.Views.Settings;
using uWidgets.Core.Models.Attributes;

[assembly: AssemblyCompany("creewick")]
[assembly: AssemblyVersion("1.0.1")]

[assembly: WidgetInfo(typeof(NowPlaying), typeof(NowPlayingModel), typeof(NowPlayingSettings), "Media_NowPlaying_Title", "Media_NowPlaying_Subtitle")]
[assembly: Locale(typeof(Locale), "Media", "M6 13c0 1.105-1.12 2-2.5 2S1 14.105 1 13s1.12-2 2.5-2 2.5.895 2.5 2m9-2c0 1.105-1.12 2-2.5 2s-2.5-.895-2.5-2 1.12-2 2.5-2 2.5.895 2.5 2 M14 11V2h1v9zM6 3v10H5V3z M5 2.905a1 1 0 0 1 .9-.995l8-.8a1 1 0 0 1 1.1.995V3L5 4z")]
