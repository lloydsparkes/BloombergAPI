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

namespace Bloomberglp.BlpapiExamples.DemoApps
{
    using System;
    using System.Collections.Generic;
    using Bloomberglp.Blpapi;
    using EventType = Bloomberglp.Blpapi.Event.EventType;

    public class LocalMktdataSubscriptionExample
    {
        private const string AuthOptionNone = "none";
        private const string AuthOptionUser = "user";
        private const string AuthOptionApp = "app=";
        private const string AuthOptionUserApp = "userapp=";
        private const string AuthOptionDir = "dir=";
        private const string AuthOptionManual = "manual=";
        private const string Port8194 = "8194";
        private const string Port8196 = "8196";

        private const string DefaultHost = "localhost";
        private const int DefaultPort = 8194;
        private const string DefaultService = "//viper/mktdata";
        private const string DefaultTicker = "/ticker/IBM Equity";
        private const int DefaultMaxEvents = int.MaxValue;

        private List<string> hosts = new List<string>();
        private int port = DefaultPort;
        private string service = DefaultService;
        private int maxEvents = DefaultMaxEvents;
        private AuthOptions authOptions = new AuthOptions(AuthUser.CreateWithLogonName());
        private List<string> topics = new List<string>();
        private List<string> fields = new List<string>();
        private List<string> options = new List<string>();

        private TlsOptions tlsOptions;
        private bool zfpOverLeasedLine;
        private ZfpUtil.Remote remote;

