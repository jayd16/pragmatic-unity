using System;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Event)]
public class DataBindingAttribute : Attribute
{
    public readonly string Id;

    public DataBindingAttribute(string id)
    {
        Id = id;
    }
}