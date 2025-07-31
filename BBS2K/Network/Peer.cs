using BBS2K.Models;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BBS2K.Network
{
    public class Peer
    {
        private readonly string _username;
        private readonly int _port;
        private readonly ILogger _logger;
        private readonly UdpClient _udpClient;

        private readonly ConcurrentDictionary<string, IPEndPoint> _peers;
        private CancellationTokenSource _cancellationTokenSource;

        public Peer(string username, int port, ILogger logger)
        {
            _username = username;
            _port = port;
            _logger = logger;
            _udpClient = new UdpClient(_port);
            _peers = new();
            _cancellationTokenSource = new();
        }

        public async Task StartAsync(IPEndPoint? initialPeer)
        {
            _logger.Information("[StartAsync@Peer] Starting peer.");

            _ = Task.Run(ListenForMessagesAsync, _cancellationTokenSource.Token);

            if (initialPeer != null)
            {
                _logger.Information("[StartAsync@Peer] initial peer available. Adding it to the peers list.");
                if (await AddPeerAsync(initialPeer).ConfigureAwait(false))
                {
                    await SendPeerRequestAsync(initialPeer).ConfigureAwait(false);
                }
            }
        }

        private async Task ListenForMessagesAsync()
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var receivedMessage = await _udpClient.ReceiveAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                    var sendingEndpoint = receivedMessage.RemoteEndPoint;

                    var decryptedMessage = string.Empty;

                    try
                    {
                        decryptedMessage = AesEncryption.Decrypt(Encoding.UTF8.GetString(receivedMessage.Buffer));
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"[ListenForMessagesAsync@Peer] Received a malformed message from peer {sendingEndpoint}: {ex.Message}.");
                        Console.WriteLine($"Received a malformed message from peer {sendingEndpoint}.");
                        continue;
                    }
                    var message = new P2PMessage();
                    try
                    {
                        message = JsonConvert.DeserializeObject<P2PMessage>(decryptedMessage);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"[ListenForMessagesAsync@Peer] Error deserializing message from peer {sendingEndpoint}: {ex.Message}.");
                        Console.WriteLine($"Error deserializing message from peer {sendingEndpoint}.");
                        continue;
                    }

                    if (string.IsNullOrEmpty(message?.Sender))
                    {
                        continue;
                    }

                    await AddPeerAsync(sendingEndpoint).ConfigureAwait(false);

                    _logger.Information($"[ListenForMessagesAsync@Peer] Received a {message.MessageType} message from peer {message.Sender}@{sendingEndpoint}.");

                    switch (message?.MessageType)
                    {
                        case MessageType.Chat:
                            Console.WriteLine($"[{message.Sender}@{sendingEndpoint}] {message.Content}");
                            break;
                        case MessageType.PeerRequest:
                            Console.WriteLine($"{message.Sender}@{sendingEndpoint} requested the peer list.");

                            var peersList = _peers.Keys.ToList();
                            var response = new P2PMessage()
                            {
                                MessageType = MessageType.PeerResponse,
                                Sender = _username,
                                Content = JsonConvert.SerializeObject(peersList)
                            };

                            _ = SendMessageAsync(response, sendingEndpoint);
                            break;
                        case MessageType.PeerResponse:
                            var discoveredPeers = JsonConvert.DeserializeObject<string[]>(message.Content ?? "[]");

                            if (discoveredPeers != null)
                            {
                                Console.WriteLine($"Received peer list with {discoveredPeers.Length} addresses from {sendingEndpoint}.");
                                foreach (var peerAddr in discoveredPeers)
                                {
                                    if (IPEndPoint.TryParse(peerAddr, out var newPeerEp))
                                    {
                                        if (await AddPeerAsync(newPeerEp).ConfigureAwait(false))
                                        {
                                            await SendPeerRequestAsync(newPeerEp).ConfigureAwait(false);
                                        }
                                    }
                                }
                            }

                            break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Error($"[ListenForMessagesAsync@Peer] Listener stopped.");
                Console.WriteLine("Bye!");
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
            {
                _logger.Error($"[ListenForMessagesAsync@Peer] Listener stopped due to a socket interruption.");
                Console.WriteLine("Bye!");
            }
            catch (Exception ex)
            {
                _logger.Error($"[ListenForMessagesAsync@Peer] Error in listener: {ex.Message}.");
                Console.WriteLine($"Error in listener: {ex.Message}.");
            }
            
        }

        public async Task SendMessageAsync(P2PMessage message, IPEndPoint receiver)
        {
            _logger.Information($"[SendMessageAsync@Peer] Sending message to {receiver}. Sender: {message.Sender}; Type: {message.MessageType}.");

            if (string.IsNullOrEmpty(message.Sender))
                message.Sender = _username;

            var encryptedJsonMessage = AesEncryption.Encrypt(JsonConvert.SerializeObject(message));

            var messageBytes = Encoding.UTF8.GetBytes(encryptedJsonMessage);

            var result = await _udpClient.SendAsync(messageBytes, messageBytes.Length, receiver);

            _logger.Information($"[SendMessageAsync@Peer] Message sent to {receiver}. Output: {result}.");
        }

        public async Task BroadcastChatMessageAsync(string message)
        {
            var fullMessage = new P2PMessage()
            {
                Sender = _username,
                Content = message,
                MessageType = MessageType.Chat
            };

            var allPeers = _peers.Values.ToList();

            _logger.Information($"[SendMessageAsync@Peer] Broadcasting message to {allPeers.Count} peers.");

            foreach (var peerEndPoint in allPeers)
            {
                try
                {
                    await SendMessageAsync(fullMessage, peerEndPoint);
                }
                catch (Exception ex)
                {
                    _logger.Error($"[SendMessageAsync@Peer] Failed to send message to {peerEndPoint}: {ex.Message}.");
                    Console.WriteLine($"Failed to send message to {peerEndPoint}: {ex.Message}.");
                    // TODO: Gestire peer che non rispondono
                }
            }
        }

        private async Task SendPeerRequestAsync(IPEndPoint destination)
        {
            _logger.Information($"[SendPeerRequestAsync@Peer] Sending peer request message to {destination}.");

            var message = new P2PMessage()
            {
                MessageType = MessageType.PeerRequest,
                Sender = _username,
            };
            await SendMessageAsync(message, destination);
        }

        private async Task<bool> AddPeerAsync(IPEndPoint peerEndPoint)
        {
            _logger.Information($"[AddPeer@Peer] Trying to add peer {peerEndPoint} to the list.");

            if (peerEndPoint.Port == _port && (IPAddress.IsLoopback(peerEndPoint.Address) ||
                peerEndPoint.Address.Equals((_udpClient.Client.LocalEndPoint as IPEndPoint)?.Address) ||
                Dns.GetHostEntry(Dns.GetHostName()).AddressList.Contains(peerEndPoint.Address)))
            {
                _logger.Warning($"[AddPeer@Peer] Pear not added because its address corresponds to this peer's address.");
                return false;
            }

            if (_peers.TryAdd(peerEndPoint.ToString(), peerEndPoint))
            {
                _logger.Information($"[AddPeer@Peer] Correctly added {peerEndPoint} to the list.");
                return true;
            }
            else
            {
                _logger.Warning($"[AddPeer@Peer] The peer {peerEndPoint} was already in the list.");
                return false;
            }
        }

        public void PrintPeers()
        {
            _logger.Information($"[PrintPeers@Peer] Printing known peers.");

            if (_peers.IsEmpty)
            {
                Console.WriteLine("No other peers known.\n\n");
                return;
            }

            Console.WriteLine("--- Known Peers ---");
            foreach (var peerAddress in _peers.Keys)
            {
                Console.WriteLine($"- {peerAddress}");
            }
            Console.WriteLine("-------------------\n\n");
        }

        public void Stop()
        {
            _logger.Information("[Stop@Peer] Stopping peer.");
            _cancellationTokenSource.Cancel();
            _udpClient.Close();
        }
    }
}
