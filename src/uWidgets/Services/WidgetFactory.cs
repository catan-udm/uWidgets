using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using uWidgets.Core;
using uWidgets.Core.Interfaces;
using uWidgets.Core.Models;
using uWidgets.Core.Models.Attributes;
using uWidgets.Core.Services;
using uWidgets.Views;

namespace uWidgets.Services;

public class WidgetFactory(
    IAssemblyProvider assemblyProvider,
    ILayoutProvider layoutProvider,
    WidgetRegistry widgetRegistry,
    IServiceProvider serviceProvider) 
    : IWidgetFactory<Window, UserControl>
{
    private readonly List<string> startupWarnings = [];
    public IReadOnlyList<string> StartupWarnings => startupWarnings;

    public IEnumerable<Window> Create()
    {
        startupWarnings.Clear();
        var created = new List<Window>();

        foreach (var widgetLayout in layoutProvider.Get())
        {
            try
            {
                created.Add(CreateInternal(widgetLayout));
            }
            catch (Exception ex)
            {
                var warning = $"{widgetLayout.Type}.{widgetLayout.SubType}: {ex.Message}";
                startupWarnings.Add(warning);
                Program.AppendCrashLog($"[{DateTime.UtcNow:O}] Failed loading widget {warning}");
            }
        }

        return created;
    }

    public IReadOnlyList<string> ValidateLayouts(IEnumerable<WidgetLayout> layouts)
    {
        var errors = new List<string>();
        var available = assemblyProvider
            .GetAssemblyInfos(Const.WidgetsFolder)
            .ToDictionary(g => g.Key, g => g.MaxBy(a => a.Version)!, StringComparer.OrdinalIgnoreCase);

        foreach (var layout in layouts)
        {
            if (string.IsNullOrWhiteSpace(layout.Type) || string.IsNullOrWhiteSpace(layout.SubType))
            {
                errors.Add("Layout contains a widget entry with missing Type or SubType.");
                continue;
            }

            if (!available.ContainsKey(layout.Type))
            {
                errors.Add($"Widget plugin '{layout.Type}' is not installed.");
                continue;
            }

            try
            {
                var asm = assemblyProvider.LoadAssembly(layout.Type);
                _ = GetWidgetInfo(asm, layout.SubType);
            }
            catch (Exception ex)
            {
                errors.Add($"{layout.Type}.{layout.SubType}: {ex.Message}");
            }
        }

        return errors;
    }
    
    public Window Add(WidgetLayout widgetLayout)
    {
        var sanitized = widgetLayout.Id == Guid.Empty
            ? widgetLayout with { Id = Guid.NewGuid() }
            : widgetLayout;
        var list = layoutProvider.Get().ToList();
        list.Add(sanitized);
        layoutProvider.Save(list);
        
        return CreateInternal(sanitized);
    }

    public UserControl CreateControl(Type type)
    {
        var widgetLayoutProvider = new WidgetLayoutProvider(layoutProvider, null);
        return CreateWidgetControl(type, widgetLayoutProvider, null);
    }
    
    private Window CreateInternal(WidgetLayout widgetLayout)
    {
        var widgetLayoutProvider = new WidgetLayoutProvider(layoutProvider, widgetLayout);
        
        var assembly = assemblyProvider.LoadAssembly(widgetLayout.Type);
        var widgetInfo = GetWidgetInfo(assembly, widgetLayout.SubType);
        var widgetControl = () => CreateWidgetControl(widgetInfo.ViewType, widgetLayoutProvider, widgetLayoutProvider.Get().GetModel(widgetInfo.ModelType));
        var settingsWindow = () => serviceProvider.GetRequiredService<SettingsWindowHost>().GetOrCreate();
        
        var editWidgetWindow = widgetInfo.EditModelViewType != null 
            ? () => CreateEditWidgetWindow(widgetLayoutProvider, widgetInfo.EditModelViewType)
            : (Func<EditWidget>?) null;
        
        var widget = editWidgetWindow != null
            ? (Widget) assemblyProvider.Activate(typeof(Widget), widgetLayoutProvider, widgetControl, settingsWindow, editWidgetWindow)
            : (Widget) assemblyProvider.Activate(typeof(Widget), widgetLayoutProvider, widgetControl, settingsWindow);

        widgetRegistry.Register(widget);
        widget.DuplicateRequested += OnWidgetDuplicateRequested;
        widget.Closed += (_, _) =>
        {
            widget.DuplicateRequested -= OnWidgetDuplicateRequested;
            widgetRegistry.Unregister(widget);
        };
        return widget;
    }

    private void OnWidgetDuplicateRequested(Widget source)
    {
        var current = source.CurrentLayout;
        var placement = serviceProvider.GetService<WidgetPlacementService>();
        var appSettings = serviceProvider.GetRequiredService<IAppSettingsProvider>().Get();
        var span = Math.Max(1, appSettings.Dimensions.Size + appSettings.Dimensions.Margin);
        var cols = Math.Max(1, (int)Math.Round((current.Width + appSettings.Dimensions.Margin) / (double)span));
        var rows = Math.Max(1, (int)Math.Round((current.Height + appSettings.Dimensions.Margin) / (double)span));

        WidgetLayout copy;
        if (placement != null)
        {
            var placed = placement.CreateSmartLayout(source, current.Type, current.SubType, cols, rows);
            copy = placed with { Settings = current.Settings, Width = current.Width, Height = current.Height };
        }
        else
        {
            copy = current with { Id = Guid.NewGuid(), X = current.X + 24, Y = current.Y + 24 };
        }

        Add(copy).Show();
    }

    private UserControl CreateWidgetControl(Type type, WidgetLayoutProvider? widgetLayoutProvider, object? model)
    {
        List<object> args = [];
        
        if (NeedsWidgetLayoutProvider(type) && widgetLayoutProvider != null)
            args.Add(widgetLayoutProvider);
        
        if (model != null)
            args.Add(model);

        return (assemblyProvider.Activate(type, args.ToArray()) as UserControl)!;
    }

    private EditWidget CreateEditWidgetWindow(IWidgetLayoutProvider widgetLayoutProvider, Type type)
    {
        var control = (UserControl) assemblyProvider.Activate(type, widgetLayoutProvider);

        return new EditWidget(widgetLayoutProvider, control);
    }

    private bool NeedsWidgetLayoutProvider(Type type)
    {
        return type
            .GetConstructors()
            .Any(constructor => constructor
                .GetParameters()
                .Any(param => param.ParameterType == typeof(IWidgetLayoutProvider)));
    }

    private static WidgetInfoAttribute GetWidgetInfo(Assembly assembly, string typeName)
    {
        var widgetInfo = assembly
            .GetCustomAttributes<WidgetInfoAttribute>()
            .SingleOrDefault(attribute => attribute.ViewType.Name == typeName);
        
        if (widgetInfo == null) 
            throw new ArgumentException($"No suitable WidgetInfoAttribute found for {typeName}");

        return widgetInfo;
    }
}