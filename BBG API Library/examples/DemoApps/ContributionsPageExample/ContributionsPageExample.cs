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
    class ContributionsPageExample
    {
        private const string AUTH_OPTION_NONE = "none";
        private const string AUTH_OPTION_USER = "user";
        private const string AUTH_OPTION_APP = "app=";
        private const string AUTH_OPTION_DIR = "dir=";
        private const string Port8194 = "8194";
        private const string Port8196 = "8196";

        private List<string> d_serverHosts;
        private int d_serverPort;
        private string d_serviceName;
        private string d_topic;
        private AuthOptions d_authOptions;
        private int d_maxEvents = 100;
        private int d_contributionId;
        private volatile bool d_running = true;

        private TlsOptions tlsOptions;
        private bool zfpOverLeasedLine;
        private ZfpUtil.Remote remote;

        public ContributionsPageExample()
        {
            d_serviceName = "//blp/mpfbapi";
            d_serverPort = 8194;
            d_serverHosts = new List<string>();
            d_authOptions = new AuthOptions(AuthUser.CreateWithLogonName());
            d_topic = "220/660/1";
            d_contributionId = 8563;
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
                    d_serviceName + "/" + d_topic,
                    new CorrelationID(new MyStream(d_topic)));

                session.CreateTopics(
                    topicList,
                    ResolveMode.AUTO_REGISTER_SERVICES);

                List<MyStream> myStreams = new List<MyStream>();
                for (int i = 0; i < topicList.Size; ++i)
                {
                    if (topicList.StatusAt(i) == TopicList.TopicStatus.CREATED)
                    {
                        Topic topic = session.GetTopic(topicList.MessageAt(i));
                        MyStream stream = (MyStream)topicList.CorrelationIdAt(i).Object;
                        stream.SetTopic(topic);
                        myStreams.Add(stream);
                    }
                }

                PublishEvents(session, myStreams);
            }
        }

        public static void Main(string[] args)
        {
            ContributionsPageExample example = new ContributionsPageExample();
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

        private void PublishEvents(ProviderSession session, List<MyStream> myStreams)
        {
            Service service = session.GetService(d_serviceName);
            if (service == null)
            {
                System.Console.Error.WriteLine("Failed to get Service: " + d_serviceName);
                return;
            }
            Random rnd = new Random(DateTime.Now.Millisecond);

            int iteration = 0;
            while (iteration++ < d_maxEvents)
            {
                if (!d_running)
                {
                    return;
                }
                Event eventObj = service.CreatePublishEvent();
                EventFormatter eventFormatter = new EventFormatter(eventObj);

                foreach (MyStream stream in myStreams)
                {
                    eventFormatter.AppendMessage("PageData", stream.GetTopic());
                    eventFormatter.PushElement("rowUpdate");

                    eventFormatter.AppendElement();
                    eventFormatter.SetElement("rowNum", 1);
                    eventFormatter.PushElement("spanUpdate");

                    eventFormatter.AppendElement();
                    eventFormatter.SetElement("startCol", 20);
                    eventFormatter.SetElement("length", 4);
                    eventFormatter.SetElement("text", "TEST");
                    eventFormatter.SetElement("attr", "INTENSIFY");
                    eventFormatter.PopElement();

                    eventFormatter.AppendElement();
                    eventFormatter.SetElement("startCol", 25);
                    eventFormatter.SetElement("length", 4);
                    eventFormatter.SetElement("text", "PAGE");
                    eventFormatter.SetElement("attr", "BLINK");
                    eventFormatter.PopElement();

                    eventFormatter.AppendElement();
                    eventFormatter.SetElement("startCol", 30);
                    string timestamp = System.DateTime.Now.ToString();
                    eventFormatter.SetElement("length", timestamp.Length);
                    eventFormatter.SetElement("text", timestamp);
                    eventFormatter.SetElement("attr", "REVERSE");
                    eventFormatter.PopElement();

                    eventFormatter.PopElement();
                    eventFormatter.PopElement();

                    eventFormatter.AppendElement();
                    eventFormatter.SetElement("rowNum", 2);
                    eventFormatter.PushElement("spanUpdate");
                    eventFormatter.AppendElement();
                    eventFormatter.SetElement("startCol", 20);
                    eventFormatter.SetElement("length", 9);
                    eventFormatter.SetElement("text", "---------");
                    eventFormatter.SetElement("attr", "UNDERLINE");
                    eventFormatter.PopElement();
                    eventFormatter.PopElement();
                    eventFormatter.PopElement();

                    eventFormatter.AppendElement();
                    eventFormatter.SetElement("rowNum", 3);
                    eventFormatter.PushElement("spanUpdate");
                    eventFormatter.AppendElement();
                    eventFormatter.SetElement("startCol", 10);
                    eventFormatter.SetElement("length", 9);
                    eventFormatter.SetElement("text", "TEST LINE");
                    eventFormatter.PopElement();
                    eventFormatter.AppendElement();
                    eventFormatter.SetElement("startCol", 23);
                    eventFormatter.SetElement("length", 5);
                    eventFormatter.SetElement("text", "THREE");
                    eventFormatter.PopElement();
                    eventFormatter.PopElement();
                    eventFormatter.PopElement();
                    eventFormatter.PopElement();

                    eventFormatter.SetElement("contributorId", d_contributionId);
                    eventFormatter.SetElement("productCode", 1);
                    eventFormatter.SetElement("pageNumber", 1);
                }

                foreach (Message msg in eventObj)
                {
                    Console.WriteLine(msg);
                }

                session.Publish(eventObj);

                int sleepSecs = rnd.Next(20);
                Thread.Sleep(sleepSecs * 1000);
            }

        }

        private class MyStream
        {
            private string d_id;
            private Topic d_topic;

            public Topic GetTopic()
            {
                return d_topic;
            }

            public void SetTopic(Topic topic)
            {
                d_topic = topic;
            }

            public string SetId()
            {
                return d_id;
            }

            public MyStream(string id)
            {
                d_id = id;
                d_topic = null;
            }
        }

        private void PrintUsage()
        {
            Console.WriteLine(@"
Usage:
Contribute page data to a topic
    [-ip   <ipAddress = localhost>
    [-p    <tcpPort = 8194>
    [-s    <service = //blp/mpfbapi>]
    [-t    <topic = 220/660/1>]
    [-me   <maxEvents = 100>]
    [-c    <contributorId = 8563>]
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
                else if (string.Compare("-t", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_topic = args[++i];
                }
                else if (string.Compare("-me", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_maxEvents = int.Parse(args[++i]);
                }
                else if (string.Compare("-c", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_contributionId = int.Parse(args[++i]);
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
                else if (string.Compare("-zfp-over-leased-line", args[i], true) == 0 && i + 1 < args.Length) {
                    this.zfpOverLeasedLine = true;
                    if (!TryGetRemote(args[++i], out remote)) {
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
            switch (input) {
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
