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
    public class LocalPageSubscriptionExample
    {
        private const string AUTH_OPTION_NONE = "none";
        private const string AUTH_OPTION_USER = "user";
        private const string AUTH_OPTION_APP = "app=";
        private const string AUTH_OPTION_DIR = "dir=";

        private int serverPort = 8194;
        private List<string> serverHosts = new List<string>();
        private string serviceName = "//viper/page";
        private string pageName = "330/1/1";
        private AuthOptions authOptions = new AuthOptions(AuthUser.CreateWithLogonName());

        public void Run(string[] args)
        {
            Session session = null;
            if (!ParseCommandLine(args)) return;
            try
            {
                session = CreateSession();

                if (!session.Start())
                {
                    System.Console.Error.WriteLine("Failed to start session.");
                    return;
                }

                if (!session.OpenService(serviceName))
                {
                    System.Console.Error.WriteLine("Failed to open service :" + serviceName);
                    return;
                }

                // Send subscription and handle subscription Reponse
                SendProcessPageSubscription(session);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send Subscription due to error :" + ex.Message);
            }
            finally
            {
                session.Stop();
            }

        }

        public static void Main(string[] args)
        {
            System.Console.WriteLine("LocalPageSubscriptionExample");
            var example = new LocalPageSubscriptionExample();
            example.Run(args);
            Console.WriteLine("Press ENTER to quit");
            System.Console.ReadLine();
        }

        #region private helper Method

        private Session CreateSession()
        {
            SessionOptions sessionOptions = new SessionOptions();
            SessionOptions.ServerAddress[] servers = new SessionOptions.ServerAddress[serverHosts.Count];
            for (int i = 0; i < serverHosts.Count; ++i)
            {
                servers[i] = new SessionOptions.ServerAddress(serverHosts[i], serverPort);
            }
            sessionOptions.ServerAddresses = servers;
            sessionOptions.AutoRestartOnDisconnection = true;
            sessionOptions.NumStartAttempts = serverHosts.Count;
            sessionOptions.SetSessionIdentityOptions(authOptions);

            System.Console.WriteLine("Connecting to port " + serverPort + " on ");
            foreach (string host in serverHosts)
            {
                System.Console.WriteLine(host + " ");
            }
            Session session = new Session(sessionOptions);
            return session;
        }

        private void SendProcessPageSubscription(Session session)
        {
            string topicName = serviceName + "/" + pageName;

            List<Subscription> subscriptionList = new List<Subscription>();
            subscriptionList.Add(new Subscription(topicName, new CorrelationID(topicName)));

            System.Console.WriteLine("Subscribing...");
            session.Subscribe(subscriptionList);

            ProcessSubscriptionResponse(session);
        }

        private static void ProcessSubscriptionResponse(Session session)
        {
            while (true)
            {
                Event eventObj = session.NextEvent();
                System.Console.WriteLine("Got Event " + eventObj.Type);

                if (eventObj.Type == Event.EventType.SUBSCRIPTION_DATA || eventObj.Type == Event.EventType.SUBSCRIPTION_STATUS)
                {
                    foreach (Message msg in eventObj)
                    {
                        if (msg != null)
                        {

                            string topic = msg.CorrelationID.ToString();
                            System.Console.WriteLine(topic + " - ");
                            msg.Print(System.Console.Out);
                        }
                    }
                }
            }
        }

        private void PrintUsage()
        {
            Console.WriteLine($@"
Usage:
Local Page Subscription
    [-ip   <ipAddress = localhost>]
    [-p    <tcpPort   = {serverPort}>]
    [-s    <service   = {serviceName}>]
    [-P    <Page      = {pageName}>]
    [-auth <user|none|app=<app>|dir=<property>> (default: user)]
        none            applicable to Desktop API product that requires
                        Bloomberg Professional service to be installed locally.
        user            as a user using OS logon information
        dir=<property>  as a user using directory services
        app=<app>       as the specified application");
        }

        private bool ParseCommandLine(string[] args)
        {
            try
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    if (string.Compare("-s", args[i], true) == 0)
                    {
                        serviceName = args[++i];
                    }
                    else if (string.Compare("-ip", args[i], true) == 0)
                    {
                        serverHosts.Add(args[++i]);
                    }
                    else if (string.Compare("-p", args[i], true) == 0)
                    {
                        serverPort = int.Parse(args[++i]);
                    }
                    else if (string.Compare("-page", args[i], true) == 0)
                    {
                        pageName = args[++i];
                    }
                    else if (string.Compare("-auth", args[i], true) == 0
                        && i + 1 < args.Length)
                    {
                        ++i;
                        if (string.Compare(AUTH_OPTION_NONE, args[i], true) == 0)
                        {
                            authOptions = new AuthOptions();
                        }
                        else if (string.Compare(AUTH_OPTION_USER, args[i], true)
                                                                        == 0)
                        {
                            authOptions = new AuthOptions(AuthUser.CreateWithLogonName());
                        }
                        else if (string.Compare(AUTH_OPTION_APP, 0, args[i], 0,
                                            AUTH_OPTION_APP.Length, true) == 0)
                        {
                            string appName = args[i].Substring(AUTH_OPTION_APP.Length);
                            authOptions = new AuthOptions(new AuthApplication(appName));
                        }
                        else if (string.Compare(AUTH_OPTION_DIR, 0, args[i], 0,
                                            AUTH_OPTION_DIR.Length, true) == 0)
                        {
                            string dir = args[i].Substring(AUTH_OPTION_DIR.Length);
                            authOptions = new AuthOptions(AuthUser.CreateWithActiveDirectoryProperty(dir));
                        }
                        else
                        {
                            PrintUsage();
                            return false;
                        }
                    }
                    else if (string.Compare("-h", args[i], true) == 0)
                    {
                        PrintUsage();
                        return false;
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
            if (serverHosts.Count == 0)
            {
                serverHosts.Add("localhost");
            }

            return true;
        }

        #endregion
    }
}
