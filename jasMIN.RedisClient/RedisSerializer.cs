using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace jasMIN.Redis
{
    class RedisSerializer
    {
        private const bool _simpleCommandSerialization = true;
        private static readonly Regex _trailingLinefeedRegex = new(@"\r\n$");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
        public string Serialize(string command)
        {
            var words = command.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (_simpleCommandSerialization)
            {
                return $"{string.Join(" ", words)}\r\n";
            }
            else
            {
#pragma warning disable CS0162 // Unreachable code detected
                var length = words.Length;
                return $"*{length}\r\n$3\r\n{string.Join("\r\n$3\r\n", words)}\r\n";
#pragma warning restore CS0162 // Unreachable code detected
            }
        }

        public string Deserialize(string response)
        {
            switch (response[0])
            {
                case '+': // String
                    return $@"""{response.Trim()[1..]}""";
                case ':': // Number
                    return $@"{response.Trim()[1..]}";
                case '*': // Array
                    var lines = response.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                    var items = new List<string>();
                    for (var i = 1; i < lines.Length; i++) // Start on index 1 to skip array length line
                    {
                        if (lines[i][0] == '$' && lines[i] != "$-1") // If bulk string, item consist of two lines
                        {
                            items.Add(string.Join("\r\n", lines[i], lines[i + 1]));
                            i++;
                        } 
                        else
                        {
                            items.Add(lines[i]);
                        }
                    }
                    return string.Join("\r\n", items.Select(e => this.Deserialize(e)));
                case '$': // Bulk string
                    var parts = response.Split("\r\n", StringSplitOptions.RemoveEmptyEntries); // Do not include the byt lenth prefix line
                    return parts.Length == 2 ? $@"""{parts[1]}""" : "(null)";
                case '-': // Error
                    throw new CommandException(response.Trim()[1..]);
                default:
                    return _trailingLinefeedRegex.Replace($@"{response}", "");

            }
        }
    }
}
