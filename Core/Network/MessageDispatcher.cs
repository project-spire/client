using System.Reflection;
using Spire.Message.Game;

namespace Spire.Core.Network;

public abstract class MessageDispatcher
{
    protected static readonly Dictionary<ushort, (MethodInfo handler, PropertyInfo valueProperty)> HandlerEntries = [];

    protected static void Initialize(Assembly assembly, params Type[] additionalParameterTypes)
    {
        var handlers = assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.IsDefined(typeof(MessageHandlerAttribute), false));

        foreach (var handler in handlers)
        {
            Register(handler, additionalParameterTypes);
        }
    }

    private static void Register(MethodInfo handler, Type[] additionalParameterTypes)
    {
        var attribute = handler.GetCustomAttribute<MessageHandlerAttribute>()!;

        var parameters = handler.GetParameters();
        var expectedParameterCount = 1 + additionalParameterTypes.Length;

        if (parameters.Length != expectedParameterCount)
        {
            throw new Exception(
                $"Invalid message handler signature: {handler.DeclaringType?.Name}.{handler.Name}. " +
                $"Expected {expectedParameterCount} parameters, got {parameters.Length}");
        }

        // Validate attribute type
        var messageWrapperType = attribute.MessageType;
        if (!typeof(IMessage).IsAssignableFrom(messageWrapperType))
        {
            throw new Exception(
                $"Message type {messageWrapperType.Name} must implement {nameof(IMessage)}");
        }

        // Validate first parameter to be protobuf IMessage.
        var messageDataType = parameters[0].ParameterType;
        if (!typeof(Google.Protobuf.IMessage).IsAssignableFrom(messageDataType))
        {
            throw new Exception(
                $"Message data type {messageDataType.Name} must implement {nameof(Google.Protobuf.IMessage)}");
        }

        // Validate additional parameters
        for (int i = 0; i < additionalParameterTypes.Length; i++)
        {
            var actualType = parameters[i + 1].ParameterType;
            var expectedType = additionalParameterTypes[i];

            if (actualType != expectedType)
            {
                throw new Exception(
                    $"Invalid message handler signature: {handler.DeclaringType?.Name}.{handler.Name}. " +
                    $"Parameter {i + 1} expected type {expectedType.Name}, got {actualType.Name}");
            }
        }

        var instance = (IMessage)Activator.CreateInstance(messageWrapperType, [null])!;
        var messageId = instance.MessageId;

        var valueProperty = messageWrapperType.GetProperty("Value")!;

        HandlerEntries[messageId] = (handler, valueProperty);
    }

    public abstract void Dispatch(IMessage message);
}

[Serializable]
public class DispatchException(string message) : Exception(message);
