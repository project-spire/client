namespace Spire.Core.Network;

[AttributeUsage(AttributeTargets.Method)]
public class ProtocolHandlerAttribute(Type protocolType) : Attribute
{
    public Type ProtocolType { get; } = protocolType;
}