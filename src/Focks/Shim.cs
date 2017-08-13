using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Focks
{
    public class Shim
    {
        protected MethodBase _original;
        protected Delegate _replacement;

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
            return new Shim(GetMethodFromExpression(original.Body), null);
        }

        public Shim With(Delegate replacement)
        {
            if (!SignatureEquals(_original, replacement.Method))
                throw new Exception("Signature mismatch");

            _replacement = replacement;
            return this;
        }

        private static MethodBase GetMethodFromExpression(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.MemberAccess:
                    MemberExpression memberExpression = expression as MemberExpression;
                    MemberInfo memberInfo = memberExpression.Member;
                    if (memberInfo.MemberType == MemberTypes.Property)
                    {
                        PropertyInfo propertyInfo = memberInfo as PropertyInfo;
                        return propertyInfo.GetGetMethod();
                    }
                    else
                        throw new NotImplementedException("Unsupported expression");
                case ExpressionType.Call:
                    MethodCallExpression methodCall = expression as MethodCallExpression;
                    return methodCall.Method;
                default:
                    throw new NotImplementedException("Unsupported expression");
            }
        }

        private bool SignatureEquals(MethodBase m1, MethodInfo m2)
        {
            string signature1 = string.Format("{0}({1})",
                m1.IsConstructor ? m1.DeclaringType : (m1 as MethodInfo).ReturnType,
                string.Join<Type>(",", m1.GetParameters().Select(p => p.ParameterType)));

            string signature2 = string.Format("{0}({1})",
                m2.ReturnType,
                string.Join<Type>(",", m2.GetParameters().Select(p => p.ParameterType)));

            return signature1 == signature2;
        }
    }
}
