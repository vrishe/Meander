namespace Meander;

[AttributeUsage(AttributeTargets.Property)]
internal class ResourceValueAttribute : Attribute
{
    public string Alias { get; set; }

    public ResourceValueAttribute() { }

    public ResourceValueAttribute(string alias) { Alias = alias; }
}
