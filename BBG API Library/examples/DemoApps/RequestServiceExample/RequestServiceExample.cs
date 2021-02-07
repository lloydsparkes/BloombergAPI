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
    public class RequestServiceExample
    {
        private enum Role
        {
            SERVER,
            CLIENT,
            BOTH
        }

        private static readonly Name REFERENCE_DATA_REQUEST = Name.GetName("ReferenceDataRequest");

        private static readonly string AUTH_OPTION_NONE = "none";
        private static readonly string AUTH_OPTION_USER = "user";
        private static readonly string AUTH_OPTION_APP = "app=";
        private static readonly string AUTH_OPTION_DIR = "dir=";

        private string d_service = "//example/refdata";
        private int d_verbose = 0;
        private List<string> d_hosts = new List<string>();
        private int d_port = 8194;
        private int d_numRetry = 2;

        private AuthOptions d_authOptions = new AuthOptions(AuthUser.CreateWithLogonName());
        private List<string> d_securities = new List<string>();
        private List<string> d_fields = new List<string>();
        private Role d_role = Role.BOTH;

        static double GetTimestamp()
        {
            return ((double)System.DateTime.Now.Ticks) / 10000000;
        }

        private void ProcessServerEvent(Event eventObj, ProviderSession session)
        {
            Console.WriteLine("Server received event " + eventObj.Type);
            if (eventObj.Type == Event.EventType.REQUEST)
            {
                foreach (Message msg in eventObj)
                {
                    Console.WriteLine("Message = " + msg);
                    if (msg.MessageType.Equals(REFERENCE_DATA_REQUEST))
                    {
                        // Similar to createPublishEvent. We assume just one
                        // service - d_service. A responseEvent can only be
                        // for single request so we can specify the
                        // correlationId - which establishes context -
                        // when we create the Event.
                        Service service = session.GetService(d_service);
                        if (msg.HasElement("timestamp"))
                        {
                            double requestTime = msg.GetElementAsFloat64("timestamp");
                            double latency = GetTimestamp() - requestTime;
                            Console.WriteLine("Request latency = " + latency);
                        }
                        Event response = service.CreateResponseEvent(msg.CorrelationID);
                        EventFormatter ef = new EventFormatter(response);

                        // In AppendResponse the string is the name of the
                        // operation, the correlationId indicates
                        // which request we are responding to.
                        ef.AppendResponse("ReferenceDataRequest");
                        Element securities = msg.GetElement("securities");
                        Element fields = msg.GetElement("fields");
                        ef.SetElement("timestamp", GetTimestamp());
                        ef.PushElement("securityData");
                        for (int i = 0; i < securities.NumValues; ++i)
                        {
                            ef.AppendElement();
                            ef.SetElement("security", securities.GetValueAsString(i));
                            ef.PushElement("fieldData");
                            for (int j = 0; j < fields.NumValues; ++j)
                            {
                                ef.AppendElement();
                                ef.SetElement("fieldId", fields.GetValueAsString(j));
                                ef.PushElement("data");
                                ef.SetElement("doubleValue", GetTimestamp());
                                ef.PopElement();
                                ef.PopElement();
                            }
                            ef.PopElement();
                            ef.PopElement();
                        }
                        ef.PopElement();

                        // Service is implicit in the Event. sendResponse has a
                        // second parameter - partialResponse -
                        // that defaults to false.
                        session.SendResponse(response);
                    }
                }
            }
            else
            {
                foreach (Message msg in eventObj)
                {
                    Console.WriteLine("Message = " + msg);
                }
            }
        }

        private void ProcessClientEvent(Event eventObj, Session session)
        {
            Console.WriteLine("Client received event " + eventObj.Type);
            foreach (Message msg in eventObj)
            {
                Console.WriteLine("Message = " + msg);
            }
        }

        private void PrintUsage()
        {
            Console.WriteLine(@"
Usage:
    [-ip   <ipAddress>]  server name or IP (default: localhost)
    [-p    <tcpPort>]    server port (default: 8194)
    [-t    <number>]     number of retrying connection on disconnected (default: number of hosts)
    [-v]                 increase verbosity (can be specified more than once)

    [-auth <option>]     authentication option: user|none|app=<app>|dir=<property> (default: user)
        none             applicable to Desktop API product that requires
                         Bloomberg Professional service to be installed locally.
        user             as a user using OS logon information
        dir=<property>   as a user using directory services
        app=<app>        as the specified application

    [-s    <security>]   request security for client (default: IBM US Equity)
    [-f    <field>]      request field for client (default: PX_LAST)
    [-r    <option>]     service role option: server|client|both (default: both)");
        }

        private bool ParseCommandLine(string[] args)
        {
            bool numRetryProvidedByUser = false;

            for (int i = 0; i < args.Length; ++i)
            {
                if (string.Compare("-v", args[i], true) == 0)
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
                else if (string.Compare("-t", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_numRetry = int.Parse(args[++i]);
                    numRetryProvidedByUser = true;
                }
                else if (string.Compare("-s", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_securities.AddRange(args[++i].Split(','));
                }
                else if (string.Compare("-f", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    d_fields.AddRange(args[++i].Split(','));
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
                else if (string.Compare("-r", args[i], true) == 0
                    && i + 1 < args.Length)
                {
                    ++i;
                    if (string.Compare("server", args[i], true) == 0)
                    {
                        d_role = Role.SERVER;
                    }
                    else if (string.Compare("client", args[i], true) == 0)
                    {
                        d_role = Role.CLIENT;
                    }
                    else if (string.Compare("both", args[i], true) == 0)
                    {
                        d_role = Role.BOTH;
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
            if (d_securities.Count == 0)
            {
                d_securities.Add("IBM US Equity");
            }
            if (d_fields.Count == 0)
            {
                d_fields.Add("PX_LAST");
            }
            if (!numRetryProvidedByUser)
            {
                d_numRetry = d_hosts.Count;
            }

            return true;
        }

        private void ServerRun(ProviderSession providerSession)
        {
            Console.WriteLine("Server is starting------");
            if (!providerSession.Start())
            {
                Console.Error.WriteLine("Failed to start server session");
                return;
            }

            if (!providerSession.RegisterService(d_service, providerSession.GetAuthorizedIdentity()))
            {
                Console.Error.WriteLine("Failed to register " + d_service);
                return;
            }
        }

        private void ClientRun(Session session)
        {
            Console.WriteLine("Client is starting------");
            if (!session.Start())
            {
                Console.Error.WriteLine("Failed to start client session");
                return;
            }

            if (!session.OpenService(d_service))
            {
                Console.Error.WriteLine("Failed to open " + d_service);
                return;
            }

            Service service = session.GetService(d_service);
            Request request = service.CreateRequest("ReferenceDataRequest");

            // Add securities to request
            Element securities = request.GetElement("securities");
            for (int i = 0; i < d_securities.Count; ++i)
            {
                securities.AppendValue(d_securities[i]);
            }
            // Add fields to request
            Element fields = request.GetElement("fields");
            for (int i = 0; i < d_fields.Count; ++i)
            {
                fields.AppendValue(d_fields[i]);
            }
            // Set time stamp
            request.Set("timestamp", GetTimestamp());

            Console.WriteLine("Sending Request: " + request);
            EventQueue eventQueue = new EventQueue();
            session.SendRequest(request, eventQueue, new CorrelationID());

            while (true)
            {
                Event eventObj = eventQueue.NextEvent();
                Console.WriteLine("Client received an event");
                foreach (Message msg in eventObj)
                {
                    if (eventObj.Type == Event.EventType.RESPONSE)
                    {
                        if (msg.HasElement("timestamp"))
                        {
                            double responseTime = msg.GetElementAsFloat64("timestamp");
                            double latency = GetTimestamp() - responseTime;
                            Console.WriteLine("Response latency = " + latency);
                        }
                    }
                    Console.WriteLine(msg);
                }
                if (eventObj.Type == Event.EventType.RESPONSE)
                {
                    break;
                }
            }
        }

        public void Run(string[] args)
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
            sessionOptions.NumStartAttempts = d_numRetry;

            Console.Write("Connecting to");
            foreach (SessionOptions.ServerAddress server in sessionOptions.ServerAddresses)
            {
                Console.Write(" " + server);
            }
            Console.WriteLine();

            if (d_role == Role.SERVER || d_role == Role.BOTH)
            {
                using (ProviderSession session = new ProviderSession(sessionOptions, ProcessServerEvent))
                {
                    ServerRun(session);
                }
            }

            if (d_role == Role.CLIENT || d_role == Role.BOTH)
            {
                using (Session session = new Session(sessionOptions, ProcessClientEvent))
                {
                    ClientRun(session);
                }
            }
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("RequestServiceExample");
            RequestServiceExample example = new RequestServiceExample();
            example.Run(args);
            Console.WriteLine("Press ENTER to quit");
            System.Console.Read();
        }
    }
}
