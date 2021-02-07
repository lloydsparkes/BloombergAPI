/* Copyright 2020. Bloomberg Finance L.P.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions: The above
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

namespace Bloomberglp.BlpapiExamples.UnitTests.MktNotifier.MktNotifierApp.Implementation
{
    using System;
    using Bloomberglp.Blpapi;
    using EventType = Bloomberglp.Blpapi.Event.EventType;

    internal class Authorizer : IAuthorizer
    {
        private const int WaitTimeMs = 10000; // 10 seconds
        private static readonly Name AuthorizationSuccess = Name.GetName("AuthorizationSuccess");
        internal static readonly Name Token = Name.GetName("token");

        private readonly ISession session;
        private readonly ITokenGenerator tokenGenerator;

        public Authorizer(ISession session, ITokenGenerator tokenGenerator)
        {
            this.session = session;
            this.tokenGenerator = tokenGenerator;
        }

        public Identity Authorize(
            string authService,
            string authOptions,
            IEventQueue eventQueue = null)
        {
            if (string.IsNullOrWhiteSpace(authOptions))
            {
                return null;
            }

            this.session.OpenService(authService);
            Service service = this.session.GetService(authService);
            return this.Authorize(service, eventQueue);
        }

        private Identity Authorize(
            Service authService,
            IEventQueue eventQueue = null)
        {
            string token = this.tokenGenerator.Generate();
            if (token == null)
            {
                throw new Exception("Failed to generate token.");
            }

            Request authRequest = authService.CreateAuthorizationRequest();
            authRequest.Set(Authorizer.Token, token);
            IEventQueue authEventQueue = eventQueue ?? new EventQueue();
            Identity identity = this.session.CreateIdentity();
            this.session.SendAuthorizationRequest(
                authRequest,
                identity,
                authEventQueue,
                new CorrelationID("auth"));

            while (true)
            {
                Event @event = authEventQueue.NextEvent(Authorizer.WaitTimeMs);

                EventType eventType = @event.Type;
                if (eventType == EventType.RESPONSE ||
                    eventType == EventType.REQUEST_STATUS ||
                    eventType == EventType.PARTIAL_RESPONSE ||
                    eventType == EventType.AUTHORIZATION_STATUS)
                {
                    foreach (Message msg in @event)
                    {
                        if (Authorizer.AuthorizationSuccess.Equals(msg.MessageType))
                        {
                            return identity;
                        }

                        throw new Exception($"Failed to authorize: {msg}");
                    }
                }

                if (eventType == EventType.TIMEOUT)
                {
                    throw new Exception("Authorization timed out");
                }
            }
        }
    }
}
