using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Pose.Exceptions;
using Pose.Extensions;

namespace Pose.Helpers
{
    internal static class ShimHelper
    {
        public static MethodBase GetMethodFromExpression(Expression expression, bool setter, out Object instanceOrType)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            switch (expression.NodeType)
            {
                case ExpressionType.MemberAccess:
                    {
                        MemberExpression memberExpression = expression as MemberExpression;
                        MemberInfo memberInfo = memberExpression.Member;
                        if (memberInfo.MemberType == MemberTypes.Property)
                        {
                            PropertyInfo propertyInfo = memberInfo as PropertyInfo;
                            instanceOrType = GetObjectInstanceOrType(memberExpression.Expression);
                            return setter ? propertyInfo.GetSetMethod() : propertyInfo.GetGetMethod();
                        }

                        throw new NotImplementedException("Unsupported expression");
                    }
                case ExpressionType.Call:
                    MethodCallExpression methodCallExpression = expression as MethodCallExpression;
                    instanceOrType = GetObjectInstanceOrType(methodCallExpression.Object);
                    return methodCallExpression.Method;
                case ExpressionType.New:
                    NewExpression newExpression = expression as NewExpression;
                    instanceOrType = null;
                    return newExpression.Constructor;
                default:
                    throw new NotImplementedException("Unsupported expression");
            }
        }

        public static void ValidateReplacementMethodSignature(MethodBase original, MethodInfo replacement, Type type,
            bool setter, Type baseType)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));
            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));

            bool isValueType = original.IsForValueType();
            bool isStatic = original.IsStatic;
            bool isConstructor = original.IsConstructor;
            bool isStaticOrConstructor = isStatic || isConstructor;

            Type vaildReturnType = isConstructor ? original.DeclaringType : (original as MethodInfo).ReturnType;
            vaildReturnType = setter ? typeof(void) : vaildReturnType;
            Type shimReturnType = replacement.ReturnType;

            Type validOwningType = type;
            Type shimOwningType = isStaticOrConstructor
                ? validOwningType : replacement.GetParameters().Select(p => p.ParameterType).FirstOrDefault();

            var validParameterTypes = original.GetParameters().Select(p => p.ParameterType).ToList();
            var shimParameterTypes = replacement.GetParameters()
                                        .Select(p => p.ParameterType)
                                        .Skip(isStaticOrConstructor ? 0 : 1).ToList();

            if (!isConstructor && vaildReturnType != shimReturnType)
                throw new InvalidShimSignatureException("Mismatched return types");

            if (isConstructor)
            {
                var isValidReturnType = CheckTypesForAssignability(baseType, shimReturnType);
                if (!isValidReturnType)
                {
                    throw new InvalidShimSignatureException("Mismatched construction types");
                }
            }

            if (!isStaticOrConstructor)
            {
                if (isValueType && !shimOwningType.IsByRef)
                    throw new InvalidShimSignatureException("ValueType instances must be passed by ref");
            }

            if ((isValueType && !isStaticOrConstructor ? validOwningType.MakeByRefType() : validOwningType) != shimOwningType)
                throw new InvalidShimSignatureException("Mismatched instance types");

            if (validParameterTypes.Count != shimParameterTypes.Count)
                throw new InvalidShimSignatureException("Parameters count do not match");

            for (int i = 0; i < validParameterTypes.Count; i++)
            {
                if (validParameterTypes.ElementAt(i) != shimParameterTypes.ElementAt(i))
                    throw new InvalidShimSignatureException($"Parameter types at {i} do not match");
            }
        }

        public static object GetObjectInstanceOrType(Expression expression)
        {
            object instanceOrType = null;
            switch (expression?.NodeType)
            {
                case ExpressionType.MemberAccess:
                    {
                        MemberExpression memberExpression = expression as MemberExpression;
                        ConstantExpression constantExpression = memberExpression.Expression as ConstantExpression;
                        if (memberExpression.Member.MemberType == MemberTypes.Field)
                        {
                            FieldInfo fieldInfo = (memberExpression.Member as FieldInfo);
                            var obj = fieldInfo.IsStatic ? null : constantExpression.Value;
                            instanceOrType = fieldInfo.GetValue(obj);
                        }
                        else if (memberExpression.Member.MemberType == MemberTypes.Property)
                        {
                            PropertyInfo propertyInfo = (memberExpression.Member as PropertyInfo);
                            var obj = propertyInfo.GetMethod.IsStatic ? null : constantExpression.Value;
                            instanceOrType = propertyInfo.GetValue(obj);
                        }
                        EnsureInstanceNotValueType(instanceOrType);
                        break;
                    }
                case ExpressionType.Call:
                    {
                        MethodCallExpression methodCallExpression = expression as MethodCallExpression;
                        MethodInfo methodInfo = methodCallExpression.Method;
                        instanceOrType = methodInfo.GetGenericArguments().FirstOrDefault();
                        break;
                    }
                default:
                    return null;
            }

            return instanceOrType;
        }

        private static void EnsureInstanceNotValueType(object instance)
        {
            if (instance.GetType().IsSubclassOf(typeof(ValueType)))
                throw new NotSupportedException("You cannot replace methods on specific value type instances");
        }

        private static bool CheckTypesForAssignability(Type baseType, Type typeToCheck)
        {
            var stype = typeToCheck;
            while (stype != typeof(object))
            {
                if (baseType.IsAssignableFrom(stype))
                {
                    return true;
                }

                stype = stype.BaseType;
            }

            stype = baseType.BaseType;
            while (stype != typeof(object))
            {
                if (stype.IsAssignableFrom(typeToCheck))
                {
                    return true;
                }

                stype = stype.BaseType;
            }

            return false;
        }
    }
}