namespace Spire.Core.Network;

public interface ISessionContext
{
    public Session Session { get; }
    
    public void HandleError(DispatchException e);
}
