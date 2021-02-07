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
using System.Text;
using Bloomberglp.Blpapi;

namespace Bloomberglp.BlpapiExamples.DemoApps
{
    /// <summary>
    /// Example to demonstrate use of //blp/instruments
    /// </summary>

    public class SecurityLookupExample
    {
        private static readonly Name DESCRIPTION_ELEMENT = Name.GetName("description");
        private static readonly Name QUERY_ELEMENT = Name.GetName("query");
        private static readonly Name RESULTS_ELEMENT = Name.GetName("results");
        private static readonly Name MAX_RESULTS_ELEMENT = Name.GetName("maxResults");
        private static readonly Name SECURITY_ELEMENT = Name.GetName("security");

        private static readonly Name ERROR_RESPONSE = Name.GetName("ErrorResponse");
        private static readonly Name INSTRUMENT_LIST_RESPONSE =
            Name.GetName("InstrumentListResponse");
        private static readonly Name CURVE_LIST_RESPONSE = Name.GetName("CurveListResponse");
        private static readonly Name GOVT_LIST_RESPONSE = Name.GetName("GovtListResponse");

        private static readonly Name INSTRUMENT_LIST_REQUEST =
            Name.GetName("instrumentListRequest");

        private static readonly string INSTRUMENT_SERVICE = "//blp/instruments";
        private static readonly string DEFAULT_HOST = "localhost";
        private static readonly int DEFAULT_PORT = 8194;
        private static readonly string DEFAULT_QUERY_STRING = "IBM";
        private static readonly int DEFAULT_MAX_RESULTS = 10;

        private static readonly string AUTH_OPTION_NONE = "none";
        private static readonly string AUTH_OPTION_USER = "user";
        private static readonly string AUTH_OPTION_APP = "app=";
        private static readonly string AUTH_OPTION_USER_APP = "userapp=";
        private static readonly string AUTH_OPTION_DIR = "dir=";

        private static readonly string[] FILTERS_INSTRUMENTS = {
            "yellowKeyFilter",
            "languageOverride"
        };

        private static readonly string[] FILTERS_GOVT = {
            "ticker",
            "partialMatch"
        };

        private static readonly string[] FILTERS_CURVE = {
            "countryCode",
            "currencyCode",
            "type",
            "subtype",
            "curveid",
            "bbgid"
        };

        private static readonly Name CURVE_ELEMENT = Name.GetName("curve");
        private static readonly Name[] CURVE_RESPONSE_ELEMENTS = {
            Name.GetName("country"),
            Name.GetName("currency"),
            Name.GetName("curveid"),
            Name.GetName("type"),
            Name.GetName("subtype"),
            Name.GetName("publisher"),
            Name.GetName("bbgid")
        };

        private static readonly Name PARSEKY_ELEMENT = Name.GetName("parseky");
        private static readonly Name NAME_ELEMENT = Name.GetName("name");
        private static readonly Name TICKER_ELEMENT = Name.GetName("ticker");

        private string d_queryString = DEFAULT_QUERY_STRING;
        private string d_host = DEFAULT_HOST;
        private Name d_requestType = INSTRUMENT_LIST_REQUEST;
        private int d_port = DEFAULT_PORT;
        private int d_maxResults = DEFAULT_MAX_RESULTS;
        private Dictionary<string, string> d_filters = new Dictionary<string, string>();
        private AuthOptions d_authOptions = new AuthOptions();

