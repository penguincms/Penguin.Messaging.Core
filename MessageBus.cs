using Penguin.Debugging;
using Penguin.Messaging.Abstractions.Interfaces;
using Penguin.Messaging.Abstractions.Messages;
using Penguin.Messaging.Core.Subscriptions;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace Penguin.Messaging.Core
{
    /// <summary>
    /// The core message bus class for pub/sub access
    /// </summary>
    public partial class MessageBus
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of the message bus using the specified service provider. Pass in null to use internal resolver
        /// </summary>
        /// <param name="serviceProvider">The service provider to use for resolving subscription targets</param>
        /// <param name="attemptSubscribe">If true the message bus will attempt to find all relevant subscriptions and register them</param>
        public MessageBus(IServiceProvider serviceProvider, bool attemptSubscribe = false)
        {
            this.ServiceProvider = serviceProvider;

            if (attemptSubscribe)
            {
                SubscribeAll();
            }
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Searches the specified type for any methods that have a parameter accepting a Message type, and sets them as subscription targets
        /// </summary>
        /// <param name="messageHandler">The type to check for methods</param>
        public static void Subscribe(Type messageHandler)
        {
            if (messageHandler is null)
            {
                throw new ArgumentNullException(nameof(messageHandler));
            }

            if (SubscribedTypes.Contains(messageHandler)) { return; }

            foreach (MethodInfo method in messageHandler.GetMethods())
            {
                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Any(
                        p => typeof(IMessage).IsAssignableFrom(p.ParameterType)))
                {
                    Subscriptions.Add(new MethodMessageSubscription(method));
                }
            }
            SubscribedTypes.Add(messageHandler);
        }

        /// <summary>
        /// Adds a new subscription using the MethodInfo of the receiver
        /// </summary>
        /// <param name="method">The method to subscribe</param>
        public static void Subscribe(MethodInfo method)
        {
            if (method is null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            ParameterInfo[] parameters = method.GetParameters();

            if (parameters.Length > 0)
            {
                Subscriptions.Add(new MethodMessageSubscription(method));
            }
        }

        /// <summary>
        /// Adds a new subscription using an Action as the target
        /// </summary>
        /// <typeparam name="T">The message type to accept</typeparam>
        /// <param name="action">The action to process</param>
        public static void Subscribe<T>(Action<T> action) where T : Message
        {
            Subscriptions.Add(new ActionMethodSubscription<T>(action));
        }

        /// <summary>
        /// Searches an IEnumerable of Types and subscribes all methods containing parameters matching the Message type
        /// </summary>
        /// <param name="toSubscribe">The list of Types to search</param>
        public static void SubscribeAll(IEnumerable<Type> toSubscribe)
        {
            if (toSubscribe is null)
            {
                throw new ArgumentNullException(nameof(toSubscribe));
            }

            foreach (Type t in toSubscribe)
            {
                if (t.IsAbstract)
                {
                    continue;
                }

                Subscribe(t);
            }
        }

        /// <summary>
        /// Searches All currently loaded assemblies for types with the IMessageHandler interface, and
        /// subscribes all methods beneath them
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public static void SubscribeAll()
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (SubscribedAssemblies.Contains(a))
                {
                    continue;
                }
                else
                {
                    SubscribedAssemblies.Add(a);
                }

                try
                {
                    foreach (Type t in a.GetTypes())
                    {
                        if (typeof(IMessageHandler).IsAssignableFrom(t))
                        {
                            MessageBus.Subscribe(t);
                        }
                    }
                }
                catch (Exception ex)
                {
                    StaticLogger.Log(ex.Message, StaticLogger.LoggingLevel.Call);
                    StaticLogger.Log(ex.StackTrace, StaticLogger.LoggingLevel.Call);
                }
            }
        }

        /// <summary>
        /// Sends a message that will be received by any subscriptions that can handle the parameters in the order sent in
        /// </summary>
        /// <param name="objects">The parameters to send to the subscriptions</param>
        public void Send(params object[] objects)
        {
            for (int i = 0; i < Subscriptions.Count; i++)
            {
                IMessageSubscription subscription = Subscriptions[i];
                if (MessageMatch(subscription, objects))
                {
                    subscription.Invoke(objects, this.ServiceProvider);
                }
            }
        }

        #endregion Methods

        #region Fields

        private static readonly List<IMessageSubscription> Subscriptions = new List<IMessageSubscription>();

        #endregion Fields

        #region Properties

        private static HashSet<Assembly> SubscribedAssemblies { get; set; } = new HashSet<Assembly>();
        private static HashSet<Type> SubscribedTypes { get; set; } = new HashSet<Type>();
        private IServiceProvider ServiceProvider { get; set; }

        #endregion Properties

        private static bool MessageMatch(IMessageSubscription subscription, params object[] messageArgs)
        {
            if (subscription.Parameters.Count() != messageArgs.Length)
            {
                return false;
            }

            for (int i = 0; i < subscription.Parameters.Count(); i++)
            {
                Type argsType = messageArgs[i].GetType();

                if (!subscription.Parameters.ElementAt(i).IsAssignableFrom(argsType))
                {
                    return false;
                }
            }

            return true;
        }
    }
}