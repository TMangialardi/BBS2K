using BBS2K.Network;
using Microsoft.Extensions.Configuration;
using Serilog;
using STUN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BBS2K.Services
{
    public class ChatService
    {
        //private readonly ILogger _logger;
        //private readonly IConfiguration _configuration;
        //private readonly Peer _node;
        //private readonly StunHelper _stun;
        //private string _username = "User_" + Random.Shared.Next(1000);

        //public ChatService(int port, ILogger logger, IConfiguration configuration)
        //{
        //    _logger = logger;
        //    _configuration = configuration;
        //    _node = new Peer(port, _logger);
        //    _stun = new StunHelper(_logger, _configuration);

        //    _node.MessageReceived += OnMessageReceived;
        //    _node.PeerDisconnected += OnPeerDisconnected;
        //}

        //public async Task StartAsync(int port)
        //{
        //    await _node.StartListening();

        //    // Get public IP via STUN
        //    IPEndPoint publicEndPoint = await _stun.GetPublicEndpointAsync();
        //    Console.WriteLine($"Public endpoint: {publicEndPoint}");
        //}

        //public async Task JoinGroupAsync(string peerIp, int peerPort)
        //{
        //    await _node.ConnectAsync(peerIp, peerPort);
        //}

        //public async Task SendMessageAsync(string text)
        //{
        //    string formatted = $"[{_username}]: {text}";
        //    await _node.BroadcastMessage(formatted);
        //}

        //private void OnMessageReceived(string message)
        //{
        //    Console.WriteLine(message);
        //}

        //private void OnPeerDisconnected(string ip)
        //{
        //    Console.WriteLine($"[System] {ip} disconnected");
        //}
    }
}
