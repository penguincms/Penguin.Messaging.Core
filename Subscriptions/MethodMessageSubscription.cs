using System;
using System.Linq;
using System.Reflection;
using System.Text;

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
                    caller = provider?.GetService(this.Action.ReflectedType);

                    if (caller is null)
                    {
                        if (!this.Action.ReflectedType.GetConstructors().Any(c => c.GetParameters().Length == 0))
                        {
                            StringBuilder sb = new StringBuilder();

                            if (provider is null)
                            {
                                _ = sb.Append("There is no registered service provider and ");
                            }
                            else
                            {
                                _ = sb.Append("The provided service provider returns a null instance for the requested type and ");
                            }

                            _ = sb.Append($"no constructor was found that doesn't require parameters for type {this.Action.ReflectedType}");
                        }
                        else
                        {
                            caller = Activator.CreateInstance(this.Action.ReflectedType);
                        }
                    }
                }
            } // We probably dont want to bother figuring out how to
              //Resolve a generic parameter on a static class
            else if (this.Action.ContainsGenericParameters)
            {
                return;
            }

            _ = this.Action.Invoke(caller, objects);
        }

        public override string ToString()
        {
            return $"{this.Action.Name} {string.Join(",", this.Action.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))}";
        }

        #endregion Methods
    }
}