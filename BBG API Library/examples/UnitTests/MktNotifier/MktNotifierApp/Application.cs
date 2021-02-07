/* Copyright 2020. Bloomberg Finance L.P.
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

namespace Bloomberglp.BlpapiExamples.UnitTests.MktNotifier.MktNotifierApp
{
    using System;
    using Bloomberglp.Blpapi;
    using Bloomberglp.BlpapiExamples.UnitTests.MktNotifier.MktNotifierApp.Implementation;

    /// <summary>
    /// A sample application that subscribes certain topics, reprocesses
    /// the incoming subscription data and then notifies about the new data.
    /// </summary>
    public class Application
    {
        private readonly ISession session;
        private readonly IAuthorizer authorizer;
        private readonly ISubscriber subscriber;
        private readonly AppOptions appOptions;

        public Application(
            ISession session,
            IAuthorizer authorizer,
            ISubscriber subscriber,
            AppOptions appOptions)
        {
            this.session = session;
            this.authorizer = authorizer;
            this.subscriber = subscriber;
            this.appOptions = appOptions;
        }

        public void Run()
        {
            if (!this.session.Start())
            {
                Console.Error.WriteLine("Failed to start session.");
                return;
            }

            Identity identity = this.authorizer.Authorize(
                this.appOptions.AuthService,
                this.appOptions.AuthOptions);

            this.subscriber.Subscribe(
                this.appOptions.Service,
                this.appOptions.Topics,
                this.appOptions.Fields,
                this.appOptions.Options,
                identity);
        }

        public static void Main(string[] args)
        {
            AppOptions appOptions = null;
            try
            {
                appOptions = AppOptions.ParseCommandLine(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                AppOptions.PrintUsage();
                return;
            }

            try
            {
                var sessionOptions = new SessionOptions();
                var serverAddresses = new SessionOptions.ServerAddress[
                    appOptions.Hosts.Count];
                for (int i = 0; i < serverAddresses.Length; ++i)
                {
                    serverAddresses[i] = new SessionOptions.ServerAddress(
                        appOptions.Hosts[i],
                        appOptions.Port);
                }

                sessionOptions.ServerAddresses = serverAddresses;
                sessionOptions.AuthenticationOptions = appOptions.AuthOptions;

                INotifier notifier = new Notifier();
                IComputeEngine computeEngine = new ComputeEngine();
                var eventHandler = new EventProcessor(
                    notifier,
                    computeEngine);

                using (var session = new Session(sessionOptions, eventHandler.ProcessEvent))
                {
                    ITokenGenerator tokenGenerator = new TokenGenerator(session);
                    IAuthorizer authorizer = new Authorizer(session, tokenGenerator);
                    ISubscriber subscriber = new Subscriber(session);

                    var app = new Application(
                        session,
                        authorizer,
                        subscriber,
                        appOptions);
                    app.Run();
                }

                Console.WriteLine("Press ENTER to quit");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }
    }
}
