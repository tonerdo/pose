using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Pose.Extensions;

namespace Pose.IL
{
    internal class State
    {
        public Stack<Expression> Stack { get; private set; }
        public List<Expression> Body { get; private set; }
        public ParameterExpression[] Arguments { get; private set; }
        public ParameterExpression[] Variables { get; private set; }

        public State(MethodBase method)
        {
            Stack = new Stack<Expression>();
            Body = new List<Expression>();

            List<Type> parameterTypes = new List<Type>();
            if (!method.IsStatic)
            {
                if (method.IsForValueType())
                    parameterTypes.Add(method.DeclaringType.MakeByRefType());
                else
                    parameterTypes.Add(method.DeclaringType);
            }

            parameterTypes.AddRange(method.GetParameters().Select(p => p.ParameterType));
            Arguments = parameterTypes.Select(p => Expression.Parameter(p)).ToArray();
            Variables = method.GetMethodBody().LocalVariables.Select(v => Expression.Variable(v.LocalType)).ToArray();
        }
    }
}