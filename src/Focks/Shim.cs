using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Focks.Helpers;

namespace Focks
{
    public class Shim
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

        public Shim With(Action replacement) => WithImpl(replacement);

        public Shim With<T>(Action<T> replacement) => WithImpl(replacement);

        public Shim With<T1, T2>(Action<T1, T2> replacement) => WithImpl(replacement);

        public Shim With<T1, T2, T3>(Action<T1, T2, T3> replacement) => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4>(Action<T1, T2, T3, T4> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> replacement)
            => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> replacement)
            => WithImpl(replacement);
        
        public Shim With<TResult>(Func<TResult> replacement) => WithImpl(replacement);

        public Shim With<T1, TResult>(Func<T1, TResult> replacement) => WithImpl(replacement);

        public Shim With<T1, T2, TResult>(Func<T1, T2, TResult> replacement) => WithImpl(replacement);

        public Shim With<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> replacement) => WithImpl(replacement);

        public Shim With<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> replacement)
            => WithImpl(replacement);
        
        public Shim With<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> replacement)
            => WithImpl(replacement);
        
        public Shim With<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> replacement)
            => WithImpl(replacement);
        
        public Shim With<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> replacement)
            => WithImpl(replacement);
        
        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> replacement)
            => WithImpl(replacement);
        
        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> replacement)
            => WithImpl(replacement);
        
        public Shim With<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> replacement)
            => WithImpl(replacement);
    }
}