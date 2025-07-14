using BBS2K.Models;
using BBS2K.Network;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBS2K
{
    public class InitializationHelper
    {
        //Dynamic and private TCP/UDP ports range
        public const int MIN_PORT = 49152;
        public const int MAX_PORT = 65535;

        private int chosenPort;
        private string chosenNickname;
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

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            logger.Information($"{logPrefix} Startup completed. Reading port from appsettings.");

            var defaultSettings = configuration.GetSection("DefaultSettings").Get<DefaultSettings>();

            Console.WriteLine("Welcome to BBS2K!\n\n");

            if (defaultSettings == null)
                throw new Exception("Missing DefaultSettings section inside the AppSettings file.");

            if (defaultSettings!.Port == null || defaultSettings!.Port.Value < MIN_PORT || defaultSettings.Port.Value > MAX_PORT)
            {
                logger.Information($"{logPrefix} Port missing or not valid. Asking for a value.");

                var validPort = false;
                Console.WriteLine("Insert a port:\n\n");

                while (!validPort)
                {
                    validPort = int.TryParse(Console.ReadLine(), out chosenPort);
                    if (!validPort || chosenPort < MIN_PORT || chosenPort > MAX_PORT)
                    {
                        logger.Information($"{logPrefix} Port not valid. Asking again.");
                        Console.WriteLine("Port not valid. Try again:\n\n");
                        validPort = false;
                    }
                }
                logger.Information($"{logPrefix} Port selected correctly. Port number: {chosenPort}.");
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

            var stunHelper = new StunHelper(logger);
            var stun = await stunHelper.GetPublicEndpointAsync();
        }

        public int GetPort()
        {
            return this.chosenPort;
        }

        public string GetNickname()
        {
            return this.chosenNickname;
        }
    }
}
