/* Copyright 2020. Bloomberg Finance L.P.
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

namespace Bloomberglp.BlpapiExamples.UnitTests.MktNotifier.MktNotifierApp
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The options for the application, such as host, port, topics,
    /// authorization options and so on.
    /// </summary>
    public class AppOptions
    {
        private const string AuthOptionNone = "none";
        private const string AuthOptionUser = "user";
        private const string AuthOptionApp = "app=";
        private const string AuthOptionUserApp = "userapp=";
        private const string AuthOptionDir = "dir=";

        private const string AuthUser = "AuthenticationType=OS_LOGON";
        private const string AuthAppPrefix = "AuthenticationMode=APPLICATION_ONLY;ApplicationAuthenticationType=APPNAME_AND_KEY;ApplicationName=";
        private const string AuthUserAppPrefix = "AuthenticationMode=USER_AND_APPLICATION;AuthenticationType=MANUAL;ApplicationAuthenticationType=APPNAME_AND_KEY;ApplicationName=";
        private const string AuthDirPrefix = "AuthenticationType=DIRECTORY_SERVICE;DirSvcPropertyName=";

        private const string DefaultService = "//blp/mktdata";
        private const string DefaultTopic = "/ticker/IBM Equity";

        private const string DefaultHost = "localhost";
        private const int DefaultPort = 8194;

        internal const string DefaultAuthService = "//blp/apiauth";
        internal const string LastPrice = "LAST_PRICE";

        public string Service { get; set; } = AppOptions.DefaultService;
        public string AuthService { get; set; } = AppOptions.DefaultAuthService;
        public string AuthOptions { get; set; } = null;

        public List<string> Topics { get; } = new List<string>();
        public List<string> Fields { get; } = new List<string>();
        public List<string> Options { get; } = new List<string>();

        public List<string> Hosts { get; } = new List<string>();
        public int Port { get; set; } = AppOptions.DefaultPort;

        public static void PrintUsage()
        {
            Console.WriteLine(@"
Retrieve realtime data.
Usage:
    [-ip   <ipAddress>] server name or IP (default: localhost)
    [-p    <tcpPort>]   server port (default: 8194)
    [-s    <service>]   service name (default: //blp/mktdata))
    [-t    <topic>]     topic name (default: /ticker/IBM Equity)
    [-f    <field>]     field to subscribe to (default: LAST_PRICE)
    [-o    <option>]    subscription options (default: empty)
    [-auth <option>]    authentication option (default: none):
        none
        user           as a user using OS logon information
        app=<app>      as the specified application
        userapp=<app>  as user and application using logon information for the user");
        }

        public static AppOptions ParseCommandLine(string[] args)
        {
            var appOptions = new AppOptions();

            if (args != null)
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    string option = args[i];
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Missing argument!");
                    }

                    string optValue = args[++i];
                    if (AppOptions.EqualIgnoreCase(option, "-ip"))
                    {
                        appOptions.Hosts.Add(optValue);
                    }
                    else if (AppOptions.EqualIgnoreCase(option, "-p"))
                    {
                        appOptions.Port = int.Parse(optValue);
                    }
                    else if (AppOptions.EqualIgnoreCase(option, "-s"))
                    {
                        appOptions.Service = optValue;
                    }
                    else if (AppOptions.EqualIgnoreCase(option, "-t"))
                    {
                        appOptions.Topics.Add(optValue);
                    }
                    else if (AppOptions.EqualIgnoreCase(option, "-f"))
                    {
                        appOptions.Fields.Add(optValue);
                    }
                    else if (AppOptions.EqualIgnoreCase(option, "-o"))
                    {
                        appOptions.Options.Add(optValue);
                    }
                    else if (AppOptions.EqualIgnoreCase(option, "-auth"))
                    {
                        appOptions.AuthOptions = ParseAuthOptions(optValue);
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid argument: '{option}'!");
                    }
                }
            }

            if (appOptions.Hosts.Count == 0)
            {
                appOptions.Hosts.Add(AppOptions.DefaultHost);
            }

            if (appOptions.Topics.Count == 0)
            {
                appOptions.Topics.Add(AppOptions.DefaultTopic);
            }

            if (!appOptions.Fields.Contains(AppOptions.LastPrice))
            {
                // Always add LAST_PRICE, as the application does complex
                // computations on LAST_PRICE after receiving subscription data.
                appOptions.Fields.Add(AppOptions.LastPrice);
            }

            return appOptions;
        }

        private static string ParseAuthOptions(string value)
        {
            if (value.Equals(AppOptions.AuthOptionNone))
            {
                return null;
            }

            if (value.Equals(AppOptions.AuthOptionUser))
            {
                return AppOptions.AuthUser;
            }

            if (value.StartsWith(AppOptions.AuthOptionApp))
            {
                return AppOptions.BuildAuthOption(
                    value,
                    AppOptions.AuthOptionApp,
                    AppOptions.AuthAppPrefix);
            }

            if (value.StartsWith(AppOptions.AuthOptionUserApp))
            {
                return AppOptions.BuildAuthOption(
                    value,
                    AppOptions.AuthOptionUserApp,
                    AppOptions.AuthUserAppPrefix);
            }

            if (value.StartsWith(AppOptions.AuthOptionDir))
            {
                return AppOptions.BuildAuthOption(
                    value,
                    AppOptions.AuthOptionDir,
                    AppOptions.AuthDirPrefix);
            }

            throw new ArgumentException($"Invalid auth option: '{value}'!");
        }

        private static string BuildAuthOption(
            string value,
            string cmdOption,
            string prefix)
        {
            return $"{prefix}{value.Substring(cmdOption.Length)}";
        }

        /// <summary>
        /// Determines whether <paramref name="s1"/> and <paramref name="s2"/>
        /// have the same value using case-insensitive rule.
        /// </summary>
        /// <returns>True if the two strings have the same value.</returns>
        private static bool EqualIgnoreCase(string s1, string s2)
        {
            return s1.Equals(s2, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
