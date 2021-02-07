/* Copyright 2015. Bloomberg Finance L.P.
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

namespace Bloomberglp.BlpapiExamples.DemoApps
{
    class SnapshotRequestTemplateExample
    {
        private const string AUTH_OPTION_NONE = "none";
        private const string AUTH_OPTION_USER = "user";
        private const string AUTH_OPTION_APP  = "app=";
        private const string AUTH_OPTION_USER_APP = "userapp=";
        private const string AUTH_OPTION_DIR  = "dir=";

        private const string d_defaultHost      = "localhost";
        private const int    d_defaultPort      = 8194;
        private const string d_defaultService   = "//viper/mktdata";
        private const int    d_defaultMaxEvents = int.MaxValue;

        private string              d_service = d_defaultService;
        private List<string>        d_hosts = new List<string>();
        private int                 d_port = d_defaultPort;
        private int                 d_maxEvents = d_defaultMaxEvents;

        private AuthOptions d_authOptions = new AuthOptions(AuthUser.CreateWithLogonName());
        private List<string>        d_topics = new List<string>();
        private List<string>        d_fields = new List<string>();
        private List<string>        d_options = new List<string>();

        private void PrintUsage() {
            Console.WriteLine(@"
Create a snapshot request template and send a request using the request template.
Usage:
    [-ip   <ipAddress>]  server name or IP (default: localhost)
    [-p    <tcpPort>]    server port (default: 8194)
    [-s    <service>]    service name (default: //viper/mktdata)
    [-t    <topic>]      topic to subscribe to (default: ""/ticker/IBM Equity"")
    [-f    <field>]      field to subscribe to (default: empty)
    [-o    <option>]     subscription options (default: empty)
    [-me   <maxEvents>]  stop after this many events (default: int.MaxValue)
    [-auth <option>]     authentication option: user|none|app=<app>|userapp=<app>|dir=<property> (default: user)
        none            applicable to Desktop API product that requires
                        Bloomberg Professional service to be installed locally.
        user            as a user using OS logon information
        dir=<property>  as a user using directory services
        app=<app>       as the specified application
        userapp=<app>   as user and application using logon information for the user");
        }

        private bool ParseCommandLine(string[] args) {
            for (int i = 0; i < args.Length; ++i) {
                if (string.Compare("-s", args[i], true) == 0
                        && i + 1 < args.Length) {
                    d_service = args[++i];
                } else if (string.Compare("-t", args[i], true) == 0
                        && i + 1 < args.Length) {
                    d_topics.Add(args[++i]);
                } else if (string.Compare("-f", args[i], true) == 0
                        && i + 1 < args.Length) {
                    d_fields.Add(args[++i]);
                } else if (string.Compare("-o", args[i], true) == 0
                        && i + 1 < args.Length) {
                    d_options.Add(args[++i]);
                } else if (string.Compare("-ip", args[i], true) == 0
                        && i + 1 < args.Length) {
                    d_hosts.Add(args[++i]);
                } else if (string.Compare("-p", args[i], true) == 0
                        && i + 1 < args.Length) {
                    d_port = int.Parse(args[++i]);
                } else if (string.Compare("-me", args[i], true) == 0
                        && i + 1 < args.Length) {
                    d_maxEvents = int.Parse(args[++i]);
                } else if (string.Compare("-auth", args[i], true) == 0
                        && i + 1 < args.Length) {
                    ++i;
                    if (string.Compare(AUTH_OPTION_NONE, args[i], true) == 0) {
                        d_authOptions = new AuthOptions();
                    } else if (string.Compare(AUTH_OPTION_USER,
                                              args[i],
                                              true) == 0) {
                        d_authOptions = new AuthOptions(AuthUser.CreateWithLogonName());
                    } else if (string.Compare(AUTH_OPTION_APP,
                                              0,
                                              args[i], 0,
                                              AUTH_OPTION_APP.Length,
                                              true) == 0) {
                        string appName = args[i].Substring(AUTH_OPTION_APP.Length);
                        d_authOptions = new AuthOptions(new AuthApplication(appName));
                    } else if (string.Compare(AUTH_OPTION_DIR,
                                              0,
                                              args[i],
                                              0,
                                              AUTH_OPTION_DIR.Length,
                                              true) == 0) {
                        string dir = args[i].Substring(AUTH_OPTION_DIR.Length);
                        d_authOptions = new AuthOptions(
                            AuthUser.CreateWithActiveDirectoryProperty(dir));
                    } else if (string.Compare(AUTH_OPTION_USER_APP,
                                              0,
                                              args[i],
                                              0,
                                              AUTH_OPTION_USER_APP.Length,
                                              true) == 0) {
                        string appName = args[i].Substring(AUTH_OPTION_APP.Length);
                        d_authOptions = new AuthOptions(
                            AuthUser.CreateWithLogonName(),
                            new AuthApplication(appName));

                    } else {
                        PrintUsage();
                        return false;
                    }
                } else {
                    PrintUsage();
                    return false;
                }
            }

            if (d_hosts.Count == 0) {
                d_hosts.Add(d_defaultHost);
            }
            if (d_topics.Count == 0) {
                d_topics.Add("/ticker/IBM Equity");
            }

            return true;
        }

        void PrintMessage(Event eventObj) {
            foreach (Message msg in eventObj) {
                Console.WriteLine(msg);
            }
        }

        private void Run(string[] args) {
            if (!ParseCommandLine(args))
                return;

            SessionOptions.ServerAddress[] servers
                = new SessionOptions.ServerAddress[d_hosts.Count];

            for (int i = 0; i < d_hosts.Count; ++i) {
                servers[i]
                    = new SessionOptions.ServerAddress(d_hosts[i], d_port);
            }

            SessionOptions sessionOptions = new SessionOptions();
            sessionOptions.ServerAddresses = servers;
            sessionOptions.DefaultSubscriptionService = d_service;
            sessionOptions.DefaultTopicPrefix = "ticker";
                // normally defaults to "ticker"
            sessionOptions.SetSessionIdentityOptions(d_authOptions);
            sessionOptions.AutoRestartOnDisconnection = true;
            sessionOptions.NumStartAttempts = servers.Length;

            Console.WriteLine("Connecting to");
            foreach (SessionOptions.ServerAddress server in
                sessionOptions.ServerAddresses) {
                Console.WriteLine(" " + server);
            }
            Console.WriteLine();

            using (Session session = new Session(sessionOptions)) {
                if (!session.Start()) {
                    for (;;) {
                        Event e = session.TryNextEvent();
                        if (e == null)
                            break;
                        PrintMessage(e);
                    }
                    Console.Error.WriteLine("Failed to start session.");
                    return;
                }

                string fieldsString = "?fields=";
                for(int iField = 0; iField < d_fields.Count; ++iField) {
                    if(0 != iField) {
                        fieldsString += ",";
                    }
                    fieldsString += d_fields[iField];
                }

                // NOTE: resources used by a snapshot request template are
                // released only when 'RequestTemplateTerminated' message
                // is received or when the session is destroyed.  In order
                // to release resources when request template is not needed
                // anymore, user should call the 'Session.cancel' and pass
                // the correlation id used when creating the request template,
                // or call 'RequestTemplate.close'. If the 'Session.cancel'
                // is used, all outstanding requests are canceled and the
                // underlying subscription is closed immediately. If the
                // handle is closed with the 'RequestTemplate.close', the
                // underlying subscription is closed only when all outstanding
                // requests are served.
                Console.WriteLine("Creating snapshot request templates\n");
                List<RequestTemplate> snapshots = new List<RequestTemplate>();
                for (int iTopic = 0; iTopic < d_topics.Count; ++iTopic) {
                    string subscriptionString
                            = d_service + d_topics[iTopic] + fieldsString;
                    RequestTemplate requestTemplate
                            = session.createSnapshotRequestTemplate(
                                    subscriptionString,
                                    new CorrelationID(d_topics[iTopic]));
                    snapshots.Add(requestTemplate);
                }

                int eventCount = 0;
                while (true) {
                    Event eventObj = session.NextEvent(1000);
                    foreach (Message msg in eventObj) {
                        if (eventObj.Type == Event.EventType.RESPONSE ||
                            eventObj.Type == Event.EventType.PARTIAL_RESPONSE) {
                            long iTopic = msg.CorrelationID.Value;
                            string topic = d_topics[(int)iTopic];
                            Console.WriteLine(topic + " - SNAPSHOT - ");
                        }
                        Console.WriteLine(msg);
                    }
                    if (eventObj.Type == Event.EventType.RESPONSE) {
                        if (++ eventCount >= d_maxEvents) {
                            break;
                        }
                    }
                    if (eventObj.Type == Event.EventType.TIMEOUT) {
                        Console.WriteLine(
                                  "Sending request using the request templates\n");
                        for (int iTopic = 0; iTopic < snapshots.Count; ++iTopic) {
                            session.SendRequest(snapshots[iTopic],
                                                new CorrelationID(iTopic));
                        }
                    }
                }
            }
        }

        public static void Main(string[] args) {
            Console.WriteLine("SnapshotRequestTemplateExample");
            SnapshotRequestTemplateExample example
                    = new SnapshotRequestTemplateExample();
            try {
                example.Run(args);
            } catch (System.IO.IOException e) {
                Console.Error.WriteLine(e.StackTrace);
            } catch (System.Threading.ThreadInterruptedException e) {
                Console.Error.WriteLine(e.StackTrace);
            }
        }
    }
}
