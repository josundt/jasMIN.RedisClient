using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace jasMIN.Redis
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1118:Utility classes should not have public constructors", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "<Pending>")]
    class Program
    {
        static async Task Main(string[] args)
        {

            // https://medium.com/concerning-pharo/quick-write-me-a-redis-client-5fbe4ddfb13d

            if (args.Length != 1)
            {
                Console.WriteLine("Wrong number of arguments");
            }

            if (!RedisUrlParser.TryParseRedisUrl(args[0], out var options))
            {
                WriteConsoleErrorLine("Invalid url argument");
                Console.WriteLine("Valid: tsp(s)://[credentials]@host:[port]");
            }

            using var client = new RedisClient(options);
            while (true)
            {
                var protocol = options.UseSsl ? "tcps" : "tcp";
                Console.Write($"{protocol}://{options.Host}:{options.Port}> ");
                var command = Console.ReadLine();
                if (command == "clear")
                {
                    Console.Clear();
                }
                else if (command != null)
                {
                    await TrySendCommandAsync(client, command);
                }
            }
        }

        private static async Task TrySendCommandAsync(RedisClient client, string command)
        {
            IEnumerable<string>? responseLines = null;
            try
            {
                responseLines = await client.SendCommandAsync(command);
            }
            catch (CommandException ex)
            {
                WriteConsoleErrorLine(ex.Message);
            }
            if (responseLines != null)
            {
                foreach(var l in responseLines)
                {
                    Console.WriteLine(l);
                }
            }

            Console.WriteLine();
        }

        private static void WriteConsoleErrorLine(string line)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(line);
            Console.ForegroundColor = oldColor;

        }
    }
}
