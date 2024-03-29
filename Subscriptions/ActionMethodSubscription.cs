﻿using System;
using System.Collections.Generic;

namespace Penguin.Messaging.Core.Subscriptions
{
    internal class ActionMethodSubscription<T> : MessageSubscription
    {
        #region Constructors

        public ActionMethodSubscription(Action<T> a)
        {
            Action = a;
            Parameters = new List<Type>() { typeof(T) };
        }

        #endregion Constructors

        #region Methods

        public override void Invoke(object[] objects, IServiceProvider service)
        {
            Action.Invoke((T)objects[0]);
        }

        #endregion Methods

        #region Properties

        internal Action<T> Action { get; set; }

        #endregion Properties
    }
}