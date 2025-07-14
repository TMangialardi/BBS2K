using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BBS2K.Network
{
    public class Peer
    {
        private readonly TcpListener _listener;
        private readonly List<TcpClient> _connectedPeers;
        private readonly int _port;
        private readonly ILogger _logger;
        private bool _isRunning;

        public event Action<string>? MessageReceived;
        public event Action<string>? PeerDisconnected;

        public Peer(int port, ILogger logger)
        {
            _port = port;
            _logger = logger;
            _listener = new(IPAddress.Any, _port);
            _connectedPeers = [];
        }

        public async Task StartListening()
        {
            _isRunning = true;
            _listener.Start();

            await Task.Run(async () =>
            {
                while (_isRunning)
                {
                    try
                    {
                        var client =  await _listener.AcceptTcpClientAsync();
                        lock (_connectedPeers)
                        {
                            _connectedPeers.Add(client);

                        }
                        await HandlePeer(client);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex);      //TODO: MIGLIORARE
                    }
                }
            });            
        }

        private async Task HandlePeer(TcpClient tcpClient)
        {
            ArgumentNullException.ThrowIfNull(tcpClient);

            using var stream = tcpClient.GetStream();
            byte[] buffer = new byte[4096];

            try
            {
                while (_isRunning)      //TODO: VERIFICARE IL COMPORTAMENTO
                {
                    int bytesRead = await stream.ReadAsync(buffer);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    MessageReceived?.Invoke(AesEncryption.Decrypt(message));
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);      //TODO: MIGLIORARE
            }
            finally
            {
                lock (_connectedPeers)
                {
                    _connectedPeers.Remove(tcpClient);
                }
                tcpClient.Close();
            }
        }

        public async Task BroadcastMessage(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(AesEncryption.Encrypt(message));

            foreach (var peer in _connectedPeers.ToArray())
            {
                try
                {
                    await peer.GetStream().WriteAsync(data);
                }
                catch
                {
                    lock (_connectedPeers)
                    {
                        _connectedPeers.Remove(peer);

                    }
                }
            }  
        }

        public async Task ConnectToPeers(List<string> peers)
        {
            foreach (var peerIP in peers)
            {
                try
                {
                    var client = new TcpClient();
                    await client.ConnectAsync(peerIP, _port);
                    lock (_connectedPeers)
                    {
                        _connectedPeers.Add(client); //valutare se rimpiazzare connectedPeers con una concurrentBag
                    }
                    await HandlePeer(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);      //TODO: MIGLIORARE
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            foreach (TcpClient peer in _connectedPeers)
                peer.Close();
        }
    }
}
