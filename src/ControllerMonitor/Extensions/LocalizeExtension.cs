using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using ControllerMonitor.Services;

namespace ControllerMonitor.Extensions;

public class LocalizeExtension : MarkupExtension
{
    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public string Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var keyToUse = Key;

        // Create a MultiBinding that binds to CurrentCulture property
        // This ensures the binding updates when the language changes
        var multiBinding = new MultiBinding
        {
            Converter = new FuncMultiValueConverter<object, string>(values =>
            {
                // When CurrentCulture changes, this converter is called again
                return LocalizationService.Instance[keyToUse];
            })
        };

        // Bind to CurrentCulture so changes trigger the converter
        var cultureBinding = new ReflectionBindingExtension("CurrentCulture")
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay
        };

        multiBinding.Bindings.Add((IBinding)cultureBinding.ProvideValue(serviceProvider));

        return multiBinding;
    }
}