        public void Run(string[] args)
        {
            if (!this.ParseCommandLine(args))
            {
                return;
            }

            var sessionOptions = this.zfpOverLeasedLine ?
                this.PrepareZfpSessionOptions() :
                this.PrepareStandardSessionOptions();

            sessionOptions.SetSessionIdentityOptions(this.authOptions);

            using (var session = new Session(sessionOptions))
            {
                if (!session.Start())
                {
                    Console.Error.WriteLine("Failed to start session.");
                    CheckFailures(session);
                    return;
                }

                if (!session.OpenService(this.service))
                {
                    CheckFailures(session);
                    return;
                }

                var subscriptions = new List<Subscription>();
                foreach (string topic in this.topics)
                {
                    subscriptions.Add(
                        new Subscription(
                            this.service + topic,
                            this.fields,
                            this.options,
                            new CorrelationID(topic)));
                }

                session.Subscribe(subscriptions);
                this.ProcessSubscriptionResponse(session);
            }
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
        /// Prints the error if the <paramref name="message"/> is a failure message.
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
        private static bool ProcessGenericMessage(
            EventType eventType,
            Message message)
        {
            if (eventType == EventType.SESSION_STATUS)
            {
                if (message.MessageType.Equals(Names.SessionTerminated) ||
                    message.MessageType.Equals(Names.SessionStartupFailure))
                {
                    string error = message.GetElement("reason")
                        .GetElementAsString("description");
                    Console.Error.WriteLine($"Session failed to start or terminated: {error}");
                    PrintContactSupportMessage(message);
                    return true;
                }
            }
            else if (eventType == EventType.SERVICE_STATUS)
            {
                if (message.MessageType.Equals(Names.ServiceOpenFailure))
                {
                    string serviceName = message.GetElementAsString("serviceName");
                    string error = message.GetElement("reason")
                        .GetElementAsString("description");
                    Console.Error.WriteLine($"Failed to open {serviceName}: {error}");
                    PrintContactSupportMessage(message);
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
        /// </para>
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

                EventType eventType = @event.Type;
                foreach (Message msg in @event)
                {
                    Console.WriteLine(msg);

                    if (ProcessGenericMessage(eventType, msg))
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Prepares <see cref="SessionOptions"/> for connections other than
        /// ZFP Leased Line connections.
        /// </summary>
        private SessionOptions PrepareStandardSessionOptions()
        {
            var sessionOptions = new SessionOptions();
            var servers = new SessionOptions.ServerAddress[this.hosts.Count];
            for (int i = 0; i < this.hosts.Count; ++i) {
                servers[i] = new SessionOptions.ServerAddress(this.hosts[i], this.port);
            }

            sessionOptions.ServerAddresses = servers;

            Console.WriteLine($"Connecting to port {this.port} on ");
            foreach (string host in this.hosts) {
                Console.WriteLine($"{host} ");
            }

            if (this.tlsOptions != null)
            {
                Console.WriteLine("TlsOptions enabled");
                sessionOptions.TlsOptions = this.tlsOptions;
            }

            return sessionOptions;
        }

        /// <summary>
        /// Prepares <see cref="SessionOptions"/> for ZFP Leased Line connections.
        /// </summary>
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
            Console.WriteLine($@"
Usage:
    [-ip <ipAddress>]    server name or IP     (default = {DefaultHost})
    [-p <tcpPort>]       server port           (default = {DefaultPort})
    [-s <service>]       service name          (default = {DefaultService})
    [-t <topic>]         topic to subscribe to (default = ""{DefaultTicker}"")
    [-f <field>]         field to subscribe to (default: empty)
    [-o <option>]        subscription options  (default: empty)
    [-me <maxEvents>]    max number of events  (default = {DefaultMaxEvents})
    [-auth <option>]     authorization option  (default = {AuthOptionUser})
        none                  applicable to Desktop API product that requires
                              Bloomberg Professional service to be installed locally.
        user                  as a user using OS logon information
        dir=<property>        as a user using directory services
        app=<app>             as the specified application
        userapp=<app>         as user and application using logon information for the user
        manual=<app,ip,user>  as user and application, with manually provided IP address and user

    TLS OPTIONS (specify all or none):
        [-tls-client-credentials <file>]           name a PKCS#12 file to use as a source of client credentials
        [-tls-client-credentials-password <file>]  specify password for accessing client credentials
        [-tls-trust-material <file>]               name a PKCS#7 file to use as a source of trusted certificates

    ZFP connections over leased lines (requires TLS options):
        [-zfp-over-leased-line <port>]  enable ZFP connections over leased lines on the specified port (8194 or 8196)
            (When this option is enabled, '-ip' and '-p' arguments will be ignored.)
Press ENTER to quit");
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
                AuthOptionUserApp,
                0,
                value,
                0,
                AuthOptionUserApp.Length,
                ignoreCase: true) == 0)
            {
                string appName = value.Substring(AuthOptionUserApp.Length);
                this.authOptions = new AuthOptions(
                    AuthUser.CreateWithLogonName(),
                    new AuthApplication(appName));
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
            else if (string.Compare(
                AuthOptionManual,
                0,
                value,
                0,
                AuthOptionManual.Length,
                ignoreCase: true) == 0)
            {
                string[] parms = value.Substring(AuthOptionManual.Length).Split(',');
                if (parms.Length != 3)
                {
                    return false;
                }

                string appName = parms[0];
                string ip = parms[1];
                string userId = parms[2];
                this.authOptions = new AuthOptions(
                    AuthUser.CreateWithManualOptions(userId, ip),
                    new AuthApplication(appName));
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

            try
            {
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
                    if (string.Compare("-s", option, true) == 0)
                    {
                        this.service = value;
                    }
                    else if (string.Compare("-ip", option, true) == 0)
                    {
                        this.hosts.Add(value);
                    }
                    else if (string.Compare("-p", option, true) == 0)
                    {
                        this.port = int.Parse(value);
                    }
                    else if (string.Compare("-me", option, true) == 0)
                    {
                        this.maxEvents = int.Parse(value);
                    }
                    else if (string.Compare("-t", option, true) == 0)
                    {
                        this.topics.Add(value);
                    }
                    else if (string.Compare("-f", option, true) == 0)
                    {
                        this.fields.Add(value);
                    }
                    else if (string.Compare("-o", option, true) == 0)
                    {
                        this.options.Add(value);
                    }
                    else if (string.Compare("-auth", option, true) == 0)
                    {
                        if (!this.ParseAuthOptions(value))
                        {
                            PrintUsage();
                            return false;
                        }
                    }
                    else if (string.Compare("-tls-client-credentials", option, true) == 0)
                    {
                        clientCredentials = value;
                    }
                    else if (string.Compare("-tls-client-credentials-password", option, true) == 0)
                    {
                        clientCredentialsPassword = value;
                    }
                    else if (string.Compare("-tls-trust-material", option, true) == 0)
                    {
                        trustMaterial = value;
                    }
                    else if (string.Compare("-zfp-over-leased-line", option, true) == 0) {
                        this.zfpOverLeasedLine = true;
                        if (!TryGetRemote(value, out this.remote)) {
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
            }
            catch (Exception)
            {
                PrintUsage();
                return false;
            }

            if (this.hosts.Count == 0)
            {
                this.hosts.Add("localhost");
            }
            if (this.topics.Count == 0)
            {
                this.topics.Add(DefaultTicker);
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
            else if (this.zfpOverLeasedLine)
            {
                Console.WriteLine("TLS parameters are required for ZFP connections over a leased line.");
                return false;
            }

            return true;
        }

        private void ProcessSubscriptionResponse(Session session)
        {
            bool done = false;

            int eventCount = 0;
            while (!done)
            {
                Event eventObj = session.NextEvent();
                foreach (Message msg in eventObj)
                {
                    Console.WriteLine(msg);

                    if (eventObj.Type == EventType.SUBSCRIPTION_STATUS)
                    {
                        if (msg.MessageType.Equals(Names.SubscriptionFailure) ||
                            msg.MessageType.Equals(Names.SubscriptionTerminated))
                        {

                            string error = msg.GetElement("reason")
                                .GetElementAsString("description");
                            Console.Error.WriteLine($"Subscription failed: {error}");
                            PrintContactSupportMessage(msg);
                        }
                    }
                    else if (eventObj.Type == EventType.SUBSCRIPTION_DATA)
                    {
                        if (msg.RecapType == Message.Recap.SOLICITED)
                        {
                            if (msg.RequestId != null)
                            {
                                // An init paint tick can have an associated
                                // RequestId that is used to identify the
                                // source of the data and can be used when
                                // contacting support.
                                Console.WriteLine(
                                    $"Received init paint with RequestId {msg.RequestId}");
                            }
                        }
                    }
                    else
                    {
                        // SESSION_STATUS events can happen at any time and
                        // should be handled as the session can be terminated,
                        // e.g. session identity can be revoked at a later
                        // time, which terminates the session.
                        done = ProcessGenericMessage(eventObj.Type, msg);
                    }
                }

                if (eventObj.Type == EventType.SUBSCRIPTION_DATA)
                {
                    if (++eventCount >= this.maxEvents)
                    {
                        break;
                    }
                }
            }
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

        public static void Main(string[] args)
        {
            var example = new LocalMktdataSubscriptionExample();
            try
            {
                example.Run(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.ReadLine();
        }
    }

}
