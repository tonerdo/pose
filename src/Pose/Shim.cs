using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Pose.Helpers;

namespace Pose
{
    public partial class Shim
    {
        private MethodBase _original;
        private Delegate _replacement;
        private Object _instance;

        internal MethodBase Original
        {
            get
            {
                return _original;
            }
        }

        internal Delegate Replacement
        {
            get
            {
                return _replacement;
            }
        }

        internal Object Instance
        {
            get
            {
                return _instance;
            }
        }

        private Shim(MethodBase original, Delegate replacement)
        {
            _original = original;
            _replacement = replacement;
        }

        public static Shim Replace(Expression<Action> expression)
        {
            return new Shim(ShimHelper.GetMethodFromExpression(expression.Body, out object instance), null)
            {
                _instance = instance
            };
        }

        public static Shim Replace<T>(Expression<Func<T>> expression)
        {
            return new Shim(ShimHelper.GetMethodFromExpression(expression.Body, out object instance), null)
            {
                _instance = instance
            };
        }

        private Shim WithImpl(Delegate replacement)
        {
            _replacement = replacement;
            if (!ShimHelper.ValidateReplacementMethodSignature(this._original, this._replacement.Method))
                throw new Exception("Invalid shim method signature");

            return this;
        }
    }
}