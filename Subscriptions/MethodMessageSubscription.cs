using System;
using System.Linq;
using System.Reflection;

namespace Penguin.Messaging.Core.Subscriptions
{
    internal class MethodMessageSubscription : MessageSubscription
    {
        #region Properties

        public MethodInfo Action { get; set; }

        #endregion Properties

        #region Constructors

        public MethodMessageSubscription(MethodInfo action)
        {
            this.Parameters = action.GetParameters().Select(p => p.ParameterType);
            this.Action = action;
        }

        #endregion Constructors

        #region Methods

        public override void Invoke(object[] objects, IServiceProvider provider)
        {
            object caller = null;

            if (!this.Action.IsStatic)
            {
                //We cant instantiate a generic type
                if (this.Action.ReflectedType.IsGenericType)
                {
                    return;
                }
                else
                {
                    caller = provider?.GetService(this.Action.ReflectedType) ?? Activator.CreateInstance(this.Action.ReflectedType);
                }
            } // We probably dont want to bother figuring out how to
              //Resolve a generic parameter on a static class
            else if (this.Action.ContainsGenericParameters)
            {
                return;
            }

            this.Action.Invoke(caller, objects);
        }

        public override string ToString() => $"{this.Action.Name} {string.Join(",", this.Action.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))}";

        #endregion Methods
    }
}