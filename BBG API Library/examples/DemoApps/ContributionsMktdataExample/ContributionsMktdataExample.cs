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
using System.Threading;
using Bloomberglp.Blpapi;

namespace Bloomberglp.BlpapiExamples.DemoApps
{
    class ContributionsMktdataExample
    {
        private const string AUTH_OPTION_NONE = "none";
        private const string AUTH_OPTION_USER = "user";
        private const string AUTH_OPTION_APP = "app=";
        private const string AUTH_OPTION_DIR = "dir=";
        private const string Port8194 = "8194";
        private const string Port8196 = "8196";

        private static readonly Name BID = Name.GetName("BID");
        private static readonly Name ASK = Name.GetName("ASK");
        private static readonly Name BID_SIZE = Name.GetName("BID_SIZE");
        private static readonly Name ASK_SIZE = Name.GetName("ASK_SIZE");

        private List<string> d_serverHosts;
        private int d_serverPort;
        private string d_serviceName;
        private string d_topic;
        private int d_maxEvents = 100;
        private AuthOptions d_authOptions;
        private volatile bool d_running = true;

        private TlsOptions tlsOptions;
        private bool zfpOverLeasedLine;
        private ZfpUtil.Remote remote;

        public ContributionsMktdataExample()
        {
            d_serviceName = "//blp/mpfbapi";
            d_topic = "/ticker/AUDEUR Curncy";
            d_serverPort = 8194;
            d_serverHosts = new List<string>();
            d_authOptions = new AuthOptions(AuthUser.CreateWithLogonName());
        }

        public void Run(string[] args)
        {
            if (!ParseCommandLine(args))
            {
                return;
            }

            SessionOptions.ServerAddress[] servers = new SessionOptions.ServerAddress[d_serverHosts.Count];
            for (int i = 0; i < d_serverHosts.Count; ++i)
            {
                servers[i] = new SessionOptions.ServerAddress(d_serverHosts[i], d_serverPort);
            }

            var sessionOptions =
                zfpOverLeasedLine ?
                    PrepareZfpSessionOptions() :
                    PrepareStandardSessionOptions();

            sessionOptions.AutoRestartOnDisconnection = true;
            sessionOptions.SetSessionIdentityOptions(d_authOptions);

            System.Console.Write("Connecting to");
            foreach (SessionOptions.ServerAddress server in sessionOptions.ServerAddresses)
            {
                System.Console.Write(" " + server);
            }
            System.Console.WriteLine();

            using (ProviderSession session = new ProviderSession(sessionOptions, ProcessEvent))
            {
                if (!session.Start())
                {
                    Console.Error.WriteLine("Failed to start session");
                    return;
                }

                TopicList topicList = new TopicList();
                topicList.Add(
                    d_serviceName + d_topic,
                    new CorrelationID(new MyStream(d_topic)));

                session.CreateTopics(
                    topicList,
                    ResolveMode.AUTO_REGISTER_SERVICES);

                Service service = session.GetService(d_serviceName);
                if (service == null)
                {
                    System.Console.Error.WriteLine("Open service failed: " + d_serviceName);
                    return;
                }

                List<MyStream> myStreams = new List<MyStream>();
                for (int i = 0; i < topicList.Size; ++ i) {
                    if (topicList.StatusAt(i) == TopicList.TopicStatus.CREATED)
                    {
                        Topic topic = session.GetTopic(topicList.MessageAt(i));
                        MyStream stream = (MyStream)topicList.CorrelationIdAt(i).Object;
                        stream.SetTopic(topic);
                        myStreams.Add(stream);
                    }
                }

                int iteration = 0;
                while (iteration++ < d_maxEvents)
                {
                    if (!d_running)
                    {
                        break;
                    }
                    Event eventObj = service.CreatePublishEvent();
                    EventFormatter eventFormatter = new EventFormatter(eventObj);

                    foreach (MyStream stream in myStreams)
                    {
                        eventFormatter.AppendMessage("MarketData", stream.GetTopic());
                        eventFormatter.SetElement(BID, stream.GetBid());
                        eventFormatter.SetElement(ASK, stream.GetAsk());
                        eventFormatter.SetElement(BID_SIZE, 1200);
                        eventFormatter.SetElement(ASK_SIZE, 1400);
                    }

                    System.Console.WriteLine(System.DateTime.Now + " -");

                    foreach (Message msg in eventObj)
                    {
                        System.Console.WriteLine(msg);
                    }

                    session.Publish(eventObj);
                    Thread.Sleep(10 * 1000);
                }
            }
        }

