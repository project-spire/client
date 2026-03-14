namespace Spire.Core.Network;

[AttributeUsage(AttributeTargets.Method)]
public class MessageHandlerAttribute(Type messageType) : Attribute
{
    public Type MessageType { get; } = messageType;
}