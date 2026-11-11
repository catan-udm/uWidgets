using System.Reflection;
using Development.Locales;
using Development.Models;
using Development.Views;
using Development.Views.Settings;
using uWidgets.Core.Models.Attributes;

[assembly: AssemblyCompany("creewick")]
[assembly: AssemblyVersion("1.0.0")]

[assembly: WidgetInfo(typeof(Docker), typeof(DockerModel), typeof(DockerSettings), "Development_Docker_Title", "Development_Docker_Subtitle")]
[assembly: WidgetInfo(typeof(DevApps), typeof(DevAppsModel), typeof(DevAppsSettings), "Development_DevApps_Title", "Development_DevApps_Subtitle")]
[assembly: WidgetInfo(typeof(GitWorkingTree), typeof(GitWorkingTreeModel), typeof(GitWorkingTreeSettings), "Development_GitWorkingTree_Title", "Development_GitWorkingTree_Subtitle")]
[assembly: Locale(typeof(Locale), "Development", "M10.478 1.647a.5.5 0 1 0-.956-.294l-4 13a.5.5 0 0 0 .956.294l4-13zM4.854 4.146a.5.5 0 0 1 0 .708L1.707 8l3.147 3.146a.5.5 0 0 1-.708.708l-3.5-3.5a.5.5 0 0 1 0-.708l3.5-3.5a.5.5 0 0 1 .708 0zm6.292 0a.5.5 0 0 0 0 .708L14.293 8l-3.147 3.146a.5.5 0 0 0 .708.708l3.5-3.5a.5.5 0 0 0 0-.708l-3.5-3.5a.5.5 0 0 0-.708 0z")]
