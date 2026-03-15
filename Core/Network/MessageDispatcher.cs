using System.Reflection;
using Spire.Message.Game;

namespace Spire.Core.Network;

/// <summary>
/// Two-phase message dispatcher. Handlers are split into core and frontend:
///
/// - Core handlers live in the Core assembly and mutate shared GameState.
///   Signature: Handle(ProtoMsg data, GameState state)
///
/// - Frontend handlers live in their respective frontend assembly (Bot/Engine)
///   and perform frontend-specific reactions (logging, UI, automation).
///   Signature varies by frontend:
///     Bot:    Handle(ProtoMsg data, BotContext ctx)
///     Engine: Handle(ProtoMsg data)
///
/// Which set a handler belongs to is determined by assembly, not by attribute.
/// On dispatch, the core handler runs first (state mutation), then the frontend
/// handler (reaction). Either or both may be absent for a given message.
/// </summary>
public abstract class MessageDispatcher
{
    private readonly Dictionary<ushort, (MethodInfo handler, PropertyInfo valueProperty)> _coreHandlers = [];
    private readonly Dictionary<ushort, (MethodInfo handler, PropertyInfo valueProperty)> _frontendHandlers = [];

    protected void RegisterCoreHandlers(Assembly assembly, params Type[] additionalParameterTypes)
        => RegisterHandlers(assembly, additionalParameterTypes, _coreHandlers);

    protected void RegisterFrontendHandlers(Assembly assembly, params Type[] additionalParameterTypes)
        => RegisterHandlers(assembly, additionalParameterTypes, _frontendHandlers);

    private static void RegisterHandlers(Assembly assembly, Type[] additionalParameterTypes,
        Dictionary<ushort, (MethodInfo handler, PropertyInfo valueProperty)> target)
    {
        var handlers = assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.IsDefined(typeof(MessageHandlerAttribute), false));

        foreach (var handler in handlers)
        {
            Register(handler, additionalParameterTypes, target);
        }
    }

    private static void Register(MethodInfo handler, Type[] additionalParameterTypes,
        Dictionary<ushort, (MethodInfo handler, PropertyInfo valueProperty)> target)
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

        target[messageId] = (handler, valueProperty);
    }

    public void Dispatch(IMessage message)
    {
        var handled = false;

        if (_coreHandlers.TryGetValue(message.MessageId, out var coreEntry))
        {
            try
            {
                var messageData = coreEntry.valueProperty.GetValue(message);
                coreEntry.handler.Invoke(null, BuildCoreArgs(messageData));
                handled = true;
            }
            catch (Exception e)
            {
                OnDispatchError(message, e);
            }
        }

        if (_frontendHandlers.TryGetValue(message.MessageId, out var frontendEntry))
        {
            try
            {
                var messageData = frontendEntry.valueProperty.GetValue(message);
                frontendEntry.handler.Invoke(null, BuildFrontendArgs(messageData));
                handled = true;
            }
            catch (Exception e)
            {
                OnDispatchError(message, e);
            }
        }

        if (!handled)
            OnUnhandledMessage(message);
    }

    protected abstract object?[] BuildCoreArgs(object? messageData);
    protected abstract object?[] BuildFrontendArgs(object? messageData);
    protected abstract void OnUnhandledMessage(IMessage message);
    protected abstract void OnDispatchError(IMessage message, Exception e);
}
