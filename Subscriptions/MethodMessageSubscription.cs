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
            Parameters = action.GetParameters().Select(p => p.ParameterType);
            Action = action;
        }

        #endregion Constructors

        #region Methods

        public override void Invoke(object[] objects, IServiceProvider provider)
        {
            object caller = null;

            if (!Action.IsStatic)
            {
                //We cant instantiate a generic type
                if (Action.ReflectedType.IsGenericType)
                {
                    return;
                }
                else
                {
                    caller = provider?.GetService(Action.ReflectedType);

                    if (caller is null)
                    {
                        if (!Action.ReflectedType.GetConstructors().Any(c => c.GetParameters().Length == 0))
                        {
                            StringBuilder sb = new();

                            _ = provider is null
                                ? sb.Append("There is no registered service provider and ")
                                : sb.Append("The provided service provider returns a null instance for the requested type and ");

                            _ = sb.Append($"no constructor was found that doesn't require parameters for type {Action.ReflectedType}");
                        }
                        else
                        {
                            caller = Activator.CreateInstance(Action.ReflectedType);
                        }
                    }
                }
            } // We probably dont want to bother figuring out how to
              //Resolve a generic parameter on a static class
            else if (Action.ContainsGenericParameters)
            {
                return;
            }

            _ = Action.Invoke(caller, objects);
        }

        public override string ToString()
        {
            return $"{Action.Name} {string.Join(",", Action.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))}";
        }

        #endregion Methods
    }
}