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
            
            Debug.Assert(handle != null);

            if (handle is RuntimeTypeHandle typeHandle)
            {
                return TypeInfo.GetTypeFromHandle(typeHandle).GetTypeInfo();
            }

            if (handle is RuntimeMethodHandle methodHandle)
            {
                return MethodBase.GetMethodFromHandle(methodHandle);
            }

            if (handle.GetType().ToString() == "System.Reflection.Emit.GenericMethodInfo")
            {
                var methodHandleFieldInfo = handle.GetType().GetField("m_methodHandle", BindingFlags.Instance | BindingFlags.NonPublic);
                var typeHandleFieldInfo = handle.GetType().GetField("m_context", BindingFlags.Instance | BindingFlags.NonPublic);
                return MethodBase.GetMethodFromHandle((RuntimeMethodHandle)methodHandleFieldInfo.GetValue(handle), (RuntimeTypeHandle)typeHandleFieldInfo.GetValue(handle));
            }

            if (handle is RuntimeFieldHandle fieldHandle)
            {
                return FieldInfo.GetFieldFromHandle(fieldHandle);
            }

            if (handle.GetType().ToString() == "System.Reflection.Emit.GenericFieldInfo")
            {
                var fieldHandleFieldInfo = handle.GetType().GetField("m_fieldHandle", BindingFlags.Instance | BindingFlags.NonPublic);
                var typeHandleFieldInfo = handle.GetType().GetField("m_context", BindingFlags.Instance | BindingFlags.NonPublic);
                return FieldInfo.GetFieldFromHandle((RuntimeFieldHandle)fieldHandleFieldInfo.GetValue(handle), (RuntimeTypeHandle)typeHandleFieldInfo.GetValue(handle));
            }

            if (handle is DynamicMethod dynamicMethod)
            {
                return dynamicMethod;
            }

            throw new NotSupportedException(handle.ToString());
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