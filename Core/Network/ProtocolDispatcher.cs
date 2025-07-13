using System.Reflection;
using Google.Protobuf;
using Spire.Protocol;
using Spire.Protocol.Auth;
using Spire.Protocol.Game;
using Spire.Protocol.Net;

namespace Spire.Core.Network;

public abstract class ProtocolDispatcher
{
    private static readonly Dictionary<
        ProtocolCategory, Dictionary<Type, Action<ISessionContext, IMessage>>> Handlers = [];

    public static void Register(Assembly assembly)
    {
        var handlers = assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.IsDefined(typeof(ProtocolHandlerAttribute), false));

        foreach (var handler in handlers)
        {
            var parameters = handler.GetParameters();
            if (parameters.Length != 2 ||
                parameters[0].ParameterType != typeof(ISessionContext) ||
                !typeof(IMessage).IsAssignableFrom(parameters[1].ParameterType))
            {
                throw new Exception(
                    $"[WARN] Invalid protocol handler: {handler.DeclaringType?.Name}.{handler.Name}");
            }
            
            var protocolType = parameters[1].ParameterType;
            var protocolCategory = GetCategory(protocolType);
            if (protocolCategory == ProtocolCategory.None)
            {
                throw new Exception($"[WARN] Invalid protocol category for {protocolType.Name}");
            }

            if (!Handlers.ContainsKey(protocolCategory))
                Handlers[protocolCategory] = [];
            Handlers[protocolCategory][protocolType] = (ctx, protocol) =>
            {
                handler.Invoke(null, [ctx, protocol]);
            };
        }
    }
    
    public void Dispatch(ISessionContext ctx, IngressProtocol protocol)
    {
        var i = typeof(int);
        
        switch (protocol.Category)
        {
            case ProtocolCategory.Auth:
                DispatchInternal<AuthServerProtocol>(ctx, protocol);
                break;
            case ProtocolCategory.Game:
                DispatchInternal<GameServerProtocol>(ctx, protocol);
                break;
            case ProtocolCategory.Net:
                DispatchInternal<NetServerProtocol>(ctx, protocol);
                break;
            case ProtocolCategory.None:
            default:
                HandleError(
                    ctx,
                    protocol,
                    new Exception("Unknown protocol category"));
                break;
        }
    }

    private void DispatchInternal<T>(ISessionContext ctx, IngressProtocol protocol)
    where T : IMessage<T>, new()
    {
        var p = new T().Descriptor.Parser.ParseFrom(protocol.Data);
        if (p == null)
        {
            HandleError(
                ctx,
                protocol,
                new Exception($"Failed to parse {nameof(T)}"));
            return;
        }

        var type = p.GetType();
        if (!Handlers[protocol.Category].TryGetValue(type, out var handler))
        {
            HandleError(
                ctx,
                protocol,
                new Exception($"Handler for {nameof(type)} is not registered"));
            return;
        }

        handler(ctx, p);
    }

    protected abstract void HandleError(ISessionContext ctx, IngressProtocol protocol, Exception e);

    private static ProtocolCategory GetCategory(Type protocolType)
    {
        var ns = protocolType.Namespace ?? "";
        if (ns.Contains("Spire.Protocol.Auth")) return ProtocolCategory.Auth;
        if (ns.Contains("Spire.Protocol.Game")) return ProtocolCategory.Game;
        if (ns.Contains("Spire.Protocol.Net")) return ProtocolCategory.Net;
        return ProtocolCategory.None;
    }
}