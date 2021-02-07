/* Copyright 2019. Bloomberg Finance L.P.
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

namespace Bloomberglp.BlpapiExamples.DemoApps
{
    using System;
    using System.Collections.Generic;
    using Bloomberglp.Blpapi;
    using EventType = Bloomberglp.Blpapi.Event.EventType;

    public class RefDataExample
    {
        private static readonly Name SecurityData = new Name("securityData");
        private static readonly Name Security = new Name("security");
        private static readonly Name FieldData = new Name("fieldData");
        private static readonly Name ResponseError = new Name("responseError");
        private static readonly Name SecurityError = new Name("securityError");
        private static readonly Name FieldExceptions = new Name("fieldExceptions");
        private static readonly Name FieldId = new Name("fieldId");
        private static readonly Name ErrorInfo = new Name("errorInfo");
        private static readonly Name Category = new Name("category");
        private static readonly Name Message = new Name("message");

        private const string RefDataService = "//blp/refdata";
        private const string AuthOptionNone = "none";
        private const string AuthOptionUser = "user";
        private const string AuthOptionApp = "app=";
        private const string AuthOptionDir = "dir=";
        private const string Port8194 = "8194";
        private const string Port8196 = "8196";

        private string host;
        private int port;
        private List<string> securities;
        private List<string> fields;

        private AuthOptions authOptions = new AuthOptions();
        private TlsOptions tlsOptions;
        private bool zfpOverLeasedLine;
        private ZfpUtil.Remote remote;

        public static void Main(string[] args)
        {
            Console.WriteLine("Reference Data Example");
            var example = new RefDataExample();
            example.Run(args);

            Console.WriteLine("Press ENTER to quit");
            Console.Read();
        }

        public RefDataExample()
        {
            this.host = "localhost";
            this.port = 8194;
            this.securities = new List<string>();
            this.fields = new List<string>();
        }

        /// <summary>
        /// Prints RequestId associated with the <paramref name="message"/>.
        /// <para>
        /// <see cref="Message"/> can have an associated RequestId that is used
        /// to identify the operation through the network. When contacting
        /// support please provide the RequestId.
        /// </para>
        /// </summary>
        private static void PrintContactSupportMessage(Message message)
        {
            string requestId = message.RequestId;
            if (requestId != null)
            {
                Console.Error.WriteLine("When contacting support, " +
                    $"please provide RequestId {requestId}.");
            }
        }

        /// <summary>
        /// Prints the messages in the <paramref name="event"/>
        /// <para>
        /// When using a session identity, i.e.
        /// <see cref="SessionOptions.SetSessionIdentityOptions"/>, token
        /// generation failure, authorization failure or revocation terminates
        /// the session, in which case, applications only need to check session
        /// status messages. Applications don't need to handle token or
        /// authorization messages, which are still printed.
        /// </para>
        /// </summary>
        /// <returns>
        /// true if session has failed to start or terminated; false otherwise.
        /// </returns>
        private static bool ProcessGenericEvent(Event @event) {
            foreach (Message msg in @event)
            {
                Console.WriteLine(msg);

                if (@event.Type == EventType.SESSION_STATUS)
                {
                    if (msg.MessageType.Equals(Names.SessionTerminated)
                        || msg.MessageType.Equals(Names.SessionStartupFailure))
                    {
                        string error = msg.GetElement("reason")
                            .GetElementAsString("description");
                        Console.Error.WriteLine($"Failed to start session: {error}");
                        PrintContactSupportMessage(msg);
                        return true;
                    }
                }
                else if (@event.Type == EventType.SERVICE_STATUS)
                {
                    if (msg.MessageType.Equals(Names.ServiceOpenFailure))
                    {
                        string serviceName = msg.GetElementAsString("serviceName");
                        string error = msg.GetElement("reason")
                            .GetElementAsString("description");
                        Console.Error.WriteLine($"Failed to open {serviceName}: {error}");
                        PrintContactSupportMessage(msg);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks failure events published by the session.
        /// <para>
        /// Note that the loop uses <see cref="Session.TryNextEvent"/> as all
        /// events have been produced before calling this function, but there
        /// could be no events at all in the queue if the OS fails to allocate
        /// resources.
        /// </para>.
        /// </summary>
        private static void CheckFailures(Session session)
        {
            while (true)
            {
                Event @event = session.TryNextEvent();
                if (@event == null)
                {
                    break;
                }

                if (ProcessGenericEvent(@event))
                {
                    break;
                }
            }
        }

        private void Run(string[] args)
        {
            if (!this.ParseCommandLine(args)) return;

            SessionOptions sessionOptions = this.zfpOverLeasedLine ?
                this.PrepareZfpSessionOptions() :
                this.PrepareStandardSessionOptions();

            sessionOptions.AutoRestartOnDisconnection = true;
            sessionOptions.SetSessionIdentityOptions(this.authOptions);

            using (var session = new Session(sessionOptions))
            {
                if (!session.Start())
                {
                    Console.Error.WriteLine("Failed to start session.");
                    CheckFailures(session);
                    return;
                }

                if (!session.OpenService(RefDataService))
                {
                    CheckFailures(session);
                    return;
                }

                try
                {
                    this.SendRefDataRequest(session);
                    WaitForResponse(session);
                }
                catch (InvalidRequestException e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        /// <summary>
        /// Waits for response after sending the request.
        /// <para>
        /// Success response can come with a number of
        /// <see cref="EventType.PARTIAL_RESPONSE"/> events followed by a
        /// <see cref="EventType.RESPONSE"/>. Failures will be delivered in a
        /// <see cref="EventType.REQUEST_STATUS"/> event holding a
        /// <see cref="Names.RequestFailure"/> message.
        /// </para>
        /// </summary>
        private static void WaitForResponse(Session session)
        {
            bool done = false;
            while (!done)
            {
                Event @event = session.NextEvent();
                if (@event.Type == EventType.PARTIAL_RESPONSE)
                {
                    Console.WriteLine("Processing Partial Response");
                    ProcessResponseEvent(@event);
                }
                else if(@event.Type == EventType.RESPONSE)
                {
                    Console.WriteLine("Processing Response");
                    ProcessResponseEvent(@event);
                    done = true;
                }
                else if (@event.Type == EventType.REQUEST_STATUS)
                {
                    foreach (Message msg in @event)
                    {
                        Console.WriteLine(msg);
                        if (msg.MessageType.Equals(Names.RequestFailure))
                        {
                            string error = msg.GetElement("reason")
                                .GetElementAsString("description");
                            Console.Error.WriteLine($"Request failed: {error}");
                            PrintContactSupportMessage(msg);
                            done = true;
                        }
                    }
                }
                else
                {
                    // SESSION_STATUS events can happen at any time and should
                    // be handled as the session can be terminated, e.g.
                    // session identity can be revoked at a later time, which
                    // terminates the session.
                    done = ProcessGenericEvent(@event);
                }
            }
        }

        /// <summary>
        /// Processes a response, either partial or final, to the reference
        /// data request.
        /// </summary>
        private static void ProcessResponseEvent(Event eventObj)
        {
            foreach (Message msg in eventObj)
            {
                Console.WriteLine($"Received response to request {msg.RequestId}");

                if (msg.HasElement(ResponseError))
                {
                    PrintErrorInfo(
                        msg,
                        "REQUEST FAILED: ",
                        msg.GetElement(ResponseError));
                    continue;
                }

                Element securities = msg.GetElement(SecurityData);
                int numSecurities = securities.NumValues;
                Console.WriteLine($"Processing {numSecurities} securities:");
                for (int i = 0; i < numSecurities; ++i)
                {
                    Element security = securities.GetValueAsElement(i);
                    string ticker = security.GetElementAsString(Security);
                    Console.WriteLine();
                    Console.WriteLine($"Ticker: {ticker}");
                    if (security.HasElement("securityError"))
                    {
                        PrintErrorInfo(
                            msg,
                            "SECURITY FAILED: ",
                            security.GetElement(SecurityError));
                        continue;
                    }

                    Element fieldData = security.GetElement(FieldData);
                    if (fieldData.NumElements > 0)
                    {
                        Console.WriteLine("FIELD\t\tVALUE");
                        Console.WriteLine("-----\t\t-----");
                        int numElements = fieldData.NumElements;
                        for (int j = 0; j < numElements; ++j)
                        {
                            Element field = fieldData.GetElement(j);
                            Console.WriteLine($"{field.Name}\t\t{field.GetValueAsString()}");
                        }
                    }
                    Console.WriteLine("");
                    Element fieldExceptions = security.GetElement(FieldExceptions);
                    if (fieldExceptions.NumValues > 0)
                    {
                        Console.WriteLine("FIELD\t\tEXCEPTION");
                        Console.WriteLine("-----\t\t---------");
                        for (int k = 0; k < fieldExceptions.NumValues; ++k)
                        {
                            Element fieldException =
                                fieldExceptions.GetValueAsElement(k);
                            PrintErrorInfo(
                                msg,
                                $"{fieldException.GetElementAsString(FieldId)}\t\t",
                                fieldException.GetElement(ErrorInfo));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sends a reference data request.
        /// </summary>
        private void SendRefDataRequest(Session session)
        {
            Service refDataService = session.GetService(RefDataService);
            Request request = refDataService.CreateRequest("ReferenceDataRequest");

            // Add securities to request
            Element securitiesElement = request.GetElement("securities");

            foreach (string security in this.securities)
            {
                securitiesElement.AppendValue(security);
            }

            // Add fields to request
            Element fieldsElement = request.GetElement("fields");
            foreach (string field in this.fields)
            {
                fieldsElement.AppendValue(field);
            }

            // Every request has a RequestId, which is automatically generated,
            // and used to identify the operation through the network and also
            // present in the response messages. The RequestId should be
            // provided when contacting support.
            Console.WriteLine($"Sending Request {request.RequestId}: {request}");
            session.SendRequest(request, correlationId: null);
        }

        private bool ParseAuthOptions(string value)
        {
            if (string.Compare(AuthOptionNone, value, ignoreCase: true) == 0)
            {
                this.authOptions = new AuthOptions();
            }
            else if (string.Compare(AuthOptionUser, value, ignoreCase: true) == 0)
            {
                this.authOptions = new AuthOptions(AuthUser.CreateWithLogonName());
            }
            else if (string.Compare(
                AuthOptionApp,
                0,
                value,
                0,
                AuthOptionApp.Length,
                ignoreCase: true) == 0)
            {
                string appName = value.Substring(AuthOptionApp.Length);
                this.authOptions = new AuthOptions(new AuthApplication(appName));
            }
            else if (string.Compare(
                AuthOptionDir,
                0,
                value,
                0,
                AuthOptionDir.Length,
                ignoreCase: true) == 0)
            {
                string dir = value.Substring(AuthOptionDir.Length);
                this.authOptions = new AuthOptions(
                    AuthUser.CreateWithActiveDirectoryProperty(dir));
            }
            else
            {
                return false;
            }

            return true;
        }

        private bool ParseCommandLine(string[] args)
        {
            string clientCredentials = null;
            string clientCredentialsPassword = null;
            string trustMaterial = null;

            for (int i = 0; i < args.Length; ++i)
            {
                string option = args[i];

                // All options require a value, return false if no value is
                // present for any of the options.
                if (i + 1 == args.Length)
                {
                    PrintUsage();
                    return false;
                }

                string value = args[++i];
                if (string.Compare(option, "-s", true) == 0)
                {
                    this.securities.Add(value);
                }
                else if (string.Compare(option, "-f", true) == 0)
                {
                    this.fields.Add(value);
                }
                else if (string.Compare(option, "-ip", true) == 0)
                {
                    this.host = value;
                }
                else if (string.Compare(option, "-p", true) == 0)
                {
                    this.port = int.Parse(value);
                }
                else if (string.Compare(option, "-h", true) == 0)
                {
                    PrintUsage();
                    return false;
                }
                else if (string.Compare("-auth", option, true) == 0) {
                    if (!ParseAuthOptions(value))
                    {
                        PrintUsage();
                        return false;
                    }
                }
                else if (string.Compare("-tls-client-credentials", option, true) == 0) {
                    clientCredentials = value;
                }
                else if (string.Compare("-tls-client-credentials-password", option, true) == 0) {
                    clientCredentialsPassword = value;
                }
                else if (string.Compare("-tls-trust-material", option, true) == 0) {
                    trustMaterial = value;
                }
                else if (string.Compare("-zfp-over-leased-line", option, true) == 0) {
                    this.zfpOverLeasedLine = true;
                    if (!TryGetRemote(value, out this.remote)) {
                        PrintUsage();
                        return false;
                    }
                }
            }

            // handle default arguments
            if (this.securities.Count == 0)
            {
                this.securities.Add("IBM US Equity");
                this.securities.Add("MSFT US Equity");
            }

            if (this.fields.Count == 0)
            {
                this.fields.Add("PX_LAST");
            }

            if (clientCredentials != null &&
                trustMaterial != null &&
                clientCredentialsPassword != null) {
                using (var password = new System.Security.SecureString()) {
                    foreach (var c in clientCredentialsPassword) {
                        password.AppendChar(c);
                    }

                    this.tlsOptions = TlsOptions.CreateFromFiles(clientCredentials, password, trustMaterial);
                }
            }
            else if (this.zfpOverLeasedLine) {
                Console.WriteLine("TLS parameters are required for ZFP connections over a leased line.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prints the error in <paramref name="errorInfo"/> provided by a
        /// reference data response.
        /// </summary>
        private static void PrintErrorInfo(
            Message message,
            string leadingStr,
            Element errorInfo)
        {
            Console.Error.WriteLine($"{leadingStr}{errorInfo.GetElementAsString(Category)}" +
                $" ({errorInfo.GetElementAsString(Message)})");
            PrintContactSupportMessage(message);
        }

        private SessionOptions PrepareStandardSessionOptions()
        {
            var sessionOptions = new SessionOptions();
            var servers = new []
            {
                new SessionOptions.ServerAddress(this.host, this.port)
            };

            sessionOptions.ServerAddresses = servers;
            sessionOptions.NumStartAttempts = 1;

            Console.WriteLine($"Connecting to {sessionOptions.ServerHost}:{sessionOptions.ServerPort}.");

            return sessionOptions;
        }

        private SessionOptions PrepareZfpSessionOptions()
        {
            Console.WriteLine("Creating a ZFP connection for leased lines.");

            SessionOptions sessionOptions = ZfpUtil.GetZfpOptionsForLeasedLines(
                this.remote,
                this.tlsOptions);

            return sessionOptions;
        }

        private static void PrintUsage()
        {
            Console.WriteLine(@"
Retrieve reference data
Usage:
    [-ip <ipAddress = localhost>]
    [-p <tcpPort = 8194>]
    [-s <security = IBM US Equity>]
    [-f <field = PX_LAST>]

    [-auth <option>] authentication option: none|user|app=<app>|userapp=<app>|dir=<property> (default: none)
        none            applicable to Desktop API product that requires
                        Bloomberg Professional service to be installed locally.
        user            as a user using OS logon information
        dir=<property>  as a user using directory services
        app=<app>       as the specified application
        userapp=<app>   as user and application using logon information for the user

    TLS OPTIONS (specify all or none):
        [-tls-client-credentials <file>]          name a PKCS#12 file to use as a source of client credentials
        [-tls-client-credentials-password <pwd>]  specify password for accessing client credentials
        [-tls-trust-material <file>]              name a PKCS#7 file to use as a source of trusted certificates

    ZFP connections over leased lines (requires TLS options):
        [-zfp-over-leased-line <port>]  enable ZFP connections over leased lines on the specified port (8194 or 8196)
            (When this option is enabled, '-ip' and '-p' arguments will be ignored.");
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
    }
}