        public static void Main(string[] args)
        {
            ContributionsMktdataExample example = new ContributionsMktdataExample();
            example.Run(args);
        }

        private void ProcessEvent(Event eventObj, ProviderSession session)
        {
            if (eventObj == null)
            {
                Console.WriteLine("Received null event ");
                return;
            }
            Console.WriteLine("Received event " + eventObj.Type);
            foreach (Message msg in eventObj)
            {
                Console.WriteLine("Message = " + msg);
                if (eventObj.Type == Event.EventType.SESSION_STATUS)
                {
                    if (msg.MessageType.Equals(Names.SessionTerminated))
                    {
                        d_running = false;
                    }
                }
            }

            //TO DO Process event if needed.
        }

        #region private helper method

        private class MyStream
        {
            private string d_id;
            private Topic d_topic;
            private static Random d_market = new Random(System.DateTime.Now.Millisecond);
            private double d_lastValue;

            public Topic GetTopic()
            {
                return d_topic;
            }

            public void SetTopic(Topic topic)
            {
                d_topic = topic;
            }

            public string GetId()
            {
                return d_id;
            }

            public MyStream(string id)
            {
                d_id = id;
                d_topic = null;
                d_lastValue = d_market.NextDouble() * 100;
            }

            public void Next()
            {
                double delta = d_market.NextDouble();
                if (d_lastValue + delta < 1.0)
                    delta = d_market.NextDouble();
                d_lastValue += delta;
            }

            public double GetAsk()
            {
                return Math.Round(d_lastValue * 101) / 100.0;
            }

            public double GetBid()
            {
                return Math.Round(d_lastValue * 98) / 100.0;
            }
        }

