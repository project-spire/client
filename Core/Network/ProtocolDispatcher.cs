using System.Reflection;
using Google.Protobuf;
using Spire.Protocol;

namespace Spire.Core.Network;

public static class ProtocolDispatcher
{
    private static readonly Dictionary<ushort, ProtocolHandlerEntry> HandlerEntries = [];

    public static void Register(Assembly assembly)
    {
        var handlers = assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.IsDefined(typeof(ProtocolHandlerAttribute), false));

        foreach (var handler in handlers)
        {
            RegisterHandler(handler);
        }
    }

    private static void RegisterHandler(MethodInfo handler)
    {
        var attribute = handler.GetCustomAttribute<ProtocolHandlerAttribute>();
        if (attribute == null) return;
        
        var parameters = handler.GetParameters();
        if (parameters.Length != 2 ||
            parameters[0].ParameterType != typeof(ISessionContext))
            throw new Exception(
                $"Invalid protocol handler signature: {handler.DeclaringType?.Name}.{handler.Name}");
        
        
        var protocolWrapperType = attribute.ProtocolType;
        var protocolDataType = parameters[1].ParameterType;
        
        if (!typeof(IProtocol).IsAssignableFrom(protocolWrapperType))
            throw new Exception(
                $"Protocol type {protocolWrapperType.Name} must implement {nameof(IProtocol)}");
        
        if (!typeof(IMessage).IsAssignableFrom(protocolDataType))
            throw new Exception(
                $"Protocol data type {protocolDataType.Name} must implement {nameof(IMessage)}");
        
        var instance = (IProtocol)Activator.CreateInstance(protocolWrapperType, [null])!;
        var protocolId = instance.ProtocolId;
        
        var valueProperty = protocolWrapperType.GetProperty("Value")!;
        
        HandlerEntries[protocolId] = new ProtocolHandlerEntry(handler, valueProperty);
    }
    
    public static void Dispatch(ISessionContext ctx, IProtocol protocol)
    {
        if (!HandlerEntries.TryGetValue(protocol.ProtocolId, out var handlerEntry))
        {
            ctx.HandleError(new DispatchException($"Unhandled protocol {nameof(protocol)}"));
            return;
        }

        try
        {
            var protocolData = handlerEntry.ValueProperty.GetValue(protocol);
            handlerEntry.Handler.Invoke(null, [ctx, protocolData]);
        }
        catch (Exception e)
        {
            ctx.HandleError(new DispatchException($"Failed to dispatch protocol {nameof(protocol)}: {e.Message}"));
        }
    }
}

internal record ProtocolHandlerEntry(
    MethodInfo Handler,
    PropertyInfo ValueProperty
);

[Serializable]
public class DispatchException(string message) : Exception(message);
