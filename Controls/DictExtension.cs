using System.Collections;

namespace Meander.Controls;

internal sealed class DictExtension : DictExtensionBase
{
    public Type KeyType { get; set; } = typeof(object);
    public Type ValueType { get; set; } = typeof(object);

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var t = typeof(Dictionary<,>).MakeGenericType(KeyType, ValueType);
        var d = Activator.CreateInstance(t) as IDictionary;
        foreach (var entry in Entries)
            d.Add(entry.Key, entry.Value);
        return d;
    }
}

internal abstract class DictExtensionBase : IMarkupExtension, IList
{
    protected readonly List<DictionaryEntry> Entries = new();

    public int Add(object value)
    {
        return ((IList)Entries).Add(value);
    }

    public abstract object ProvideValue(IServiceProvider serviceProvider);

    #region aux

    public object this[int index]
    {
        get => ((IList)Entries)[index];
        set => ((IList)Entries)[index] = value;
    }

    public bool IsFixedSize => ((IList)Entries).IsFixedSize;

    public bool IsReadOnly => ((IList)Entries).IsReadOnly;

    public int Count => ((ICollection)Entries).Count;

    public bool IsSynchronized => ((ICollection)Entries).IsSynchronized;

    public object SyncRoot => ((ICollection)Entries).SyncRoot;

    void IList.Clear()
    {
        ((IList)Entries).Clear();
    }

    bool IList.Contains(object value)
    {
        return ((IList)Entries).Contains(value);
    }

    void ICollection.CopyTo(Array array, int index)
    {
        ((ICollection)Entries).CopyTo(array, index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Entries).GetEnumerator();
    }

    int IList.IndexOf(object value)
    {
        return ((IList)Entries).IndexOf(value);
    }

    void IList.Insert(int index, object value)
    {
        ((IList)Entries).Insert(index, value);
    }

    void IList.Remove(object value)
    {
        ((IList)Entries).Remove(value);
    }

    void IList.RemoveAt(int index)
    {
        ((IList)Entries).RemoveAt(index);
    }

    #endregion aux
}
