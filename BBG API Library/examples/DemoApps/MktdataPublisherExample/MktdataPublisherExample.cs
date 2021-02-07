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
    public class MktdataPublisherExample
    {
        private const string AUTH_OPTION_NONE = "none";
        private const string AUTH_OPTION_USER = "user";
        private const string AUTH_OPTION_APP = "app=";
        private const string AUTH_OPTION_DIR = "dir=";

        private static readonly Name TOPIC = Name.GetName("topic");

        private string d_service = "//viper/mktdata";
        private int d_verbose = 0;
        private List<string> d_hosts = new List<string>();
        private int d_port = 8194;
        private List<int> d_eids = new List<int>();

        private readonly Dictionary<Topic, Topic> d_topicSet = new Dictionary<Topic, Topic>();
        private readonly Dictionary<string, string> d_subscribedTopics
            = new Dictionary<string, string>();
        private bool? d_registerServiceResponse = null;
        private string d_groupId = null;
        private int d_priority = int.MaxValue;

        private AuthOptions d_authOptions = new AuthOptions(AuthUser.CreateWithLogonName());
        private int d_clearInterval = 0;

        private bool d_useSsc = false;
        private int  d_sscBegin;
        private int  d_sscEnd;
        private int  d_sscPriority;

        private int? d_resolveSubServiceCode = null;
        private volatile bool d_running = true;

        public void ProcessEvent(Event eventObj, ProviderSession session)
        {

            if (d_verbose > 0)
            {
                Console.WriteLine("Received event " + eventObj.Type);
                foreach (Message msg in eventObj)
                {
                    Console.WriteLine("Message = " + msg);
                }
            }

            if (eventObj.Type == Event.EventType.SESSION_STATUS)
            {
                foreach (Message msg in eventObj)
                {
                    if (msg.MessageType.Equals(Names.SessionTerminated))
                    {
                        d_running = false;
                        break;
                    }
                }
            }
            else if (eventObj.Type == Event.EventType.TOPIC_STATUS)
            {
                TopicList topicList = new TopicList();
                foreach (Message msg in eventObj)
                {
                    if (msg.MessageType.Equals(Names.TopicSubscribed))
                    {
                        Topic topic = session.GetTopic(msg);
                        lock (d_topicSet)
                        {
                            string topicStr = msg.GetElementAsString(TOPIC);
                            d_subscribedTopics[topicStr] = topicStr;
                            if (topic == null)
                            {
                                CorrelationID cid
                                    = new CorrelationID(msg.GetElementAsString("topic"));
                                topicList.Add(msg, cid);
                            }
                            else
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
                        lock (d_topicSet)
                        {
                            d_subscribedTopics.Remove(msg.GetElementAsString(TOPIC));
                            Topic topic = session.GetTopic(msg);
                            d_topicSet.Remove(topic);
                        }
                    }
                    else if (msg.MessageType.Equals(Names.TopicCreated))
                    {
                        Topic topic = session.GetTopic(msg);
                        lock (d_topicSet)
                        {
                            if (d_subscribedTopics.ContainsKey(msg.GetElementAsString(TOPIC))
                                && !d_topicSet.ContainsKey(topic))
                            {
                                d_topicSet[topic] = topic;
                                Monitor.PulseAll(d_topicSet);
                            }
                        }
                    }
                    else if (msg.MessageType.Equals(Names.TopicRecap))
                    {
                        // Here we send a recap in response to a Recap Request.
                        Topic topic = session.GetTopic(msg);
                        lock (d_topicSet)
                        {
                            if (!d_topicSet.ContainsKey(topic))
                            {
                                continue;
                            }
                        }
                        Service service = topic.Service;
                        Event recapEvent = service.CreatePublishEvent();
                        EventFormatter eventFormatter = new EventFormatter(recapEvent);
                        eventFormatter.AppendRecapMessage(topic, msg.CorrelationID);
                        eventFormatter.SetElement("OPEN", 100.0);

                        session.Publish(recapEvent);
                        foreach (Message recapMsg in recapEvent)
                        {
                            Console.WriteLine(recapMsg);
                        }
                    }
                }

                // createTopicsAsync will result in RESOLUTION_STATUS, TOPIC_CREATED events.
                if (topicList.Size > 0)
                {
                    session.CreateTopicsAsync(topicList);
                }
            }
            else if (eventObj.Type == Event.EventType.SERVICE_STATUS)
            {
                foreach (Message msg in eventObj)
                {
                    if (msg.MessageType.Equals(Names.ServiceRegistered))
                    {
                        Object registerServiceResponseMonitor = msg.CorrelationID.Object;
                        lock (registerServiceResponseMonitor)
                        {
                            d_registerServiceResponse = true;
                            Monitor.PulseAll(registerServiceResponseMonitor);
                        }
                    }
                    else if (msg.MessageType.Equals(Names.ServiceRegisterFailure))
                    {
                        Object registerServiceResponseMonitor = msg.CorrelationID.Object;
                        lock (registerServiceResponseMonitor)
                        {
                            d_registerServiceResponse = false;
                            Monitor.PulseAll(registerServiceResponseMonitor);
                        }
                    }
                }
            }
            else if (eventObj.Type == Event.EventType.RESOLUTION_STATUS)
            {
                foreach (Message msg in eventObj)
                {
                    if (msg.MessageType.Equals(Names.ResolutionSuccess))
                    {
                        string resolvedTopic
                            = msg.GetElementAsString(Name.GetName("resolvedTopic"));
                        Console.WriteLine("ResolvedTopic: " + resolvedTopic);
                    }
                    else if (msg.MessageType.Equals(Names.ResolutionFailure))
                    {
                        Console.WriteLine(
                                "Topic resolution failed (cid = " +
                                msg.CorrelationID +
                                ")");
                    }
                }
            }
            else if (eventObj.Type == Event.EventType.REQUEST)
            {
                Service service = session.GetService(d_service);
                foreach (Message msg in eventObj)
                {
                    if (msg.MessageType.Equals(Names.PermissionRequest))
                    {
                        // Similar to createPublishEvent. We assume just one
                        // service - d_service. A responseEvent can only be
                        // for single request so we can specify the
                        // correlationId - which establishes context -
                        // when we create the Event.
                        Event response = service.CreateResponseEvent(msg.CorrelationID);
                        EventFormatter ef = new EventFormatter(response);
                        int permission = 1; // ALLOWED: 0, DENIED: 1
                        if (msg.HasElement("uuid"))
                        {
                            int uuid = msg.GetElementAsInt32("uuid");
                            Console.WriteLine("UUID = " + uuid);
                            permission = 0;
                        }
                        if (msg.HasElement("applicationId"))
                        {
                            int applicationId = msg.GetElementAsInt32("applicationId");
                            Console.WriteLine("APPID = " + applicationId);
                            permission = 0;
                        }
                        // In appendResponse the string is the name of the
                        // operation, the correlationId indicates
                        // which request we are responding to.
                        ef.AppendResponse("PermissionResponse");
                        ef.PushElement("topicPermissions");
                        // For each of the topics in the request, add an entry
                        // to the response
                        Element topicsElement = msg.GetElement(Name.GetName("topics"));
                        for (int i = 0; i < topicsElement.NumValues; ++i)
                        {
                            ef.AppendElement();
                            ef.SetElement("topic", topicsElement.GetValueAsString(i));

                            ef.SetElement("result", permission); // ALLOWED: 0, DENIED: 1

                            if (permission == 1)
                            {
                                // DENIED
                                ef.PushElement("reason");
                                ef.SetElement("source", "My Publisher Name");
                                ef.SetElement("category", "NOT_AUTHORIZED");
                                // or BAD_TOPIC, or custom

                                ef.SetElement("subcategory", "Publisher Controlled");
                                ef.SetElement(
                                    "description",
                                    "Permission denied by My Publisher Name");
                                ef.PopElement();
                            }
                            else
                            {
                                // ALLOWED
                                if (d_resolveSubServiceCode != null)
                                {
                                    ef.SetElement("subServiceCode",
                                                  d_resolveSubServiceCode.Value);
                                    Console.WriteLine(
                                        string.Format(
                                            "Mapping topic {0} to "
                                                + "subserviceCode {1}",
                                            topicsElement.GetValueAsString(i),
                                            d_resolveSubServiceCode));
                                }
                                if (d_eids.Count != 0)
                                {
                                    ef.PushElement("permissions");
                                    ef.AppendElement();
                                    ef.SetElement("permissionService", "//blp/blpperm");
                                    ef.PushElement("eids");
                                    for (int j = 0; j < d_eids.Count; ++j)
                                    {
                                        ef.AppendValue(d_eids[j]);
                                    }
                                    ef.PopElement();
                                    ef.PopElement();
                                    ef.PopElement();
                                }
                            }
                            ef.PopElement();
                        }
                        ef.PopElement();
                        // Service is implicit in the Event. sendResponse has a
                        // second parameter - partialResponse -
                        // that defaults to false.
                        session.SendResponse(response);
                    }
                    else
                    {
                        Console.WriteLine("Received unknown request: " + msg);
                    }
                }
            }
            else
            {
                foreach (Message msg in eventObj)
                {
                    Console.Out.WriteLine("Message = " + msg);
                }
            }
        }

        private void PrintUsage()
        {
            Console.WriteLine(@"
Publish market data.
Usage:
    [-ip   <ipAddress>]    server name or IP (default: localhost)
    [-p    <tcpPort>]      server port (default: 8194)
    [-s    <service>]      service name (default: //viper/mktdata)
    [-g    <groupId>]      publisher groupId (defaults to a unique value)
    [-pri  <priority>]     publisher priority (default: int.MaxValue)
    [-v]                   increase verbosity (can be specified more than once)
    [-c    <event count>]  number of events after which cache will be cleared (default: 0 i.e cache never cleared)
    [-ssc  <ssc range>]    active sub-service code option: <begin>,<end>,<priority>
    [-rssc <ssc>]          sub-service code to be used in resolve
    [-auth <option>]       authentication option: user|none|app=<app>|userapp=<app>|dir=<property> (default: user)
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
                else if (string.Compare("-e", args[i], true) == 0
                        && i + 1 < args.Length)
                {
                    d_eids.Add(int.Parse(args[++i]));
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
                else if (string.Compare("-c", args[i], true) == 0
                        && i + 1 < args.Length)
                {
                    d_clearInterval = int.Parse(args[++i]);
                }
                else if (string.Compare("-ssc", args[i], true) == 0
                        && i + 1 < args.Length)
                {
                    string[] splitRange = args[++i].Split(',');
                    if (splitRange.Length != 3)
                    {
                        PrintUsage();
                        return false;
                    }
                    d_useSsc = true;
                    d_sscBegin = int.Parse(splitRange[0]);
                    d_sscEnd = int.Parse(splitRange[1]);
                    d_sscPriority = int.Parse(splitRange[2]);
                }
                else if (string.Compare("-rssc", args[i], true) == 0
                        && i + 1 < args.Length)
                {
                    d_resolveSubServiceCode = int.Parse(args[++i]);
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

        private void Activate(ProviderSession session) {
            if (d_useSsc) {
                Console.WriteLine(
                    string.Format(
                        "Activating sub service code range [{0}, {1}] "
                            + "@ priority: {2}",
                        d_sscBegin,
                        d_sscEnd,
                        d_sscPriority));
                session.ActivateSubServiceCodeRange(d_service,
                                                    d_sscBegin,
                                                    d_sscEnd,
                                                    d_sscPriority);
            }
        }

        private void Deactivate(ProviderSession session) {
            if (d_useSsc) {
                Console.WriteLine(
                    string.Format(
                        "DeActivating sub service code range [{0}, {1}] "
                            + "@ priority: {2}",
                        d_sscBegin,
                        d_sscEnd,
                        d_sscPriority));
                session.DeactivateSubServiceCodeRange(d_service,
                                                      d_sscBegin,
                                                      d_sscEnd);
            }
        }

        public void Run(string[] args)
        {
            if (!ParseCommandLine(args))
                return;

            SessionOptions.ServerAddress[] servers
                = new SessionOptions.ServerAddress[d_hosts.Count];
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
                    Console.Error.WriteLine("Failed to start session");
                    return;
                }

                ServiceRegistrationOptions serviceRegistrationOptions
                    = new ServiceRegistrationOptions();
                serviceRegistrationOptions.GroupId = d_groupId;
                serviceRegistrationOptions.ServicePriority = d_priority;


                if (d_useSsc) {
                    Console.WriteLine(
                        string.Format(
                            "Activating sub service code range [{0}, {1}] "
                                + "@ priority: {2}",
                            d_sscBegin,
                            d_sscEnd,
                            d_sscPriority));
                    try {
                        serviceRegistrationOptions.AddActiveSubServiceCodeRange(
                            d_sscBegin,
                            d_sscEnd,
                            d_sscPriority);
                    } catch(Exception e) {
                        Console.WriteLine(
                            "FAILED to add active sub service codes. Exception " + e);
                    }
                }

                bool wantAsyncRegisterService = true;
                if (wantAsyncRegisterService)
                {
                    Object registerServiceResponseMonitor = new Object();
                    CorrelationID registerCID = new CorrelationID(registerServiceResponseMonitor);
                    lock (registerServiceResponseMonitor)
                    {
                        if (d_verbose > 0)
                        {
                            Console.WriteLine("start registerServiceAsync, cid = " + registerCID);
                        }
                        session.RegisterServiceAsync(
                            d_service,
                            session.GetAuthorizedIdentity(),
                            registerCID,
                            serviceRegistrationOptions);
                        for (int i = 0; d_registerServiceResponse == null && i < 10; ++i)
                        {
                            Monitor.Wait(registerServiceResponseMonitor, 1000);
                        }
                    }
                }
                else
                {
                    bool result = session.RegisterService(
                        d_service,
                        session.GetAuthorizedIdentity(),
                        serviceRegistrationOptions);
                    d_registerServiceResponse = result;
                }

                Service service = session.GetService(d_service);
                if (service != null && d_registerServiceResponse == true)
                {
                    Console.WriteLine("Service registered: " + d_service);
                }
                else
                {
                    Console.Error.WriteLine("Service registration failed: " + d_service);
                    return;
                }

                // Dump schema for the service
                if (d_verbose > 1)
                {
                    Console.WriteLine("Schema for service:" + d_service);
                    for (int i = 0; i < service.NumEventDefinitions; ++i)
                    {
                        SchemaElementDefinition eventDefinition = service.GetEventDefinition(i);
                        Console.WriteLine(eventDefinition);
                    }
                }

                // Now we will start publishing
                int eventCount = 0;
                long tickCount = 1;
                while (d_running)
                {
                    Event eventObj;
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

                        eventObj = service.CreatePublishEvent();
                        EventFormatter eventFormatter = new EventFormatter(eventObj);

                        bool publishNull = false;
                        if (d_clearInterval > 0 && eventCount == d_clearInterval)
                        {
                            eventCount = 0;
                            publishNull = true;
                        }

                        foreach (Topic topic in d_topicSet.Keys)
                        {
                            if (!topic.IsActive())
                            {
                                System.Console.WriteLine("[WARNING] Publishing on an inactive topic.");
                            }
                            eventFormatter.AppendMessage("MarketDataEvents", topic);
                            if (publishNull)
                            {
                                eventFormatter.SetElementNull("HIGH");
                                eventFormatter.SetElementNull("LOW");
                            }
                            else
                            {
                                ++eventCount;
                                if (1 == tickCount)
                                {
                                    eventFormatter.SetElement("BEST_ASK", 100.0);
                                }
                                else if (2 == tickCount)
                                {
                                    eventFormatter.SetElement("BEST_BID", 99.0);
                                }
                                eventFormatter.SetElement("HIGH", 100 + tickCount * 0.01);
                                eventFormatter.SetElement("LOW", 100 - tickCount * 0.005);
                                ++tickCount;
                            }
                        }
                    }

                    foreach (Message msg in eventObj)
                    {
                        Console.WriteLine(msg);
                    }

                    session.Publish(eventObj);
                    Thread.Sleep(10 * 1000);
                    if (tickCount % 3 == 0)
                    {
                        Deactivate(session);
                        Thread.Sleep(10 * 1000);
                        Activate(session);
                    }
                }
            }
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("MktdataPublisherExample");
            MktdataPublisherExample example = new MktdataPublisherExample();
            example.Run(args);
            Console.WriteLine("Press ENTER to quit");
            Console.Read();
        }
    }
}
