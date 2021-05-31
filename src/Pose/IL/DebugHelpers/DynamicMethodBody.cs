using System;
using System.Collections.Generic;
using System.Reflection;

namespace Pose.IL.DebugHelpers
{
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

        public override byte[] GetILAsByteArray() => m_ilBytes;

        public override IList<ExceptionHandlingClause> ExceptionHandlingClauses => throw new NotImplementedException();
    }
}