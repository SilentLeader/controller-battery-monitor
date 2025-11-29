using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using ControllerMonitor.Converters;
using ControllerMonitor.Services;

namespace ControllerMonitor.Extensions;

public class LocalizeExtension(string key) : MarkupExtension
{
    public string Key { get; set; } = key;

    private static readonly LocalizeConverter LOCALIZE_CONVERTER = new();

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Binding usage here is intentional and safe; avoid linker warning for reflection-based binding.")]
    public override object ProvideValue(IServiceProvider serviceProvider)
    {   
        return new Binding
        {
            Converter = LOCALIZE_CONVERTER,
            ConverterParameter = Key,
            Source = LocalizationService.Instance,
            Path = nameof(LocalizationService.CurrentCulture),
            Mode = BindingMode.OneWay
        };
    }
}
