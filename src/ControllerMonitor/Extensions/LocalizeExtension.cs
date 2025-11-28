using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using System.Diagnostics.CodeAnalysis;
using ControllerMonitor.Services;

namespace ControllerMonitor.Extensions;

public class LocalizeExtension : MarkupExtension
{
    public LocalizeExtension(string key)
    {
        Key = key;
    }

    public string Key { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Binding usage here is intentional and safe; avoid linker warning for reflection-based binding.")]
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
        var cultureBinding = new Binding
        {
            Path = "CurrentCulture",
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay
        };

        multiBinding.Bindings.Add(cultureBinding);

        return multiBinding;
    }
}
