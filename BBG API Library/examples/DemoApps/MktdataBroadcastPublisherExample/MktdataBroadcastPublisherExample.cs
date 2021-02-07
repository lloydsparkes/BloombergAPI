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
using System.Threading;

namespace Bloomberglp.BlpapiExamples.DemoApps
{
    class MktdataBroadcastPublisherExample
    {
        private const string AUTH_OPTION_NONE = "none";
        private const string AUTH_OPTION_USER = "user";
        private const string AUTH_OPTION_APP = "app=";
        private const string AUTH_OPTION_DIR = "dir=";

        private string d_service = "//viper/mktdata";
        private int d_verbose = 0;
        private List<string> d_hosts = new List<string>();
        private int d_port = 8194;
        private int d_numRetry = 2;
        private int d_maxEvents = 100;

        private AuthOptions d_authOptions = new AuthOptions(AuthUser.CreateWithLogonName());
        private List<string> d_topics = new List<string>();

        private string d_groupId = null;
        private int d_priority = int.MaxValue;
        private volatile bool d_running = true;

        class MyStream
        {
            string d_id;
            Topic d_topic;

            public MyStream()
            {
                d_id = "";
            }

            public MyStream(string id)
            {
                d_id = id;
            }

            public void setTopic(Topic topic)
            {
                d_topic = topic;
            }

            public string getId()
            {
                return d_id;
            }

            public Topic getTopic()
            {
                return d_topic;
            }
        }

        public void Run(string[] args)
        {
            if (!ParseCommandLine(args))
            {
                return;
            }

            SessionOptions.ServerAddress[] servers = new SessionOptions.ServerAddress[d_hosts.Count];
            for (int i = 0; i < d_hosts.Count; ++i)
            {
                servers[i] = new SessionOptions.ServerAddress(d_hosts[i], d_port);
            }

            SessionOptions sessionOptions = new SessionOptions();
            sessionOptions.ServerAddresses = servers;
            sessionOptions.SetSessionIdentityOptions(d_authOptions);
            sessionOptions.AutoRestartOnDisconnection = true;
            sessionOptions.NumStartAttempts = d_numRetry;

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
                    System.Console.Error.WriteLine("Failed to start session");
                    return;
                }

                if (d_groupId != null)
                {
                    // NOTE: will perform explicit service registration here, instead of letting
                    //       createTopics do it, as the latter approach doesn't allow for custom
                    //       ServiceRegistrationOptions
                    ServiceRegistrationOptions serviceRegistrationOptions = new ServiceRegistrationOptions();
                    serviceRegistrationOptions.GroupId = d_groupId;
                    serviceRegistrationOptions.ServicePriority = d_priority;

                    if (!session.RegisterService(
                        d_service,
                        session.GetAuthorizedIdentity(),
                        serviceRegistrationOptions))
                    {
                        System.Console.Write("Failed to register " + d_service);
                        return;
                    }
                }

                TopicList topicList = new TopicList();
                for (int i = 0; i < d_topics.Count; i++)
                {
                    topicList.Add(
                            d_service + "/ticker/" + d_topics[i],
                            new CorrelationID(new MyStream(d_topics[i])));
                }

                session.CreateTopics(
                        topicList,
                        ResolveMode.AUTO_REGISTER_SERVICES);
                // createTopics() is synchronous, topicList will be updated
                // with the results of topic creation (resolution will happen
                // under the covers)

                List<MyStream> myStreams = new List<MyStream>();

                for (int i = 0; i < topicList.Size; ++i)
                {
                    MyStream stream = (MyStream)topicList.CorrelationIdAt(i).Object;
                    if (topicList.StatusAt(i) == TopicList.TopicStatus.CREATED)
                    {
                        Message msg = topicList.MessageAt(i);
                        stream.setTopic(session.GetTopic(msg));
                        myStreams.Add(stream);
                        System.Console.WriteLine("Topic created: " + topicList.TopicStringAt(i));
                    }
                    else
                    {
                        System.Console.WriteLine("Stream '" + stream.getId()
                                + "': topic not resolved, status = " + topicList.StatusAt(i));
                    }
                }
                Service service = session.GetService(d_service);
                if (service == null)
                {
                    System.Console.Error.WriteLine("Service registration failed: " + d_service);
                    return;
                }

