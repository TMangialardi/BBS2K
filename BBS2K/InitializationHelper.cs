using BBS2K.Models;
using BBS2K.Network;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BBS2K
{
    public class InitializationHelper
    {
        //Dynamic and private TCP/UDP ports range
        public const int MIN_PORT = 10000;
        public const int MAX_PORT = 20000;
        private const string GREETING = "Commands:\n\n/exit to leave\n/peers to see known peers\n/myaddress to see your address and share it\n/help to get this help message\n";

        private int chosenPort;
        private string chosenNickname;
        private IPEndPoint stunData;
        private IPEndPoint? initialPeer;
        private Logger logger;


        public InitializationHelper()
        {
            chosenPort = 0;
            chosenNickname = string.Empty;
        }

        public async Task Initialize()
        {
            var logPrefix = "[Initialize@InitializerHelper]";
            IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

            logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            logger.Information($"{logPrefix} Startup completed. Reading port from AppSettings.");

            var defaultSettings = configuration.GetSection("DefaultSettings").Get<DefaultSettings>();

            Console.WriteLine("Welcome to BBS2K!\n\n");
            Console.WriteLine(GREETING);

            if (defaultSettings == null)
            {
                logger.Information($"{logPrefix} Missing DefaultSettings section inside the AppSettings file.");
                throw new Exception("Missing DefaultSettings section inside the AppSettings file.");
            }

            if (defaultSettings!.Port == null || defaultSettings!.Port.Value < MIN_PORT || defaultSettings.Port.Value >= MAX_PORT)
            {
                logger.Information($"{logPrefix} Port missing or not valid. Generating a value.");

                chosenPort = new Random().Next(MIN_PORT, MAX_PORT);

                logger.Information($"{logPrefix} Port generated. Port number: {chosenPort}.");
                defaultSettings!.Port = chosenPort;
            }
            else
            {
                logger.Information($"{logPrefix} Port configured inside the AppSettings file. Port number: {defaultSettings.Port}.");
                Console.WriteLine($"Using the port {defaultSettings.Port}\n\n");
            }

            if (string.IsNullOrEmpty(defaultSettings!.Nickname))
            {
                logger.Information($"{logPrefix} Nickname missing. Asking for a value.");

                Console.WriteLine("Insert your nickname:\n\n");

                var validNickname = false;

                while (!validNickname)
                {
                    chosenNickname = Console.ReadLine();
                    if(!string.IsNullOrEmpty(chosenNickname))
                        validNickname = true;
                }

                logger.Information($"{logPrefix} Nickname selected correctly. Nickname: {chosenNickname}.");
            }
            else
            {
                logger.Information($"{logPrefix} Nickname configured inside the AppSettings file. Nickname: {defaultSettings.Nickname}.");
                Console.WriteLine($"Your nickname is {defaultSettings.Nickname}\n\n");
            }

            logger.Information($"{logPrefix} Configuring initial peer.");

            Console.WriteLine($"Enter the address of a known peer [e.g. 12.34.56.78:12345] to join a chat.\n" +
                $"Press Enter to start a new chat.\n");
            var validPeer = false;
            while (!validPeer)
            {
                var initialPeerAddress = Console.ReadLine()?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(initialPeerAddress))
                {
                    validPeer = true;
                }
                else
                {
                    try
                    {
                        initialPeer = IPEndPoint.Parse(initialPeerAddress);
                        validPeer = true;
                    }
                    catch (Exception e)
                    {
                        logger.Error($"{logPrefix} Invalid peer address format: {e.Message}. Asking to try again.");
                        Console.WriteLine($"Invalid peer address format. Try again.\n\n");
                    }
                }
            }


            if(!(initialPeer == null))
            {
                Console.WriteLine($"Joining a chat via {initialPeer}...");
            }
            else
            {
                var stunHelper = new StunHelper(logger, configuration);
                stunData = await stunHelper.GetPublicEndpointAsync();

                logger.Information($"{logPrefix} STUN address: {stunData.Address}.");
                Console.WriteLine($"Your public address is {stunData.Address}:{defaultSettings.Port}. Share it with your friends to let them connect.\n\n");
            }

            Console.WriteLine($"You are '{this.chosenNickname}'. Happy chatting!\n");
        }

        public int GetPort()
        {
            return this.chosenPort;
        }

        public string GetNickname()
        {
            return this.chosenNickname;
        }

        public static string GetGreeting()
        {
            return GREETING;
        }

        public IPEndPoint GetStunData()
        {
            return stunData;
        }

        public ILogger GetLogger()
        {
            return this.logger;
        }

        public IPEndPoint? GetInitialPeer()
        {
            return this.initialPeer;
        }
    }
}
