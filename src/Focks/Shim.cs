using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Focks.Helpers;

namespace Focks
{
    public partial class Shim
    {
        private MethodBase _original;
        private Delegate _replacement;

        public MethodBase Original
        {
            get
            {
                return _original;
            }
        }

        public Delegate Replacement
        {
            get
            {
                return _replacement;
            }
        }

        internal Shim(MethodBase original, Delegate replacement)
        {
            _original = original;
            _replacement = replacement;
        }

        public static Shim Replace(Expression<Action> original)
        {
            MethodCallExpression methodCall = original.Body as MethodCallExpression;
            return new Shim(methodCall.Method, null);
        }

        public static Shim Replace<T>(Expression<Func<T>> original)
        {
            return new Shim(ShimHelper.GetMethodFromExpression(original.Body), null);
        }

        private Shim WithImpl(Delegate replacement)
        {
            if (!ShimHelper.SignatureEquals(_original, replacement.Method))
                throw new Exception("Signature mismatch");

            _replacement = replacement;
            return this;
        }
    }
}