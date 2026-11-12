using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
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
    public IEnumerable<Window> Create() => layoutProvider.Get().Select(CreateInternal);
    
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
        var settingsWindow = () => serviceProvider.GetRequiredService<Settings>();
        
        var editWidgetWindow = widgetInfo.EditModelViewType != null 
            ? () => CreateEditWidgetWindow(widgetLayoutProvider, widgetInfo.EditModelViewType)
            : (Func<EditWidget>?) null;
        
        var widget = editWidgetWindow != null
            ? (Widget) assemblyProvider.Activate(typeof(Widget), widgetLayoutProvider, widgetControl, settingsWindow, editWidgetWindow)
            : (Widget) assemblyProvider.Activate(typeof(Widget), widgetLayoutProvider, widgetControl, settingsWindow);

        widgetRegistry.Register(widget);
        widget.Closed += (_, _) => widgetRegistry.Unregister(widget);
        return widget;
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