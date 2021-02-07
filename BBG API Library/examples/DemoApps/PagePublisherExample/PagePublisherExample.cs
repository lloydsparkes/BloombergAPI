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
    class PagePublisherExample
    {
        private const string AUTH_OPTION_NONE = "none";
        private const string AUTH_OPTION_USER = "user";
        private const string AUTH_OPTION_APP = "app=";
        private const string AUTH_OPTION_DIR = "dir=";

        private static readonly Name TOPIC = Name.GetName("topic");

        private Dictionary<Topic, Topic> d_topicSet = new Dictionary<Topic, Topic>(); // Hashset
        private string d_service = "//viper/page";
        private int d_verbose = 0;
        private List<string> d_hosts = new List<string>();
        private int d_port = 8194;

        private string d_groupId = null;
        private int d_priority = int.MaxValue;

        private AuthOptions d_authOptions = new AuthOptions(AuthUser.CreateWithLogonName());

        private volatile bool d_running = true;
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
                processEvent))
            {
                if (!session.Start())
                {
                    Console.Error.WriteLine("Failed to start session");
                    return;
                }

                ServiceRegistrationOptions serviceRegistrationOptions =
                    new ServiceRegistrationOptions();
                serviceRegistrationOptions.GroupId = d_groupId;
                serviceRegistrationOptions.ServicePriority = d_priority;

                if (!session.RegisterService(
                    d_service,
                    session.GetAuthorizedIdentity(),
                    serviceRegistrationOptions))
                {
                    Console.WriteLine("Failed to register " + d_service);
                    return;
                }
                Console.WriteLine("Service registered " + d_service);

                //Publishing events for the active topics of the designated service.
                PublishEvents(session);
            }
        }

        #region Event Processing

        // Handling subscription start and stop events, which add and remove topics to the active publication set.
        private void processEvent(Event eventObj, ProviderSession session)
        {
            switch (eventObj.Type)
            {
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
                case Event.EventType.TOPIC_STATUS:
                    ProcessTopicStatusMsg(session, eventObj);
                    break;
                case Event.EventType.REQUEST:
                    Service service = session.GetService(d_service);
                    foreach (Message msg in eventObj)
                    {
                        if (msg.MessageType.Equals(Names.PermissionRequest))
                        {
                            // This example always sends a 'PERMISSIONED' response.
                            // See 'MktdataPublisherExample' on how to parse a Permission
                            // request and send an appropriate 'PermissionResponse'.
                            Event response = service.CreateResponseEvent(msg.CorrelationID);
                            EventFormatter ef = new EventFormatter(response);
                            int permission = 0; // ALLOWED: 0, DENIED: 1
                            ef.AppendResponse("PermissionResponse");
                            ef.PushElement("topicPermissions");
                            // For each of the topics in the request, add an entry
                            // to the response
                            Element topicsElement = msg.GetElement(Name.GetName("topics"));
                            for (int i = 0; i < topicsElement.NumValues; ++i)
                            {
                                ef.AppendElement();
                                ef.SetElement("topic", topicsElement.GetValueAsString(i));
                                ef.SetElement("result", permission); // PERMISSIONED
                                ef.PopElement();
                            }
                            ef.PopElement();
                            session.SendResponse(response);
                        }
                    }
                    break;
                default:
                    if (d_verbose > 0)
                    {
                        foreach (Message msg in eventObj)
                        {
                            Console.WriteLine("Message = " + msg);
                        }
                    }
                    break;
            }
        }

        private void ProcessTopicStatusMsg(ProviderSession session, Event eventObj)
        {
            TopicList topicList = new TopicList();

            foreach (Message msg in eventObj)
            {
                Console.WriteLine(msg);

                if (msg.MessageType.Equals(Names.TopicSubscribed))
                {
                    Topic topic = session.GetTopic(msg);
                    if (topic == null)
                    {
                        CorrelationID cid = new CorrelationID(msg.GetElementAsString(TOPIC));
                        topicList.Add(msg, cid);
                    }
                    else
                    {
                        lock (d_topicSet)
                        {
                            if (!d_topicSet.ContainsKey(topic))
                            {
                                d_topicSet[topic] = topic;
                                Monitor.PulseAll(d_topicSet);
                            }
                        }
                    }
                }
                else if (msg.MessageType.Equals(Names.TopicUnsubscribed))
                {
                    Topic topic = session.GetTopic(msg);
                    lock (d_topicSet)
                    {
                        d_topicSet.Remove(topic);
                    }
                }
                else if (msg.MessageType.Equals(Names.TopicCreated))
                {
                    Topic topic = session.GetTopic(msg);
                    lock (d_topicSet)
                    {
                        if (!d_topicSet.ContainsKey(topic))
                        {
                            d_topicSet[topic] = topic;
                            Monitor.PulseAll(d_topicSet);
                        }
                    }
                }
                else if (msg.MessageType.Equals(Names.TopicRecap))
                {
                    Topic topic = session.GetTopic(msg);
                    lock (d_topicSet)
                    {
                        if (!d_topicSet.ContainsKey(topic))
                        {
                            continue;
                        }
                    }
                    // send initial paint.this should come from app's cache.
                    Service service = session.GetService(d_service);
                    Event recapEvent = service.CreatePublishEvent();
                    EventFormatter eventFormatter = new EventFormatter(recapEvent);
                    eventFormatter.AppendRecapMessage(topic, msg.CorrelationID);
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

            if (topicList.Size > 0)
            {
                session.CreateTopicsAsync(topicList);
            }
        }

        #endregion

        private void PublishEvents(ProviderSession session)
        {
            Service service = session.GetService(d_service);
            while (d_running)
            {
                Event eventObj = null;
                lock (d_topicSet)
                {
                    if (d_topicSet.Count == 0)
                    {
                        Monitor.Wait(d_topicSet, 100);
                    }
                    if (d_topicSet.Count == 0)
                    {
                        continue;
                    }

                    Console.WriteLine("Publishing");
                    eventObj = service.CreatePublishEvent();
                    EventFormatter eventFormatter = new EventFormatter(eventObj);

                    foreach (Topic topic in d_topicSet.Keys)
                    {
                        string os = DateTime.Now.ToLocalTime().ToString();

                        int numRows = 5;
                        for (int i = 1; i <= numRows; ++i)
                        {
                            eventFormatter.AppendMessage("RowUpdate", topic);
                            eventFormatter.SetElement("rowNum", i);
                            eventFormatter.PushElement("spanUpdate");

                            eventFormatter.AppendElement();
                            eventFormatter.SetElement("startCol", 1);
                            eventFormatter.SetElement("length", os.Length);
                            eventFormatter.SetElement("text", os);
                            eventFormatter.PopElement();

                            eventFormatter.PopElement();
                        }
                    }
                }
                if (d_verbose > 1 && eventObj != null)
                {
                    foreach (Message msg in eventObj)
                    {
                        Console.WriteLine(msg);
                    }
                }
                session.Publish(eventObj);
                Thread.Sleep(10 * 1000);
            }
        }

        private void PrintUsage()
        {
            Console.WriteLine(@"
Publish page data.
Usage:
    [-ip   <ipAddress>] server name or IP (default: localhost)
    [-p    <tcpPort>]   server port (default: 8194)
    [-s    <service>]   service name (default: //viper/page)
    [-g    <groupId>]   publisher groupId (defaults to a unique value)
    [-pri  <priority>]  publisher priority (default: int.MaxValue)
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

        static void Main(string[] args)
        {
            Console.WriteLine("PagePublisherExample");
            PagePublisherExample example = new PagePublisherExample();
            example.Run(args);

            Console.WriteLine("Press <ENTER> to terminate.");
            Console.ReadLine();
        }
    }
}
