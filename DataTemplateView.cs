namespace Meander;

using Microsoft.Maui.Controls.Compatibility;

internal class DataTemplateView : Frame
{
    private Element _content;

    public static readonly BindableProperty DataSourceProperty =
        BindableProperty.Create(nameof(DataSource), typeof(object), typeof(DataTemplateView),
            propertyChanged: OnDataSourceChanged,
            defaultBindingMode: BindingMode.OneWay);

    public object DataSource
    {
        get { return (object)GetValue(DataSourceProperty); }
        set { SetValue(DataSourceProperty, value); }
    }

    public static readonly BindableProperty DataTemplateSelectorProperty =
        BindableProperty.Create(nameof(DataTemplate), typeof(DataTemplateSelector), typeof(DataTemplateView),
            propertyChanged: OnDataTemplateSelectorChanged,
            defaultBindingMode: BindingMode.OneWay);

    public DataTemplate DataTemplate
    {
        get { return (DataTemplate)GetValue(DataTemplateSelectorProperty); }
        set { SetValue(DataTemplateSelectorProperty, value); }
    }

    private static View GenerateStubContent()
    {
        var result = new Label();
        result.SetBinding(Label.TextProperty, new Binding(string.Empty));
        return result;
    }

    private static void OnDataSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        (bindable as DataTemplateView)!.UpdateDataTemplate();
    }

    private static void OnDataTemplateSelectorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        (bindable as DataTemplateView)!.UpdateDataTemplate();
    }

    private void ClearContent()
    {
        if (_content == null) return;

        _content.Parent = null;
        _content = null;

        Content = null;
    }

    private void SetContent(View elem, object data)
    {
        elem.BindingContext = data;
        elem.Parent = this;

        _content = elem;
        Content = elem;

        UpdateChildrenLayout();
    }

    private void UpdateDataTemplate()
    {
        var dataSource = DataSource;
        if (dataSource == null)
        {
            ClearContent();
            return;
        }

        var dataTemplate = DataTemplate;

        if (dataTemplate is DataTemplateSelector sel)
            dataTemplate = sel.SelectTemplate(dataSource, this);

        if (dataTemplate == null)
        {
            SetContent(GenerateStubContent(), dataSource);
            return;
        }

        var newContent = dataTemplate.CreateContent() as View;
        if (newContent == null) throw new Exception($"{nameof(DataTemplate)} has generated a 'null' content.");

        SetContent(newContent, dataSource);
    }
}
