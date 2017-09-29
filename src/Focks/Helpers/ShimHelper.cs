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

        public static bool ValidateReplacementMethodSignature(MethodBase original, MethodInfo replacement)
        {
            List<Type> parameterTypes = new List<Type>();
            if (!original.IsStatic && !original.IsConstructor)
            {
                if (original.IsForValueType())
                    parameterTypes.Add(original.DeclaringType.MakeByRefType());
                else
                    parameterTypes.Add(original.DeclaringType);
            }

            parameterTypes.AddRange(original.GetParameters().Select(p => p.ParameterType));

            string validSignature = string.Format("{0} ({1})",
                original.IsConstructor ? typeof(void) : (original as MethodInfo).ReturnType,
                string.Join<Type>(", ", parameterTypes));

            string shimSignature = string.Format("{0} ({1})",
                replacement.ReturnType,
                string.Join<Type>(", ", replacement.GetParameters().Select(p => p.ParameterType)));

            return shimSignature == validSignature;
        }
    }
}