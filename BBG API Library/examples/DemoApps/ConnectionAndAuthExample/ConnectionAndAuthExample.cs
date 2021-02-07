/* Copyright 2012. Bloomberg Finance L.P.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:  The above
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
using System;
using System.Collections.Generic;
using Bloomberglp.Blpapi;

namespace Bloomberglp.BlpapiExamples.DemoApps
{
    public class ConnectionAndAuthExample
    {
        class HostAndPort
        {
            string d_host;
            int    d_port;
            public HostAndPort(string host, int port) {
                d_host = host;
                d_port = port;
            }
            public string host() {
                return d_host;
            }
            public int port() {
                return d_port;
            }
        }

        private const string AUTH_OPTION_NONE = "none";
        private const string AUTH_OPTION_USER = "user";
        private const string AUTH_OPTION_APP = "app=";
        private const string AUTH_OPTION_DIR = "dir=";
        private const string AUTH_OPTION_MANUAL = "manual=";
        private const string AUTH_OPTION_USERAPP = "userapp=";

        private const string d_defaultHost      = "localhost";
        private const int    d_defaultPort      = 8194;

        private List<HostAndPort> d_hosts       = new List<HostAndPort>();

        private string d_clientCredentials         = null;
        private string d_clientCredentialsPassword = null;
        private string d_trustMaterial             = null;

        private AuthOptions d_authOptions = new AuthOptions(AuthUser.CreateWithLogonName());

        public void Run(string[] args)
        {
            if (!ParseCommandLine(args)) return;

            SessionOptions sessionOptions = new SessionOptions();
            SessionOptions.ServerAddress[] servers = new SessionOptions.ServerAddress[d_hosts.Count];
            for (int i = 0; i < d_hosts.Count; ++i)
            {
                servers[i] = new SessionOptions.ServerAddress(d_hosts[i].host(),
                                                              d_hosts[i].port());
            }

            sessionOptions.ServerAddresses = servers;
            sessionOptions.AutoRestartOnDisconnection = true;
            sessionOptions.NumStartAttempts = d_hosts.Count;
            sessionOptions.SetSessionIdentityOptions(d_authOptions);

            if (d_clientCredentials != null && d_trustMaterial != null) {
                using (System.Security.SecureString password = new System.Security.SecureString())
                {
                    foreach (var c in d_clientCredentialsPassword)
                    {
                        password.AppendChar(c);
                    }

                    TlsOptions tlsOptions = TlsOptions.CreateFromFiles(d_clientCredentials, password, d_trustMaterial);
                    sessionOptions.TlsOptions = tlsOptions;
                }
            }

            System.Console.WriteLine("Connecting to: ");
            foreach (HostAndPort host in d_hosts)
            {
                System.Console.WriteLine(host.host() + ":" + host.port() + " ");
            }
            Session session = new Session(sessionOptions);

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

            while (true)
            {
                var nextEvent = session.NextEvent();
                foreach (Message message in nextEvent)
                {
                    Console.WriteLine(message.AsElement);
                }
            }
        }

        private void PrintUsage()
        {
            Console.WriteLine(@"
Usage:
    [-host <ipAddress:port>]  server name or IP (default: localhost)
    [-auth <option>]          authentication option (default: user):
        none                  applicable to Desktop API product that requires
                              Bloomberg Professional service to be installed locally.
        user                  as a user using OS logon information
        dir=<property>        as a user using directory services
        app=<app>             as the specified application
        userapp=<app>         as user and application using logon information for the user
        manual=<app,ip,user>  as user and application, with manually provided
                              IP address and EMRS user

    TLS OPTIONS (specify all or none):
        [-tls-client-credentials <file>]           name a PKCS#12 file to use as a source of client credentials
        [-tls-client-credentials-password <file>]  specify password for accessing client credentials
        [-tls-trust-material <file>]               name a PKCS#7 file to use as a source of trusted certificates
Press ENTER to quit");
        }

        private HostAndPort ParseHostAndPort(string arg) {
            string[] parms = arg.Split(':');
            int port = 8194;
            if (parms.Length > 2) {
                Console.Error.WriteLine("Invalid argument to -host: " + arg);
                PrintUsage();
                return null;
            }
            if (parms.Length == 2) {
                if (!Int32.TryParse(parms[1], out port)) {
                    Console.Error.WriteLine("Invalid argument to -host: " + arg);
                    PrintUsage();
                    return null;
                }
            }
            return new HostAndPort(parms[0], port);
        }

        private bool ParseCommandLine(string[] args)
        {
            try
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    if (string.Compare("-host", args[i], true) == 0 && i + 1 < args.Length)
                    {
                        ++i;
                        HostAndPort host = ParseHostAndPort(args[i]);
                        if (host == null) {
                            return false;
                        }
                        d_hosts.Add(host);
                    }
                    else if (string.Compare("-auth", args[i], true) == 0 && i + 1 < args.Length)
                    {
                        ++i;
                        if (string.Compare(AUTH_OPTION_NONE, args[i], true) == 0)
                        {
                            d_authOptions = new AuthOptions();
                        }
                        else if (string.Compare(AUTH_OPTION_USER, args[i], true) == 0)
                        {
                            d_authOptions = new AuthOptions(AuthUser.CreateWithLogonName());
                        }
                        else if (string.Compare(AUTH_OPTION_APP, 0, args[i], 0,
                                            AUTH_OPTION_APP.Length, true) == 0)
                        {
                            string appName = args[i].Substring(AUTH_OPTION_APP.Length);
                            d_authOptions = new AuthOptions(new AuthApplication(appName));
                        }
                        else if (string.Compare(AUTH_OPTION_DIR, 0, args[i], 0,
                                            AUTH_OPTION_DIR.Length, true) == 0)
                        {
                            string dir = args[i].Substring(AUTH_OPTION_DIR.Length);
                            d_authOptions = new AuthOptions(AuthUser.CreateWithActiveDirectoryProperty(dir));
                        }
                        else if (string.Compare(AUTH_OPTION_USERAPP, 0, args[i], 0,
                                     AUTH_OPTION_USERAPP.Length, true) == 0)
                        {
                            string appName = args[i].Substring(AUTH_OPTION_USERAPP.Length);
                            d_authOptions = new AuthOptions(
                                AuthUser.CreateWithLogonName(),
                                new AuthApplication(appName));
                        }
                        else if (string.Compare(AUTH_OPTION_MANUAL, 0, args[i],
                                    0, AUTH_OPTION_MANUAL.Length, true) == 0)
                        {
                            string[] parms = args[i].Substring(AUTH_OPTION_MANUAL.Length).Split(',');
                            if (parms.Length != 3) {
                                PrintUsage();
                                return false;
                            }

                            string appName = parms[0];
                            string ip = parms[1];
                            string userId = parms[2];
                            d_authOptions = new AuthOptions(
                                AuthUser.CreateWithManualOptions(userId, ip),
                                new AuthApplication(appName));
                        }
                        else
                        {
                            PrintUsage();
                            return false;
                        }
                    }
                    else if (string.Compare("-tls-client-credentials", args[i], true) == 0 && i + 1 < args.Length)
                    {
                        d_clientCredentials = args[++i];
                    }
                    else if (string.Compare("-tls-client-credentials-password", args[i], true) == 0 && i + 1 < args.Length)
                    {
                        d_clientCredentialsPassword = args[++i];
                    }
                    else if (string.Compare("-tls-trust-material", args[i], true) == 0 && i + 1 < args.Length)
                    {
                        d_trustMaterial = args[++i];
                    }
                    else
                    {
                        PrintUsage();
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                PrintUsage();
                return false;
            }

            if (d_hosts.Count == 0)
            {
                d_hosts.Add(new HostAndPort(d_defaultHost, d_defaultPort));
            }

            return true;
        }

        public static void Main(string[] args)
        {
            ConnectionAndAuthExample example = new ConnectionAndAuthExample();
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
