using BBS2K.Models;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using STUN;
using STUN.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BBS2K.Network
{
    public class StunHelper
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public StunHelper(ILogger logger, IConfiguration configuration)
        {
            this._logger = logger;
            _configuration = configuration;
        }

        public async Task<IPEndPoint> GetPublicEndpointAsync()
        {
            var logPrefix = "[GetPublicEndpointAsync@StunHelper]";

            _logger.Information($"{logPrefix} Reading STUN settings from configuration.");

            var stunSettings = _configuration.GetSection("StunSettings").Get<StunSettings>();
            if(stunSettings == null || string.IsNullOrEmpty(stunSettings.BaseUrl) || !stunSettings.Port.HasValue || !stunSettings.TimeOut.HasValue)
            {
                _logger.Error($"{logPrefix} Missing STUN settings in configuration.");
                throw new Exception("Missing STUN settings in configuration.");
            }

            _logger.Information($"{logPrefix} Finding the IP associated to the STUN server: {stunSettings.BaseUrl}.");

            var ip = await Dns.GetHostAddressesAsync(stunSettings.BaseUrl);
            if (ip.Length == 0)
            {
                _logger.Error($"{logPrefix} Could not find STUN server's IP.");
                throw new Exception("Could not find STUN server's IP.");
            }
            var stunEndpoint = new IPEndPoint(ip.First(), stunSettings.Port.Value);

            _logger.Information($"{logPrefix} IP associated to the STUN server found: {stunEndpoint.Address}.");

            STUNClient.ReceiveTimeout = stunSettings.TimeOut.Value;

            _logger.Information($"{logPrefix} Contacting the STUN server.");

            var queryResult = STUNClient.Query(stunEndpoint, STUNQueryType.ExactNAT, true);

            if (queryResult.QueryError != STUNQueryError.Success)
            {
                _logger.Error($"{logPrefix} STUN server query error: {queryResult.QueryError}.");
                throw new Exception("Query Error: " + queryResult.QueryError.ToString());
            }

            _logger.Information($"{logPrefix} Public IP found: {queryResult.PublicEndPoint}.");
            return queryResult.PublicEndPoint;
        }
    }
}
