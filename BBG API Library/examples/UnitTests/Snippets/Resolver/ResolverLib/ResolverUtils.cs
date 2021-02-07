/* Copyright 2020. Bloomberg Finance L.P.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the ""Software""), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions: The above
 * copyright notice and this permission notice shall be included in all copies
 * or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
 * IN THE SOFTWARE.
 */

namespace Bloomberglp.BlpapiExamples.UnitTests.Snippets.Resolver.ResolverLib
{
    using System;
    using System.Diagnostics;
    using Bloomberglp.Blpapi;

    /// <summary>
    /// A utility class that shows interactions that are particular to resolvers:
    /// i.e. register a resolver service and handle a permission request.
    /// </summary>
    public class ResolverUtils
    {
        public const string NotAuthorized = "NOT_AUTHORIZED";
        public const int AllowedAppId = 1234;

        // 'Name' objects are more expensive to construct than string,
        // but are more efficient on use through the interface. By creating the
        // 'Name' objects in advance we can take advantage of the efficiency
        // without paying the cost of constructing them when needed.
        public static readonly Name PermissionRequest = Name.GetName("PermissionRequest");
        public static readonly Name PermissionResponse = Name.GetName("PermissionResponse");
        public static readonly Name TopicPermissions = Name.GetName("topicPermissions");
        public static readonly Name Result = Name.GetName("result");
        public static readonly Name Reason = Name.GetName("reason");
        public static readonly Name Category = Name.GetName("category");

        private static readonly Name Topic = Name.GetName("topic");
        private static readonly Name Topics = Name.GetName("topics");
        private static readonly Name Source = Name.GetName("source");
        private static readonly Name Subcategory = Name.GetName("subcategory");
        private static readonly Name Description = Name.GetName("description");

        // This can be any string, but it's helpful to provide information on the
        // instance of the resolver that responded to debug failures in production.
        private static readonly string ResolverId = "service:hostname";
        private static readonly string ApplicationId = "applicationId";

        /// <summary>
        /// Demonstrates how to register a resolver service.
        /// <para>
        /// This method assumes the following:
        /// <paramref name="session"/> is already started;
        /// <paramref name="providerIdentity"/> is already authorized if auth is
        /// needed or null if authorization is not required.
        /// </para>
        /// </summary>
        public static bool ResolutionServiceRegistration(
            IProviderSession session,
            Identity providerIdentity,
            string serviceName)
        {
            // Prepare registration options
            var serviceOptions = new ServiceRegistrationOptions();

            const int DummyPriority = 123;
            serviceOptions.ServicePriority = DummyPriority;

            serviceOptions.PartsToRegister =
                ServiceRegistrationOptions.RegistrationParts.PART_SUBSCRIBER_RESOLUTION;

            if (!session.RegisterService(
                serviceName,
                providerIdentity,
                serviceOptions))
            {
                Console.Error.WriteLine($"Failed to register {serviceName}");
                return false;
            }

            return true;
        }

        public static void HandlePermissionRequest(
            IProviderSession session,
            Service service,
            Message request)
        {
            Debug.Assert(
                PermissionRequest.Equals(request.MessageType),
                $"{nameof(request)} must be a {PermissionRequest} ");

            bool isAllowed = request.HasElement(ApplicationId) &&
                request.GetElementAsInt32(ApplicationId) == AllowedAppId;

            Event response = service.CreateResponseEvent(request.CorrelationID);
            var formatter = new EventFormatter(response);
            formatter.AppendResponse(PermissionResponse);

            formatter.PushElement(TopicPermissions);

            Element topics = request.GetElement(Topics);
            for (int i = 0; i < topics.NumValues; ++i)
            {
                formatter.AppendElement();
                formatter.SetElement(Topic, topics.GetValueAsString(i));

                // ALLOWED: 0, DENIED: 1
                formatter.SetElement(Result, isAllowed ? 0 : 1);

                if (!isAllowed)
                {
                    formatter.PushElement(Reason);
                    formatter.SetElement(Source, ResolverId);
                    formatter.SetElement(Category, NotAuthorized);
                    formatter.SetElement(Subcategory, "");
                    formatter.SetElement(
                        Description,
                        $"Only app {AllowedAppId} is allowed");
                    formatter.PopElement();
                }

                formatter.PopElement();
            }

            formatter.PopElement();

            session.SendResponse(response);
        }
    }
}
