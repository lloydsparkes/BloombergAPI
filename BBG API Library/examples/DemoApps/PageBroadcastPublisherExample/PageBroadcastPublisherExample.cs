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
    class PageBroadcastPublisherExample
    {
        private const string AUTH_OPTION_NONE = "none";
        private const string AUTH_OPTION_USER = "user";
        private const string AUTH_OPTION_APP = "app=";
        private const string AUTH_OPTION_DIR = "dir=";

        private string d_service = "//viper/page";
        private int d_verbose = 0;
        private List<string> d_hosts = new List<string>();
        private int d_port = 8194;

        private volatile bool d_running = true;

        private AuthOptions d_authOptions = new AuthOptions(AuthUser.CreateWithLogonName());

        private class MyStream
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
        };

        private void ProcessTopicStatusEvent(Event eventObj, ProviderSession session)
        {
            foreach (Message msg in eventObj)
            {
                Console.WriteLine(msg);
                if (msg.MessageType.Equals(Names.TopicRecap) || msg.MessageType.Equals(Names.TopicSubscribed))
                {
                    Topic topic = session.GetTopic(msg);
                    if (topic != null)
                    {
                        // send initial paint, this should come from my own cache
                        Service service = session.GetService(d_service);
                        if (service == null)
                        {
                            Console.Error.WriteLine("service unavailable");
                            return;
                        }
                        Event recapEvent = service.CreatePublishEvent();
                        EventFormatter eventFormatter = new EventFormatter(recapEvent);
                        CorrelationID recapCid = msg.MessageType.Equals(Names.TopicRecap) ?
                            msg.CorrelationID :    //solicited recap
                            null;                //unsolicited recap
                        eventFormatter.AppendRecapMessage(
                            topic,
                            recapCid);
                        eventFormatter.SetElement("numRows", 25);
                        eventFormatter.SetElement("numCols", 80);
                        eventFormatter.PushElement("rowUpdate");
                        for (int i = 1; i <= 5; ++i)
                        {
                            eventFormatter.AppendElement();
                            eventFormatter.SetElement("rowNum", i);
                            eventFormatter.PushElement("spanUpdate");
                            eventFormatter.AppendElement();
                            eventFormatter.SetElement("startCol", 1);
                            eventFormatter.SetElement("length", 10);
                            eventFormatter.SetElement("text", "RECAP");
                            eventFormatter.PopElement();
                            eventFormatter.PopElement();
                            eventFormatter.PopElement();
                        }
                        eventFormatter.PopElement();
                        session.Publish(recapEvent);
                    }
                }
            }
        }

        private void ProcessRequestEvent(Event eventObj, ProviderSession session)
        {
            foreach (Message msg in eventObj)
            {
                Console.WriteLine(msg);
                if (msg.MessageType.Equals(Names.PermissionRequest))
                {
                    Service pubService = session.GetService(d_service);
                    if (pubService == null)
                    {
                        Console.Error.WriteLine("service unavailable");
                        return;
                    }
                    Event response = pubService.CreateResponseEvent(msg.CorrelationID);
                    EventFormatter ef = new EventFormatter(response);
                    ef.AppendResponse("PermissionResponse");
                    ef.PushElement("topicPermissions"); // TopicPermissions

                    Element topicElement = msg.GetElement(Name.GetName("topics"));
                    for (int i = 0; i < topicElement.NumValues; ++i)
                    {
                        ef.AppendElement();
                        ef.SetElement("topic", topicElement.GetValueAsString(i));
                        ef.SetElement("result", 0); // ALLOWED: 0, DENIED: 1
                        ef.PopElement();
                    }

                    session.SendResponse(response);
                }
            }
        }

        private void ProcessEvent(Event eventObj, ProviderSession session)
        {
            switch (eventObj.Type)
            {
                case Event.EventType.TOPIC_STATUS:
                    ProcessTopicStatusEvent(eventObj, session);
                    break;
                case Event.EventType.REQUEST:
                    ProcessRequestEvent(eventObj, session);
                    break;
                case Event.EventType.SESSION_STATUS:
                    foreach (Message msg in eventObj)
                    {
                        Console.WriteLine(msg);
                        if (msg.MessageType.Equals(Names.SessionTerminated))
                        {
                            d_running = false;
                        }
                    }
                    break;
                default:
                    PrintMessage(eventObj);
                    break;
            }
        }

        private void PrintUsage()
        {
            Console.WriteLine(@"
Broadcast page data.
Usage:
    [-ip   <ipAddress>] server name or IP (default: localhost)
    [-p    <tcpPort>]   server port (default: 8194)
    [-s    <service>]   service name (default: //viper/page)
    [-v]                increase verbosity (can be specified more than once)
    [-auth <option>]    authentication option: user|none|app=<app>|userapp=<app>|dir=<property> (default: user)
        none            applicable to Desktop API product that requires
                        Bloomberg Professional service to be installed locally.
        user            as a user using OS logon information
        dir=<property>  as a user using directory services
        app=<app>       as the specified application
        userapp=<app>   as user and application using logon information for the user");
        }

        private bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                if (string.Compare("-ip", args[i], true) == 0
                        && i + 1 < args.Length)
                {
                    d_hosts.Add(args[++i]);
                }
                else if (string.Compare("-p", args[i], true) == 0
                        && i + 1 < args.Length)
                {
                    d_port = int.Parse(args[++i]);
                }
                else if (string.Compare("-s", args[i], true) == 0
                        && i + 1 < args.Length)
                {
                    d_service = args[++i];
                }
                else if (string.Compare("-v", args[i], true) == 0)
                {
                    ++d_verbose;
                }
                else if (string.Compare("-auth", args[i], true) == 0
                        && i + 1 < args.Length)
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
                        d_authOptions = new AuthOptions(
                            AuthUser.CreateWithActiveDirectoryProperty(dir));
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

            if (d_hosts.Count == 0)
            {
                d_hosts.Add("localhost");
            }

            return true;
        }

        private void PrintMessage(Event eventObj)
        {
            foreach (Message msg in eventObj)
            {
                Console.WriteLine(msg);
            }
        }

        private void Publish(TopicList topicList, ProviderSession session)
        {
            List<MyStream> myStreams = new List<MyStream>();
            for (int i = 0; i < topicList.Size; ++i)
            {
                if (topicList.StatusAt(i) == TopicList.TopicStatus.CREATED)
                {
                    Message message = topicList.MessageAt(i);
                    Topic topic = session.GetTopic(message);
                    MyStream stream = (MyStream)topicList.CorrelationIdAt(i).Object;
                    stream.setTopic(topic);
                    myStreams.Add(stream);
                    Console.WriteLine("Topic created: " + stream.getId());
                }
            }

            Service pubService = session.GetService(d_service);
            if (pubService == null)
            {
                Console.Error.WriteLine("service unavailable");
                return;
            }

            // Now we will start publishing
            Event eventObj = pubService.CreatePublishEvent();
            EventFormatter eventFormatter = new EventFormatter(eventObj);
            for (int index = 0; index < myStreams.Count; index++)
            {
                MyStream stream = (MyStream)myStreams[index];

                eventFormatter.AppendRecapMessage(stream.getTopic(), null);
                eventFormatter.SetElement("numRows", 25);
                eventFormatter.SetElement("numCols", 80);
                eventFormatter.PushElement("rowUpdate");
                for (int i = 1; i <= 5; ++i)
                {
                    eventFormatter.AppendElement();
                    eventFormatter.SetElement("rowNum", i);
                    eventFormatter.PushElement("spanUpdate");
                    eventFormatter.AppendElement();
                    eventFormatter.SetElement("startCol", 1);
                    eventFormatter.SetElement("length", 10);
                    eventFormatter.SetElement("text", "INITIAL");
                    eventFormatter.SetElement("fgColor", "RED");
                    eventFormatter.PushElement("attr");
                    eventFormatter.AppendValue("UNDERLINE");
                    eventFormatter.AppendValue("BLINK");
                    eventFormatter.PopElement();
                    eventFormatter.PopElement();
                    eventFormatter.PopElement();
                    eventFormatter.PopElement();
                }
                eventFormatter.PopElement();
            }
            if (d_verbose > 0)
            {
                PrintMessage(eventObj);
            }
            session.Publish(eventObj);

            while (d_running)
            {
                eventObj = pubService.CreatePublishEvent();
                eventFormatter = new EventFormatter(eventObj);

                for (int index = 0; index < myStreams.Count; index++)
                {
                    MyStream stream = (MyStream)myStreams[index];
                    eventFormatter.AppendMessage("RowUpdate", stream.getTopic());
                    eventFormatter.SetElement("rowNum", 1);
                    eventFormatter.PushElement("spanUpdate");
                    eventFormatter.AppendElement();
                    eventFormatter.SetElement("startCol", 1);
                    string text = System.DateTime.Now.ToString();
                    eventFormatter.SetElement("length", text.Length);
                    eventFormatter.SetElement("text", text);
                    eventFormatter.PopElement();
                    eventFormatter.AppendElement();
                    eventFormatter.SetElement("startCol", text.Length + 10);
                    text = System.DateTime.Now.ToString();
                    eventFormatter.SetElement("length", text.Length);
                    eventFormatter.SetElement("text", text);
                    eventFormatter.PopElement();
                    eventFormatter.PopElement();
                }

                if (d_verbose > 0)
                {
                    PrintMessage(eventObj);
                }
                session.Publish(eventObj);
                Thread.Sleep(10 * 1000);
            }
        }

        public void Run(string[] args) //throws Exception
        {
            if (!ParseCommandLine(args))
                return;

            SessionOptions.ServerAddress[] servers = new SessionOptions.ServerAddress[d_hosts.Count];
            for (int i = 0; i < d_hosts.Count; ++i)
            {
                servers[i] = new SessionOptions.ServerAddress(d_hosts[i], d_port);
            }

            SessionOptions sessionOptions = new SessionOptions();
            sessionOptions.ServerAddresses = servers;
            sessionOptions.SetSessionIdentityOptions(d_authOptions);
            sessionOptions.AutoRestartOnDisconnection = true;
            sessionOptions.NumStartAttempts = servers.Length;

            Console.Write("Connecting to");
            foreach (SessionOptions.ServerAddress server in sessionOptions.ServerAddresses)
            {
                Console.Write(" " + server);
            }
            Console.WriteLine();

            using (ProviderSession session = new ProviderSession(
                sessionOptions,
                ProcessEvent))
            {
                if (!session.Start())
                {
                    Console.WriteLine("Failed to start session");
                    return;
                }

                TopicList topicList = new TopicList();
                topicList.Add(
                    d_service + "/1245/4/5",
                    new CorrelationID(new MyStream("1245/4/5")));
                topicList.Add(
                    d_service + "/330/1/1",
                    new CorrelationID(new MyStream("330/1/1")));

                session.CreateTopics(
                    topicList,
                    ResolveMode.AUTO_REGISTER_SERVICES);
                // createTopics() is synchronous, topicList will be updated
                // with the results of topic creation (resolution will happen
                // under the covers)

                Publish(topicList, session);
            }
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("PageBroadcastPublisherExample");
            PageBroadcastPublisherExample example = new PageBroadcastPublisherExample();
            example.Run(args);

            Console.WriteLine("Press <ENTER> to terminate.");
            Console.ReadLine();
        }
    }
}
