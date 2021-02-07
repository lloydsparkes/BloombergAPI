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
using EventHandler = Bloomberglp.Blpapi.EventHandler;

namespace Bloomberglp.BlpapiExamples.DemoApps
{
    /// <summary>
    /// Summary description for UserModeExample.
    /// </summary>
    public class UserModeExample
    {
        private String d_host;
        private int d_port;
        private List<string> d_securities;
        private List<int> d_uuids;
        private List<Identity> d_identities;
        private List<string> d_programAddresses;

        private Session d_session;
        private Service d_apiAuthSvc;
        private Service d_blpRefDataSvc;

        private static readonly Name RESPONSE_ERROR = Name.GetName("responseError");

        private const String API_AUTH_SVC_NAME = "//blp/apiauth";
        private const String REF_DATA_SVC_NAME = "//blp/refdata";

        public static void Main(String[] args)
        {
            System.Console.WriteLine("UserMode Example");
            UserModeExample example =
                new UserModeExample();
            example.run(args);
        }

        public UserModeExample()
        {
            d_host = "localhost";
            d_port = 8194;

            d_securities = new List<string>();
            d_uuids = new List<int>();
            d_identities = new List<Identity>();
            d_programAddresses = new List<string>();
        }

        private void run(String[] args)
        {
            if (!parseCommandLine(args))
                return;

            createSession();
            OpenServices();

            // Authorize all the users that are interested in receiving data
            authorizeUsers();

            // Make the various requests that we need to make
            sendRefDataRequest();

            // wait for enter key to exit application
            System.Console.Read();

            d_session.Stop();
            System.Console.WriteLine("Exiting.");
        }

        private void createSession()
        {
            SessionOptions options = new SessionOptions();
            options.ServerHost = d_host;
            options.ServerPort = d_port;

            System.Console.WriteLine("Connecting to " + d_host + ":" + d_port);

            d_session = new Session(options, new EventHandler(this.processEvent));
            bool sessionStarted = d_session.Start();
            if (!sessionStarted)
            {
                System.Console.WriteLine("Failed to start session. Exiting...");
                System.Environment.Exit(-1);
            }
        }

        private void OpenServices()
        {
            if (!d_session.OpenService(API_AUTH_SVC_NAME))
            {
                System.Console.WriteLine("Failed to open service: " + API_AUTH_SVC_NAME);
                System.Environment.Exit(-1);
            }

            if (!d_session.OpenService(REF_DATA_SVC_NAME))
            {
                System.Console.WriteLine("Failed to open service: " + REF_DATA_SVC_NAME);
                System.Environment.Exit(-2);
            }

            d_apiAuthSvc = d_session.GetService(API_AUTH_SVC_NAME);
            d_blpRefDataSvc = d_session.GetService(REF_DATA_SVC_NAME);
        }

        public void processEvent(Event eventObj, Session session)
        {
            try
            {
                switch (eventObj.Type)
                {
                    case Event.EventType.SESSION_STATUS:
                    case Event.EventType.SERVICE_STATUS:
                    case Event.EventType.REQUEST_STATUS:
                    case Event.EventType.AUTHORIZATION_STATUS:
                        printEvent(eventObj);
                        break;

                    case Event.EventType.RESPONSE:
                    case Event.EventType.PARTIAL_RESPONSE:
                        processResponseEvent(eventObj);
                        break;
                }
            }
            catch (System.Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }
        }

        private void authorizeUsers()
        {
            // Authorize each of the users
            for (int i = 0; i < d_uuids.Count; ++i)
            {
                Identity identity = d_session.CreateIdentity();
                d_identities.Add(identity);

                Request authRequest = d_apiAuthSvc.CreateAuthorizationRequest();

                authRequest.Set("uuid", d_uuids[i]);
                authRequest.Set("ipAddress", d_programAddresses[i]);

                CorrelationID correlator = new CorrelationID(i);
                EventQueue eventQueue = new EventQueue();
                d_session.SendAuthorizationRequest(authRequest, identity,
                    eventQueue, correlator);

                Event eventObj = eventQueue.NextEvent();
                while (true)
                {
                    printEvent(eventObj);
                    if (eventObj.Type == Event.EventType.RESPONSE ||
                        eventObj.Type == Event.EventType.PARTIAL_RESPONSE ||
                        eventObj.Type == Event.EventType.REQUEST_STATUS)
                    {
                        break;
                    }
                    eventObj = eventQueue.NextEvent();
                }

            }
        }

