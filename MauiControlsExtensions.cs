using System.ComponentModel;

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

    public static void SubscribeBindingContextHandlers(this Page page)
    {
        if (page?.BindingContext is IEnableable enableable)
        {
            page.Appearing += (_, _) => enableable.OnEnabled();
            page.Disappearing += (_, _) => enableable.OnDisabled();
            // TODO: binding context change handling is missing.
        }
    }

    public static void PropagateResourceValuesToBindingContext(this VisualElement page)
    {
        if (page.BindingContext is null) return;

        var bc = page.BindingContext;
        var bcType = bc.GetType();
        foreach (var (p, v) in page.Resources
            .Select(kvp => (p: bcType.GetProperty(kvp.Key), v: kvp.Value))
            .Where(t => t.p?.CanWrite == true && !t.p.SetMethod.IsStatic
                    && (
                        (t.v is null && t.p.PropertyType.IsClass)
                            || (t.v is not null && t.p.PropertyType.IsAssignableFrom(t.v.GetType())))
                    ))
        {
            p.SetValue(bc, v);
        }
    }
}
