using System;
using System.Linq.Expressions;
using System.Reflection;

using Pose.Helpers;

namespace Pose
{
    public partial class Shim
    {
        private Type _baseType;
        private bool _setter;

        internal MethodBase Original { get; }

        internal Delegate Replacement { get; private set; }

        internal Object Instance { get; }

        internal Type Type { get; }

        private Shim(MethodBase original, object instanceOrType)
        {
            Original = original ?? throw new ArgumentNullException(nameof(original));
            if (instanceOrType is Type type)
                Type = type;
            else
                Instance = instanceOrType;
        }

        public static Shim Replace(Expression<Action> expression, bool setter = false)
            => ReplaceImpl(expression, setter, null);

        public static Shim Replace<T>(Expression<Func<T>> expression, bool setter = false)
            => ReplaceImpl(expression, setter, typeof(T));

        private static Shim ReplaceImpl<T>(Expression<T> expression, bool setter, Type baseType)
        {
            MethodBase methodBase = ShimHelper.GetMethodFromExpression(expression.Body, setter, out object instance);
            return new Shim(methodBase, instance) { _setter = setter, _baseType = baseType};
        }

        private Shim WithImpl(Delegate replacement)
        {
            Replacement = replacement;
            ShimHelper.ValidateReplacementMethodSignature(this.Original, this.Replacement.Method, Instance?.GetType() ?? Type, _setter, _baseType);
            return this;
        }
    }
}