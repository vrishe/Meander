using System.ComponentModel;
using System.Reflection;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Logging;

namespace Meander;

internal static class MauiControlsExtensions
{
    public static void ApplyPopupStyle(this Popup popup)
    {
        Style FindStyle(ResourceDictionary res)
        {
            var style = popup.GetPopupStyle();
            if (style == null && res.TryGetValue(typeof(Popup).FullName, out var styleRaw))
                style = styleRaw as Style;
            return style;
        }

        void ApplyStyle(Style style)
        {
            if (style == null) return;

            foreach (var setter in style.Setters)
            {
                if (setter.Value is BindingBase binding)
                {
                    popup.SetBinding(setter.Property, binding);
                    continue;
                }

                popup.SetValue(setter.Property, setter.Value);
            }
        }

        ApplyStyle(FindStyle(Application.Current.Resources));
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
        var logger = new Lazy<ILogger>(() => App.FindMauiContext(dst).Services.GetService<ILogger<App>>(),
            LazyThreadSafetyMode.None);
        foreach (var (pi, attr) in props)
        {
            var key = attr.Alias ?? pi.Name;

            try
            {
                var value = res[key];
                if (value == null)
                {
                    logger.Value?.LogWarning("'{}' resource value is 'null'", key);

                    pi.SetValue(bc, null);
                    continue;
                }

                var op = GetImplicitConversionOperator(value.GetType(), value.GetType(), pi.PropertyType);
                pi.SetValue(bc, op != null
                    ? op.Invoke(null, new[] { value })
                    : Convert.ChangeType(value, pi.PropertyType));
            }
            catch (Exception e)
            {
                logger.Value?.LogError(e, "Failed to assign '{}' resource value to {}.{}", key, type.FullName, pi.Name);
            }
        }

        (bc as IFinishedApplyResourceValues)?.OnResourceValuesApplied();
    }

    public static VisualElement WithEnableable(this VisualElement dst)
    {
        bool CheckEnabled() => dst.IsEnabled && dst.Parent != null;

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
                case nameof(VisualElement.Parent):
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
        bool CheckEnabled() => hasAppeared && dst.Parent != null && dst.IsEnabled;

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
                case nameof(VisualElement.Parent):
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

    public static ContentPage WithMenuBarItemsFix(this ContentPage page)
    {
        void ApplyMenuBarItemsContext()
        {
            foreach (var item in page.MenuBarItems)
                item.BindingContext = page.BindingContext;
        }

        page.Appearing += (s, e) => ApplyMenuBarItemsContext();
        page.BindingContextChanged += (s, e) => ApplyMenuBarItemsContext();

        return page;
    }

    private static MethodInfo GetImplicitConversionOperator(Type onType, Type fromType, Type toType)
    {
        const BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        IEnumerable<MethodInfo> mis;
        try
        {
            mis = new[] { onType.GetMethod("op_Implicit", bindingAttr, null, new[] { fromType }, null) };
        }
        catch (AmbiguousMatchException)
        {
            mis = new List<MethodInfo>();
            foreach (var mi in onType.GetMethods(bindingAttr))
            {
                if (mi.Name != "op_Implicit")
                    break;
                var parameters = mi.GetParameters();
                if (parameters.Length == 0)
                    continue;
                if (!parameters[0].ParameterType.IsAssignableFrom(fromType))
                    continue;
                ((List<MethodInfo>)mis).Add(mi);
            }
        }

        foreach (var mi in mis)
        {
            if (mi == null)
                continue;
            if (!mi.IsSpecialName)
                continue;
            if (!mi.IsPublic)
                continue;
            if (!mi.IsStatic)
                continue;
            if (!toType.IsAssignableFrom(mi.ReturnType))
                continue;

            return mi;
        }
        return null;
    }
}

internal interface IFinishedApplyResourceValues
{
    void OnResourceValuesApplied();
}