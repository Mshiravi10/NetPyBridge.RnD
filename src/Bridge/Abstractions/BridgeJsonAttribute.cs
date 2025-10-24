using System.Text.Json.Serialization;

namespace Bridge.Abstractions;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
public class BridgeJsonAttribute : Attribute
{
    public BridgeJsonAttribute() { }
}
