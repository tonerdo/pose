using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Focks.Extensions;

namespace Focks.Helpers
{
    internal static class ShimHelper
    {
        public static MethodBase GetMethodFromExpression(Expression expression)
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

        public static bool ValidateShimMethodSignature(Shim shim)
        {
            MethodBase methodBase = shim.Original;
            MethodInfo methodInfo = shim.Replacement.Method;

            List<Type> parameterTypes = new List<Type>();
            if (!methodBase.IsStatic && !methodBase.IsConstructor)
            {
                if (methodBase.IsForValueType())
                    parameterTypes.Add(methodBase.DeclaringType.MakeByRefType());
                else
                    parameterTypes.Add(methodBase.DeclaringType);
            }

            parameterTypes.AddRange(methodBase.GetParameters().Select(p => p.ParameterType));

            string validSignature = string.Format("{0} ({1})",
                methodBase.IsConstructor ? typeof(void) : (methodBase as MethodInfo).ReturnType,
                string.Join<Type>(", ", parameterTypes));

            string shimSignature = string.Format("{0} ({1})",
                methodInfo.ReturnType,
                string.Join<Type>(", ", methodInfo.GetParameters().Select(p => p.ParameterType)));

            return shimSignature == validSignature;
        }
    }
}