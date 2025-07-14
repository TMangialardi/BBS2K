using BBS2K;
using BBS2K.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Serilog;
using System;
using System.Security.Cryptography;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    static async Task Main(string[] args)
    {
        var initialization = new InitializationHelper();
        await initialization.Initialize();
    }
}