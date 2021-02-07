/* Copyright 2019. Bloomberg Finance L.P.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions: The above
 * copyright notice and this permission notice shall be included in all copies
 * or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
 * IN THE SOFTWARE.
 */
//  SessionIdentityExample:
//  =========================
//
//  This example shows how to authorize the session identity.
//
using System;
using Bloomberglp.Blpapi;

namespace Bloomberglp.BlpapiExamples.DemoApps
{
    public class SessionIdentityExample
    {
        private const string AuthOptionUser = "user";
        private const string AuthOptionApp = "app=";
        private const string AuthOptionUserApp = "userapp=";

        private const string HostArgument = "-host";
        private const string AuthArgument = "-auth";

        private AuthOptionsType authOptionsType = AuthOptionsType.None;
        private string host;
        private int port;
        private string appName;

        /// <summary>
        /// The type of authorization being used.
        /// </summary>
        private enum AuthOptionsType
        {
            None,
            User,
            App,
            UserApp,
        }

        public void Run(string[] args)
        {
            if (!ParseCommandLine(args))
            {
                PrintUsage();
                return;
            }

            AuthOptions authOptions;

            switch (this.authOptionsType)
            {
                case AuthOptionsType.None:
                    authOptions = new AuthOptions();
                    break;
                case AuthOptionsType.User:
                    authOptions = new AuthOptions(AuthUser.CreateWithLogonName());
                    break;
                case AuthOptionsType.App:
                    authOptions = new AuthOptions(new AuthApplication(this.appName));
                    break;
                case AuthOptionsType.UserApp:
                    authOptions = new AuthOptions(
                        AuthUser.CreateWithLogonName(),
                        new AuthApplication(this.appName));
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unexpected '{nameof(AuthOptionsType)}'.");
            }

            var serverAddresses = new []
            {
                new SessionOptions.ServerAddress(this.host, this.port)
            };

            var sessionOptions = new SessionOptions
            {
                AutoRestartOnDisconnection = true,
                ServerAddresses = serverAddresses
            };

            sessionOptions.SetSessionIdentityOptions(authOptions);

            var session = new Session(sessionOptions);

            if (!session.Start())
            {
                Console.Error.WriteLine("Failed to start session.");

                while (true)
                {
                    var nextEvent = session.TryNextEvent();
                    if (nextEvent == null)
                    {
                        break;
                    }

                    foreach (var message in nextEvent.GetMessages())
                    {
                        Console.WriteLine(message);
                    }
                }
            }

            var startTime = DateTime.Now;
            const int TenSeconds = 10;
            while ((DateTime.Now - startTime).Seconds < TenSeconds)
            {
                var nextEvent = session.TryNextEvent();
                if (nextEvent != null)
                {
                    foreach (Message message in nextEvent)
                    {
                        Console.WriteLine(message.AsElement);
                    }
                }
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine($@"
Example of session identity authorization mechanisms:
Usage:
    [{HostArgument} <ipAddress:port>]    server name or IP and port. Defaults to 'localhost:8194'
    [{AuthArgument} <option>]            authorization options (default: none):
        none           applicable to Desktop API product that requires
                       Bloomberg Professional service to be installed locally
        {AuthOptionUser}           as a user using OS logon information
        {AuthOptionApp}<app>      as the specified application
        {AuthOptionUserApp}<app>  as user and application using logon information for the user
Press ENTER to quit");
        }

        private bool ParseCommandLine(string[] args)
        {
            bool isEndpointProvidedByUser = false;

            for (int i = 0; i < args.Length; ++i)
            {
                if (HostArgument.Equals(args[i], StringComparison.InvariantCulture) && i + 1 < args.Length)
                {
                    isEndpointProvidedByUser = true;

                    var tokens = args[++i].Split(':');

                    if (tokens.Length != 2)
                    {
                        Console.WriteLine($"Invalid argument to '{HostArgument}'.");
                        return false;
                    }

                    this.host = tokens[0];

                    if (!int.TryParse(tokens[1], out this.port))
                    {
                        Console.WriteLine($"Invalid argument to '{HostArgument}'.");
                        return false;
                    }
                }
                else if (AuthArgument.Equals(args[i], StringComparison.InvariantCulture) && i + 1 < args.Length)
                {
                    ++i;
                    if (AuthOptionUser.Equals(args[i], StringComparison.InvariantCulture))
                    {
                        this.authOptionsType = AuthOptionsType.User;
                    }
                    else if (args[i].StartsWith(AuthOptionApp))
                    {
                        this.authOptionsType = AuthOptionsType.App;
                        this.appName = args[i].Substring(AuthOptionApp.Length);
                    }
                    else if (args[i].StartsWith(AuthOptionUserApp))
                    {
                        this.authOptionsType = AuthOptionsType.UserApp;
                        this.appName = args[i].Substring(AuthOptionUserApp.Length);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (!isEndpointProvidedByUser)
            {
                this.host = "localhost";
                this.port = 8194;
            }

            return true;
        }

        public static void Main(string[] args)
        {
            var example = new SessionIdentityExample();
            try
            {
                example.Run(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.ReadLine();
        }
    }
}