        private void PrintUsage()
        {
            Console.WriteLine($@"
Usage:
Instruments Lookup service Example.
    [-r    <requestType> = instrumentListRequest]    requestType: instrumentListRequest|curveListRequest|govtListRequest
    [-ip   <ipAddress = localhost>]
    [-p    <tcpPort = 8194>]
    [-s    <Query string = IBM>]
    [-m    <Max Results = 10>]

    [-auth <option>]  authentication option: user|none|app=<app>|userapp=<app>|dir=<property> (default: none)
        none            applicable to Desktop API product that requires
                        Bloomberg Professional service to be installed locally.
        user            as a user using OS logon information
        dir=<property>  as a user using directory services
        app=<app>       as the specified application
        userapp=<app>   as user and application using logon information for the user

    [-f <filter=value>]  Following are the filters for each request:
        instrumentListRequest: {string.Join("|", FILTERS_INSTRUMENTS)} (default: none)
        govtListRequest:       {string.Join("|", FILTERS_GOVT)} (default: none)
        curveListRequest:      {string.Join("|", FILTERS_CURVE)} (default: none)");
        }

        private void ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                if (string.Compare("-r", args[i], true) == 0 && i + 1 < args.Length)
                {
                    d_requestType = Name.GetName(args[++i]);
                }
                else if (string.Compare("-s", args[i], true) == 0 && i + 1 < args.Length)
                {
                    d_queryString = args[++i];
                }
                else if (string.Compare("-ip", args[i], true) == 0 && i + 1 < args.Length)
                {
                    d_host = args[++i];
                }
                else if (string.Compare("-p", args[i], true) == 0 && i + 1 < args.Length)
                {
                    d_port = int.Parse(args[++i]);
                }
                else if (string.Compare("-m", args[i], true) == 0 && i + 1 < args.Length)
                {
                    d_maxResults = int.Parse(args[++i]);
                }
                else if (string.Compare("-f", args[i], true) == 0 && i + 1 < args.Length)
                {
                    string[] tokens = args[++i].Split('=');
                    if (tokens.Length == 2)
                    {
                        d_filters[tokens[0].Trim()] = tokens[1].Trim();
                    }
                }
                else if (string.Compare("-auth", args[i], true) == 0 && i + 1 < args.Length)
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
                    else if (string.Compare(
                        AUTH_OPTION_APP,
                        0,
                        args[i],
                        0,
                        AUTH_OPTION_APP.Length,
                        true) == 0)
                    {
                        string appName = args[i].Substring(AUTH_OPTION_APP.Length);
                        d_authOptions = new AuthOptions(new AuthApplication(appName));
                    }
                    else if (string.Compare(
                        AUTH_OPTION_DIR,
                        0,
                        args[i],
                        0,
                        AUTH_OPTION_DIR.Length,
                        true) == 0)
                    {
                        string dir = args[i].Substring(AUTH_OPTION_DIR.Length);
                        d_authOptions = new AuthOptions(
                            AuthUser.CreateWithActiveDirectoryProperty(dir));
                    }
                    else if (string.Compare(
                        AUTH_OPTION_USER_APP,
                        0,
                        args[i],
                        0,
                        AUTH_OPTION_USER_APP.Length,
                        true) == 0)
                    {
                        string appName = args[i].Substring(AUTH_OPTION_APP.Length);
                        d_authOptions = new AuthOptions(
                            AuthUser.CreateWithLogonName(),
                            new AuthApplication(appName));
                    }
                    else
                    {
                        throw new Exception(string.Format("Invalid -auth option: {0}", args[i]));
                    }
                }
                else
                {
                    throw new Exception(string.Format("Unknown option {0}", args[i]));
                }
            }
        }

        private void ProcessInstrumentListResponse(Message msg)
        {
            Element results = msg.GetElement(RESULTS_ELEMENT);
            int numResults = results.NumValues;
            Console.WriteLine("Processing " + numResults + " results:");
            for (int i = 0; i < numResults; ++i)
            {
                Element result = results.GetValueAsElement(i);
                Console.WriteLine(
                        "\t{0} {1} - {2}",
                        i + 1,
                        result.GetElementAsString(SECURITY_ELEMENT),
                        result.GetElementAsString(DESCRIPTION_ELEMENT));
            }
        }

        private void ProcessCurveListResponse(Message msg)
        {
            Element results = msg.GetElement(RESULTS_ELEMENT);
            int numResults = results.NumValues;
            Console.WriteLine("Processing " + numResults + " results:");
            for (int i = 0; i < numResults; ++i)
            {
                Element result = results.GetValueAsElement(i);
                StringBuilder sb = new StringBuilder();
                foreach (Name n in CURVE_RESPONSE_ELEMENTS)
                {
                    if (sb.Length != 0)
                    {
                        sb.Append(" ");
                    }
                    sb.Append(n).Append("=").Append(result.GetElementAsString(n));
                }
                Console.WriteLine(
                        "\t{0} {1} - {2} '{3}'",
                        i + 1,
                        result.GetElementAsString(CURVE_ELEMENT),
                        result.GetElementAsString(DESCRIPTION_ELEMENT),
                        sb);
            }
        }

        private void ProcessGovtListResponse(Message msg)
        {
            Element results = msg.GetElement(RESULTS_ELEMENT);
            int numResults = results.NumValues;
            Console.WriteLine("Processing " + numResults + " results:");
            for (int i = 0; i < numResults; ++i)
            {
                Element result = results.GetValueAsElement(i);
                Console.WriteLine(
                        "\t{0} {1}, {2} - {3}",
                        i + 1,
                        result.GetElementAsString(PARSEKY_ELEMENT),
                        result.GetElementAsString(NAME_ELEMENT),
                        result.GetElementAsString(TICKER_ELEMENT));
            }
        }

        private void ProcessResponseEvent(Event eventObj)
        {
            foreach (Message msg in eventObj)
            {
                if (msg.MessageType == ERROR_RESPONSE)
                {
                    string description = msg.GetElementAsString(DESCRIPTION_ELEMENT);
                    Console.WriteLine("Received error: " + description);
                }
                else if (msg.MessageType == INSTRUMENT_LIST_RESPONSE)
                {
                    ProcessInstrumentListResponse(msg);
                }
                else if (msg.MessageType == CURVE_LIST_RESPONSE)
                {
                    ProcessCurveListResponse(msg);
                }
                else if (msg.MessageType == GOVT_LIST_RESPONSE)
                {
                    ProcessGovtListResponse(msg);
                }
                else
                {
                    Console.Error.WriteLine("Unknown MessageType received");
                }
            }
        }

        private void EventLoop(Session session)
        {
            bool done = false;
            while (!done)
            {
                Event eventObj = session.NextEvent();
                if (eventObj.Type == Event.EventType.PARTIAL_RESPONSE)
                {
                    System.Console.WriteLine("Processing Partial Response");
                    ProcessResponseEvent(eventObj);
                }
                else if (eventObj.Type == Event.EventType.RESPONSE)
                {
                    System.Console.WriteLine("Processing Response");
                    ProcessResponseEvent(eventObj);
                    done = true;
                }
                else
                {
                    foreach (Message msg in eventObj)
                    {
                        System.Console.WriteLine(msg);
                        if (eventObj.Type == Event.EventType.SESSION_STATUS)
                        {
                            if (msg.MessageType.Equals(Names.SessionTerminated) ||
                                msg.MessageType.Equals(Names.SessionStartupFailure))
                            {
                                done = true;
                            }
                        }
                    }
                }
            }
        }

        private void SendRequest(Session session)
        {
            Console.WriteLine("Sending Request: {0}", d_requestType);
            Service instrumentService = session.GetService(INSTRUMENT_SERVICE);
            Request request;
            try
            {
                request = instrumentService.CreateRequest(d_requestType.ToString());
            }
            catch (NotFoundException e)
            {
                throw new Exception(
                    string.Format("Request Type not found: {0}", d_requestType),
                    e);
            }
            request.Set(QUERY_ELEMENT, d_queryString);
            request.Set(MAX_RESULTS_ELEMENT, d_maxResults);

            foreach (KeyValuePair<string, string> entry in d_filters)
            {
                try
                {
                    request.Set(entry.Key, entry.Value);
                }
                catch (NotFoundException e)
                {
                    throw new Exception(string.Format("Filter not found: {0}", entry.Key), e);
                }
                catch (InvalidConversionException e)
                {
                    throw new Exception(
                        string.Format(
                            "Invalid value: {0} for filter: {1}",
                            entry.Value,
                            entry.Key),
                        e);
                }
            }
            request.Print(Console.Out);
            Console.WriteLine();
            session.SendRequest(request, correlationId: null);
        }

        private void Run(string[] args)
        {
            try
            {
                ParseCommandLine(args);
                SessionOptions sessionOptions = new SessionOptions();
                sessionOptions.ServerHost = d_host;
                sessionOptions.ServerPort = d_port;
                sessionOptions.SetSessionIdentityOptions(d_authOptions);
                Console.WriteLine("Connecting to {0}:{1}", d_host, d_port);
                using (Session session = new Session(sessionOptions))
                {
                    if (!session.Start())
                    {
                        throw new Exception("Failed to start session");
                    }

                    if (!session.OpenService(INSTRUMENT_SERVICE))
                    {
                        throw new Exception(
                            string.Format("Failed to open: {0}", INSTRUMENT_SERVICE));
                    }

                    SendRequest(session);
                    EventLoop(session);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Exception: {0}", e.Message));
                Console.WriteLine();
                PrintUsage();
            }
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("SecurityLookupExample");
            SecurityLookupExample example = new SecurityLookupExample();
            example.Run(args);
            System.Console.WriteLine("Press ENTER to quit");
            System.Console.Read();
        }
    }
}