                // Now we will start publishing
                Name eventName = Name.GetName("MarketDataEvents");
                Name high = Name.GetName("HIGH");
                Name low = Name.GetName("LOW");
                long tickCount = 1;
                for (int eventCount = 0; eventCount < d_maxEvents; ++eventCount)
                {
                    if (!d_running)
                    {
                        break;
                    }
                    Event eventObj = service.CreatePublishEvent();
                    EventFormatter eventFormatter = new EventFormatter(eventObj);

                    for (int index = 0; index < myStreams.Count; index++)
                    {
                        Topic topic = myStreams[index].getTopic();
                        if (!topic.IsActive())
                        {
                            System.Console.WriteLine("[WARNING] Publishing on an inactive topic.");
                        }
                        eventFormatter.AppendMessage(eventName, topic);
                        if (1 == tickCount)
                        {
                            eventFormatter.SetElement("OPEN", 1.0);
                        }
                        else if (2 == tickCount)
                        {
                            eventFormatter.SetElement("BEST_BID", 3.0);
                        }
                        eventFormatter.SetElement(high, tickCount * 1.0);
                        eventFormatter.SetElement(low, tickCount * 0.5);
                        ++tickCount;
                    }

                    foreach (Message msg in eventObj)
                    {
                        System.Console.WriteLine(msg);
                    }

                    session.Publish(eventObj);
                    Thread.Sleep(2 * 1000);
                }
            }
        }

        public static void Main(string[] args)
        {
            MktdataBroadcastPublisherExample example = new MktdataBroadcastPublisherExample();
            example.Run(args);
        }

        private void ProcessEvent(Event eventObj, ProviderSession session)
        {
            if (d_verbose > 0)
            {
                Console.Out.WriteLine("Received event " + eventObj.Type);
            }
            foreach (Message msg in eventObj)
            {
                Console.Out.WriteLine("Message = " + msg);

                if (eventObj.Type == Event.EventType.SESSION_STATUS)
                {
                    if (msg.MessageType.Equals(Names.SessionTerminated))
                    {
                        d_running = false;
                    }
                }
            }
        }

        private void PrintUsage()
        {
            Console.WriteLine(@"
Usage:
    [-ip   <ipAddress>]  server name or IP (default: localhost)
    [-p    <tcpPort>]    server port (default: 8194)
    [-r    <number>]     number of retrying connection on disconnected (default: number of hosts)
    [-s    <service>]    service name (default: //viper/mktdata)
    [-t    <topic>]      topic to publish (default: ""IBM Equity"")
    [-g    <groupId>]    publisher groupId (defaults to a unique value)
    [-pri  <priority>]   publisher priority (default: int.MaxValue)
    [-me   <maxEvents>]  stop after publishing this many events (default: 100)
    [-v]                 increase verbosity (can be specified more than once)
    [-auth <option>]     authentication option: user|none|app=<app>|dir=<property> (default: user)
        none            applicable to Desktop API product that requires
                        Bloomberg Professional service to be installed locally.
        user            as a user using OS logon information
        dir=<property>  as a user using directory services
        app=<app>       as the specified application");
        }

        private bool ParseCommandLine(string[] args)
        {
            bool numRetryProvidedByUser = false;
            for (int i = 0; i < args.Length; ++i)
            {
                if (string.Compare("-s", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_service = args[++i];
                }
                else if (string.Compare("-t", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_topics.AddRange(args[++i].Split(','));
                }
                else if (string.Compare("-v", args[i], true) == 0)
                {
                    ++d_verbose;
                }
                else if (string.Compare("-ip", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_hosts.Add(args[++i]);
                }
                else if (string.Compare("-p", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_port = int.Parse(args[++i]);
                }
                else if (string.Compare("-r", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_numRetry = int.Parse(args[++i]);
                    numRetryProvidedByUser = true;
                }
                else if (string.Compare("-g", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_groupId = args[++i];
                }
                else if (string.Compare("-pri", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_priority = int.Parse(args[++i]);
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
                else
                {
                    PrintUsage();
                    return false;
                }
            }

            if (d_hosts.Count == 0)
            {
                d_hosts.Add("localhost");
            }
            if (d_topics.Count == 0)
            {
                d_topics.Add("IBM Equity");
            }
            if (!numRetryProvidedByUser)
            {
                d_numRetry = d_hosts.Count;
            }

            return true;
        }
    }
}
