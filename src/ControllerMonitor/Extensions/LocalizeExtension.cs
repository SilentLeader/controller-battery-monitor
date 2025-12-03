using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using ControllerMonitor.Converters;
using ControllerMonitor.Services;

namespace ControllerMonitor.Extensions;

public class LocalizeExtension(string key) : MarkupExtension
{
    public string Key { get; set; } = key;

    private static readonly LocalizeConverter LOCALIZE_CONVERTER = new();

    private static readonly CompiledBindingPath CURRENTCULTURE_PATH = new CompiledBindingPathBuilder()
            .Property(new ClrPropertyInfo(
                nameof(LocalizationService.CurrentCulture), 
                o => LocalizationService.Instance.CurrentCulture, 
                null,
                typeof(CultureInfo)),
                PropertyInfoAccessorFactory.CreateInpcPropertyAccessor).Build();

    public override object ProvideValue(IServiceProvider serviceProvider)
    {   
        return new CompiledBindingExtension(CURRENTCULTURE_PATH)
        {
            Converter = LOCALIZE_CONVERTER,
            ConverterParameter = Key,
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay
        }.ProvideValue(serviceProvider);
    }
}
