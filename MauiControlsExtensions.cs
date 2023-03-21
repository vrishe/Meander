using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Meander;

internal static class MauiControlsExtensions
{
    public static VisualElement WithEnableable(this VisualElement dst)
    {
        bool CheckEnabled() => dst.IsEnabled;

        var isEnabled = CheckEnabled();
        var enableable = dst.BindingContext as IEnableable;
        void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(VisualElement.BindingContext):
                    if (isEnabled) enableable?.OnDisabled();
                    enableable = dst.BindingContext as IEnableable;
                    if (isEnabled) enableable?.OnEnabled();
                    break;

                case nameof(VisualElement.IsEnabled):
                    if (isEnabled != CheckEnabled())
                    {
                        if (isEnabled = !isEnabled) enableable?.OnEnabled();
                        else enableable?.OnDisabled();
                    }
                    break;
            }
        }

        dst.PropertyChanged += PropertyChanged;

        PropertyChanged(dst, new PropertyChangedEventArgs(nameof(VisualElement.BindingContext)));

        return dst;
    }

    public static VisualElement WithEnableable(this Page dst)
    {
        var hasAppeared = dst.Parent != null;
        bool CheckEnabled() => hasAppeared && dst.IsEnabled;
        
        var isEnabled = CheckEnabled();
        var enableable = dst.BindingContext as IEnableable;
        void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(VisualElement.BindingContext):
                    if (isEnabled) enableable?.OnDisabled();
                    enableable = dst.BindingContext as IEnableable;
                    if (isEnabled) enableable?.OnEnabled();
                    break;

                case nameof(VisualElement.IsEnabled):
                    if (isEnabled != CheckEnabled())
                    {
                        if (isEnabled = !isEnabled) enableable?.OnEnabled();
                        else enableable?.OnDisabled();
                    }
                    break;
            }
        }

        dst.PropertyChanged += PropertyChanged;
        dst.Appearing += (_, _) => {
            hasAppeared = true;
            if (CheckEnabled() && !isEnabled)
            {
                isEnabled = true;
                enableable?.OnEnabled();
            }
        };
        dst.Disappearing += (_, _) =>
        {
            hasAppeared = false;
            if (!CheckEnabled() && isEnabled)
            {
                isEnabled = false;
                enableable?.OnDisabled();
            }
        };

        PropertyChanged(dst, new PropertyChangedEventArgs(nameof(VisualElement.BindingContext)));

        return dst;
    }

    public static void FillBindingContextResourceValues(this VisualElement dst)
    {
        if (dst is null) throw new ArgumentNullException(nameof(dst));

        var bc = dst.BindingContext;
        if (bc is null) return;

        var type = bc.GetType();
        var props = type.GetRuntimeProperties().Where(pi => pi.CanWrite)
            .Select(pi => (pi, attr: pi.GetCustomAttribute<ResourceValueAttribute>())).Where(t => t.attr != null);

        var res = dst.Resources;
        var logger = new Lazy<ILogger>(() => App.FindMauiContext(dst).Services.GetService<ILoggerFactory>()?.CreateLogger(dst.GetType()),
            LazyThreadSafetyMode.None);
        foreach (var (pi, attr) in props)
        {
            var key = attr.Alias ?? pi.Name;

            try
            {
                pi.SetValue(bc, Convert.ChangeType(res[key], pi.PropertyType));
            }
            catch (Exception e)
            {
                logger.Value?.LogError(e, "Failed to assign '{}' resource value to {}.{}", key, type.FullName, pi.Name);
            }
        }

        (bc as IFinishedApplyResourceValues)?.OnResourceValuesApplied();
    }
}

internal interface IFinishedApplyResourceValues
{
    void OnResourceValuesApplied();
}