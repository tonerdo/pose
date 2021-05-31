using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace Pose.IL.DebugHelpers
{
    internal class BrowsableDynamicMethod : MethodInfo
    {
        private readonly DynamicMethod m_method;

        private readonly MethodBody m_methodBody;

        public BrowsableDynamicMethod(DynamicMethod method, MethodBody methodBody)
        {
            m_method = method;
            m_methodBody = methodBody;
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotImplementedException();

        public override MethodAttributes Attributes => MethodAttributes.Static;

        public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();

        public override Module Module => new DynamicModule(m_method.GetILGenerator());

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
}
