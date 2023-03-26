namespace Meander;

internal class DataTemplateView : Frame
{
    private Element _content;

    public static readonly BindableProperty DataSourceProperty =
        BindableProperty.Create(nameof(DataSource), typeof(object), typeof(DataTemplateView),
            defaultBindingMode: BindingMode.OneWay,
            propertyChanged: OnDataSourceChanged);

    public object DataSource
    {
        get { return GetValue(DataSourceProperty); }
        set { SetValue(DataSourceProperty, value); }
    }

    public static readonly BindableProperty DataTemplateSelectorProperty =
        BindableProperty.Create(nameof(DataTemplate), typeof(DataTemplateSelector), typeof(DataTemplateView),
            defaultBindingMode: BindingMode.OneWay,
            propertyChanged: OnDataTemplateSelectorChanged);

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

        if (dataTemplate.CreateContent() is not View newContent)
            throw new Exception($"{nameof(DataTemplate)} has generated invalid content.");

        newContent.BindingContext = DataSource;
        SetContent(newContent, dataSource);
    }
}
