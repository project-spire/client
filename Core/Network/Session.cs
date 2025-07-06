using System.Buffers;
using Spire.Protocol;
using System.IO.Pipelines;
using System.Net.Sockets;

namespace Spire.Core.Network;

public sealed class Session
{
    private readonly Socket _socket;
    private readonly PipeReader _reader;
    private readonly PipeWriter _writer;
    private readonly CancellationTokenSource _cancellation = new();
    
    public Session(Socket socket)
    {
        _socket = socket;
        
        var pipe = new Pipe();
        _reader = pipe.Reader;
        _writer = pipe.Writer;
    }

    public void Start()
    {
        
    }

    public async void Stop()
    {
        
    }

    public async Task StartReceive()
    {
        while (true)
        {
            var (category, body) =  await Receive();
        }
    }

    private async ValueTask<(ProtocolCategory, byte[])> Receive()
    {
        var headerResult = await _reader.ReadAtLeastAsync(
            ProtocolHeader.HeaderSize, _cancellation.Token);
        if (headerResult.IsCanceled)
            return (ProtocolCategory.None, null);
        
        var (category, length) = ProtocolHeader.Read(headerResult.Buffer);
        
        headerResult.Buffer.ToArray()
        
        _reader.;
    }
}
