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
        public static MethodBase GetMethodFromExpression(Expression expression, out Object instance)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.MemberAccess:
                    MemberExpression memberExpression = expression as MemberExpression;
                    MemberInfo memberInfo = memberExpression.Member;
                    if (memberInfo.MemberType == MemberTypes.Property)
                    {
                        PropertyInfo propertyInfo = memberInfo as PropertyInfo;
                        instance = GetObjectFromExpression(memberExpression.Expression);
                        return propertyInfo.GetGetMethod();
                    }
                    else
                        throw new NotImplementedException("Unsupported expression");
                case ExpressionType.Call:
                    MethodCallExpression methodCall = expression as MethodCallExpression;
                    instance = GetObjectFromExpression(methodCall.Object);
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

        public static object GetObjectFromExpression(Expression expression)
        {
            if (!(expression is MemberExpression))
                return null;

            MemberExpression memberExpression = expression as MemberExpression;
            ConstantExpression constantExpression = memberExpression.Expression as ConstantExpression;
            if (memberExpression.Member.MemberType == MemberTypes.Field)
            {
                FieldInfo fieldInfo = (memberExpression.Member as FieldInfo);
                var obj = fieldInfo.IsStatic ? null : constantExpression.Value;
                return (memberExpression.Member as FieldInfo).GetValue(obj);
            }
            else if (memberExpression.Member.MemberType == MemberTypes.Property)
            {
                PropertyInfo propertyInfo = (memberExpression.Member as PropertyInfo);
                var obj = propertyInfo.GetMethod.IsStatic ? null : constantExpression.Value;
                return (memberExpression.Member as PropertyInfo).GetValue(obj);
            }
            return null;
        }
    }
}