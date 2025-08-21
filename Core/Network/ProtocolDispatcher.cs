using System.Reflection;
using Google.Protobuf;
using Spire.Protocol;

namespace Spire.Core.Network;

public abstract class ProtocolDispatcher
{
    protected static readonly Dictionary<ushort, (MethodInfo handler, PropertyInfo valueProperty)> HandlerEntries = [];
    
    protected static void Initialize(Assembly assembly, params Type[] additionalParameterTypes)
    {
        var handlers = assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.IsDefined(typeof(ProtocolHandlerAttribute), false));
        
        foreach (var handler in handlers)
        {
            Register(handler, additionalParameterTypes);
        }
    }

    private static void Register(MethodInfo handler, Type[] additionalParameterTypes)
    {
        var attribute = handler.GetCustomAttribute<ProtocolHandlerAttribute>()!;
        
        var parameters = handler.GetParameters();
        var expectedParameterCount = 1 + additionalParameterTypes.Length;
        
        if (parameters.Length != expectedParameterCount)
        {
            throw new Exception(
                $"Invalid protocol handler signature: {handler.DeclaringType?.Name}.{handler.Name}. " +
                $"Expected {expectedParameterCount} parameters, got {parameters.Length}");
        }
        
        // Validate attribute type
        var protocolWrapperType = attribute.ProtocolType;
        if (!typeof(IProtocol).IsAssignableFrom(protocolWrapperType))
        {
            throw new Exception(
                $"Protocol type {protocolWrapperType.Name} must implement {nameof(IProtocol)}");
        }
        
        // Validate first parameter to be Protocol.
        var protocolDataType = parameters[0].ParameterType;
        if (!typeof(IMessage).IsAssignableFrom(protocolDataType))
        {
            throw new Exception(
                $"Protocol data type {protocolDataType.Name} must implement {nameof(IMessage)}");
        }
        
        // Validate additional parameters
        for (int i = 0; i < additionalParameterTypes.Length; i++)
        {
            var actualType = parameters[i + 1].ParameterType;
            var expectedType = additionalParameterTypes[i];
            
            if (actualType != expectedType)
            {
                throw new Exception(
                    $"Invalid protocol handler signature: {handler.DeclaringType?.Name}.{handler.Name}. " +
                    $"Parameter {i + 1} expected type {expectedType.Name}, got {actualType.Name}");
            }
        }
        
        var instance = (IProtocol)Activator.CreateInstance(protocolWrapperType, [null])!;
        var protocolId = instance.ProtocolId;
        
        var valueProperty = protocolWrapperType.GetProperty("Value")!;
        
        HandlerEntries[protocolId] = (handler, valueProperty);
    }
    
    public abstract void Dispatch(IProtocol protocol);
}

[Serializable]
public class DispatchException(string message) : Exception(message);
