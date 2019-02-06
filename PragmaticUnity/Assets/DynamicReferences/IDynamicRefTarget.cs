namespace Com.Duffy.DynamicReferences
{
    public interface IDynamicRefTarget
    {
        string Id { get; }
        object Target { get; }
    }
}