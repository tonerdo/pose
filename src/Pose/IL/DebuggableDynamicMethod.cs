using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace Pose.IL
{
    internal class DebuggableDynamicMethod : MethodInfo
    {
        private readonly DynamicMethod m_method;

        private readonly ILGenerator m_ilGenerator;

        private readonly MethodBody m_methodBody;

        public DebuggableDynamicMethod(DynamicMethod method, ILGenerator ilGenerator, MethodBody methodBody)
        {
            m_method = method;
            m_ilGenerator = ilGenerator;
            m_methodBody = methodBody;
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotImplementedException();

        public override MethodAttributes Attributes => MethodAttributes.Static;

        public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();

        public override Module Module => new DynamicModule(m_ilGenerator);

        public override Type DeclaringType => m_method.DeclaringType;

        public override string Name => m_method.Name;

        public override Type ReflectedType => throw new NotImplementedException();

        public override MethodInfo GetBaseDefinition() => throw new NotImplementedException();

        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

        public override Type[] GetGenericArguments() => Array.Empty<Type>();

        public override MethodBody GetMethodBody() => m_methodBody;

        public override MethodImplAttributes GetMethodImplementationFlags() => throw new NotImplementedException();

        public override ParameterInfo[] GetParameters() => m_method.GetParameters();

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
            => throw new NotImplementedException();

        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
    }

    internal class DynamicMethodBody : MethodBody
    {
        private readonly byte[] m_ilBytes;

        private readonly IList<LocalVariableInfo> m_locals;

        public DynamicMethodBody(byte[] ilBytes, IList<LocalVariableInfo> locals)
        {
            m_ilBytes = ilBytes;
            m_locals = locals;
        }

        public override int LocalSignatureMetadataToken => throw new NotImplementedException();

        public override IList<LocalVariableInfo> LocalVariables => m_locals;

        public override int MaxStackSize => throw new NotImplementedException();

        public override bool InitLocals => throw new NotImplementedException();

        public override byte[]? GetILAsByteArray() => m_ilBytes;

        public override IList<ExceptionHandlingClause> ExceptionHandlingClauses => throw new NotImplementedException();
    }

    internal class DynamicModule: Module
    {
        private static FieldInfo s_scopeField;

        private readonly ILGenerator m_ilGenerator;

        public DynamicModule(ILGenerator ilGenerator)
        {
            m_ilGenerator = ilGenerator;

            if (s_scopeField == null)
            {
                s_scopeField = m_ilGenerator.GetType().GetField("m_scope", BindingFlags.Instance | BindingFlags.NonPublic);
            }
        }

        public override string ResolveString(int metadataToken)
        {
            var dynamicScope = s_scopeField.GetValue(m_ilGenerator);
            Debug.Assert(dynamicScope != null);

            return (string)dynamicScope.GetType().GetMethod("GetString", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(dynamicScope, new object[] { metadataToken });
        }

        public override MemberInfo ResolveMember(int metadataToken, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            var dynamicScope = s_scopeField.GetValue(m_ilGenerator);
            Debug.Assert(dynamicScope != null);

            var handle = dynamicScope.GetType().GetMethod("get_Item", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(dynamicScope, new object[] { metadataToken });

            return handle switch
            {
                RuntimeTypeHandle typeHandle => TypeInfo.GetTypeFromHandle(typeHandle).GetTypeInfo(),
                RuntimeMethodHandle methodHandle => MethodBase.GetMethodFromHandle(methodHandle),
                RuntimeFieldHandle fieldHandle => FieldInfo.GetFieldFromHandle(fieldHandle),
                _ => throw new NotSupportedException(handle.ToString()),
            };
        }

        public override byte[] ResolveSignature(int metadataToken)
        {
            var dynamicScope = s_scopeField.GetValue(m_ilGenerator);
            Debug.Assert(dynamicScope != null);

            return (byte[])dynamicScope.GetType().GetMethod("ResolveSignature", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(dynamicScope, new object[] { metadataToken, 0 });
        }
    }
}
