namespace Meander.Controls;

internal class DataTemplateView : Frame
{
    private Element _content;

    public static readonly BindableProperty DataSourceProperty =
        BindableProperty.Create(nameof(DataSource), typeof(object), typeof(DataTemplateView),
            defaultBindingMode: BindingMode.OneWay,
            propertyChanged: (bo, _, _) => (bo as DataTemplateView)!.UpdateDataTemplate());

    public object DataSource
    {
        get { return GetValue(DataSourceProperty); }
        set { SetValue(DataSourceProperty, value); }
    }

    public static readonly BindableProperty DataTemplateProperty =
        BindableProperty.Create(nameof(DataTemplate), typeof(DataTemplate), typeof(DataTemplateView),
            defaultBindingMode: BindingMode.OneWay,
            propertyChanged: (bo, _, _) => (bo as DataTemplateView)!.UpdateDataTemplate());

    public DataTemplate DataTemplate
    {
        get { return (DataTemplate)GetValue(DataTemplateProperty); }
        set { SetValue(DataTemplateProperty, value); }
    }

    private static View GenerateStubContent()
    {
        var result = new Label();
        result.SetBinding(Label.TextProperty, new Binding("."));
        return result;
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

        ClearContent();

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
            throw new Exception($"{nameof(DataTemplate)} has generated an invalid content (must be a {nameof(View)} type instance).");

        newContent.BindingContext = DataSource;
        SetContent(newContent, dataSource);
    }
}