        private void sendRefDataRequest()
        {
            Request request = d_blpRefDataSvc.CreateRequest(
                                    "ReferenceDataRequest");

            // Add securities.
            Element securities = request.GetElement("securities");
            for (int i = 0; i < d_securities.Count; ++i)
            {
                securities.AppendValue((String)d_securities[i]);
            }

            // Add fields
            Element fields = request.GetElement("fields");
            fields.AppendValue("PX_LAST");
            fields.AppendValue("LAST_UPDATE");

            request.Set("returnEids", true);

            for (int i = 0; i < d_identities.Count; ++i)
            {
                // Send the request for each user
                Identity identity = (Identity)d_identities[i];
                System.Console.WriteLine("Sending RefDataRequest for User " +
                    d_uuids[i]);
                d_session.SendRequest(request, identity, new CorrelationID(d_uuids[i]));
            }
        }

        private void processResponseEvent(Event eventObj)
        {
            foreach (Message msg in eventObj)
            {
                if (msg.HasElement(RESPONSE_ERROR))
                {
                    System.Console.WriteLine(msg);
                    continue;
                }
                long uuid = msg.CorrelationID.Value;
                System.Console.WriteLine("Response for User " + uuid + ": " + msg);
            }
        }

        private bool parseCommandLine(String[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                if (string.Compare(args[i], "-s", true) == 0)
                {
                    d_securities.Add(args[i + 1]);
                }
                else if (string.Compare(args[i], "-c", true) == 0)
                {
                    String credential = args[i + 1];
                    int idx = credential.IndexOf(':');
                    d_uuids.Add(
                        int.Parse(credential.Substring(0, idx)));
                    d_programAddresses.Add(credential.Substring(idx + 1));
                }
                else if (string.Compare(args[i], "-ip", true) == 0)
                {
                    d_host = args[i + 1];
                }
                else if (string.Compare(args[i], "-p", true) == 0)
                {
                    d_port = int.Parse(args[i + 1]);
                }
                else if (string.Compare(args[i], "-h", true) == 0)
                {
                    printUsage();
                    return false;
                }
            }

            if (d_uuids.Count <= 0)
            {
                System.Console.WriteLine("No uuids were specified");
                return false;
            }

            if (d_uuids.Count != d_programAddresses.Count)
            {
                System.Console.WriteLine("Invalid number of program addresses provided");
            }

            if (d_securities.Count <= 0)
            {
                d_securities.Add("MSFT US Equity");
            }

            return true;
        }

        private void printUsage()
        {
            System.Console.WriteLine("Usage:");
            System.Console.WriteLine("    UserMode example");
            System.Console.WriteLine("        [-s        <security   = MSFT US Equity>]");
            System.Console.WriteLine("        [-c        <credential uuid:ipAddress" +
                " eg:12345:10.20.30.40>]");
            System.Console.WriteLine("        [-ip     <ipAddress  = localhost>]");
            System.Console.WriteLine("        [-p     <tcpPort    = 8194>]");
            System.Console.WriteLine("Notes:");
            System.Console.WriteLine("Multiple securities and credentials can be" +
                " specified.");
        }

        private void printEvent(Event eventObj)
        {
            foreach (Message msg in eventObj)
            {
                CorrelationID correlationId = msg.CorrelationID;
                if (correlationId != null)
                {
                    System.Console.WriteLine("Correlator: " + correlationId);
                }

                Service service = msg.Service;
                if (service != null)
                {
                    System.Console.WriteLine("Service: " + service.Name);
                }
                System.Console.WriteLine(msg);
            }
        }

    }
}
