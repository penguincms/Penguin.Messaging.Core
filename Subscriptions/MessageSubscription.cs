using Penguin.Messaging.Abstractions.Interfaces;
using System;
using System.Collections.Generic;

namespace Penguin.Messaging.Core.Subscriptions
{
    internal abstract class MessageSubscription : IMessageSubscription
    {
        #region Properties

        public IEnumerable<Type> Parameters { get; set; }

        #endregion Properties

        #region Methods

        public abstract void Invoke(object[] objects, IServiceProvider provider);

        #endregion Methods
    }
}