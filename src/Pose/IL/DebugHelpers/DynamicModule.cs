using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Pose.IL.DebugHelpers
{
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