        private void PrintUsage()
        {
            Console.WriteLine($@"
Usage:
Contribute market data to a topic
    [-ip   <ipAddress = localhost>]
    [-p    <tcpPort = 8194>]
    [-s    <service = //blp/mpfbapi>]
    [-t    <topic = /ticker/AUDEUR Curncy>]
    [-me   <maxEvents = {this.d_maxEvents}>]     max number of events
    [-auth <option = user> (user|none|app=<app>|dir=<property>)]
        none            applicable to Desktop API product that requires
                        Bloomberg Professional service to be installed locally.
        user            as a user using OS logon information
        dir=<property>  as a user using directory services
        app=<app>       as the specified application

    TLS OPTIONS (specify all or none):
        [-tls-client-credentials <file>]          name a PKCS#12 file to use as a source of client credentials
        [-tls-client-credentials-password <pwd>]  specify password for accessing client credentials
        [-tls-trust-material <file>]              name a PKCS#7 file to use as a source of trusted certificates

    ZFP connections over leased lines (requires TLS options):
         [-zfp-over-leased-line <port>]  enable ZFP connections over leased lines on the specified port (8194 or 8196)
            (When this option is enabled, '-ip' and '-p' arguments will be ignored.)
Press ENTER to quit");
            Console.ReadLine();
        }

        private SessionOptions PrepareStandardSessionOptions()
        {
            var sessionOptions = new SessionOptions();
            SessionOptions.ServerAddress[] servers = new SessionOptions.ServerAddress[d_serverHosts.Count];
            for (int i = 0; i < d_serverHosts.Count; ++i) {
                servers[i] = new SessionOptions.ServerAddress(d_serverHosts[i], d_serverPort);
            }

            sessionOptions.ServerAddresses = servers;
            sessionOptions.NumStartAttempts = d_serverHosts.Count;

            Console.WriteLine("Connecting to port " + d_serverPort + " on ");
            foreach (string host in d_serverHosts) {
                Console.WriteLine(host + " ");
            }

            return sessionOptions;
        }

        private SessionOptions PrepareZfpSessionOptions()
        {
            Console.WriteLine("Creating a ZFP connection for leased lines.");

            var sessionOptions = ZfpUtil.GetZfpOptionsForLeasedLines(
                this.remote,
                this.tlsOptions);

            return sessionOptions;
        }

        private bool ParseCommandLine(string[] args)
        {
            string clientCredentials = null;
            string clientCredentialsPassword = null;
            string trustMaterial = null;

            for (int i = 0; i < args.Length; ++i)
            {
                if (string.Compare("-s", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_serviceName = args[++i];
                }
                else if (string.Compare("-t", args[i], true) == 0
                         && i + 1 < args.Length)
                {
                    this.d_topic = args[++i];
                }
                else if (string.Compare("-ip", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_serverHosts.Add(args[++i]);
                }
                else if (string.Compare("-p", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_serverPort = int.Parse(args[++i]);
                }
                else if (string.Compare("-me", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_maxEvents = int.Parse(args[++i]);
                }
                else if (string.Compare("-auth", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    ++i;
                    if (string.Compare(AUTH_OPTION_NONE, args[i], true) == 0)
                    {
                        d_authOptions = new AuthOptions();
                    }
                    else if (string.Compare(AUTH_OPTION_USER, args[i], true)
                                                                        == 0)
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
                        d_authOptions = new AuthOptions(
                            AuthUser.CreateWithActiveDirectoryProperty(dir));
                    }
                    else
                    {
                        PrintUsage();
                        return false;
                    }
                }
                else if (string.Compare("-tls-client-credentials", args[i], true) == 0 && i + 1 < args.Length) {
                    clientCredentials = args[++i];
                }
                else if (string.Compare("-tls-client-credentials-password", args[i], true) == 0 && i + 1 < args.Length) {
                    clientCredentialsPassword = args[++i];
                }
                else if (string.Compare("-tls-trust-material", args[i], true) == 0 && i + 1 < args.Length) {
                    trustMaterial = args[++i];
                }
                else if (string.Compare("-zfp-over-leased-line", args[i], true) == 0 && i + 1 < args.Length)
                {
                    this.zfpOverLeasedLine = true;
                    if (!TryGetRemote(args[++i], out remote))
                    {
                        PrintUsage();
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine($"Unknown argument '{args[i]}'");
                    PrintUsage();
                    return false;
                }
            }
            if (d_serverHosts.Count == 0)
            {
                d_serverHosts.Add("localhost");
            }

            if (clientCredentials != null &&
                trustMaterial != null &&
                clientCredentialsPassword != null) {
                using (var password = new System.Security.SecureString()) {
                    foreach (var c in clientCredentialsPassword) {
                        password.AppendChar(c);
                    }

                    tlsOptions = TlsOptions.CreateFromFiles(clientCredentials, password, trustMaterial);
                }
            }
            else if (zfpOverLeasedLine) {
                Console.WriteLine("TLS parameters are required for ZFP connections over a leased line.");
                return false;
            }

            return true;
        }

        private static bool TryGetRemote(
            string input,
            out ZfpUtil.Remote remote)
        {
            switch (input)
            {
                case Port8194:
                    remote = ZfpUtil.Remote.Remote_8194;
                    return true;
                case Port8196:
                    remote = ZfpUtil.Remote.Remote_8196;
                    return true;
                default:
                    Console.WriteLine($"Invalid ZFP port '{input}'");
                    remote = default(ZfpUtil.Remote);
                    return false;
            }
        }

        #endregion

    }

}
