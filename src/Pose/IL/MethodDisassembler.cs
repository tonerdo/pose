using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using Mono.Reflection;

namespace Pose.IL
{
    internal class MethodDisassembler
    {
        private MethodBase _method;

        public MethodDisassembler(MethodBase method)
        {
            _method = method;
        }

        public List<Instruction> GetILInstructions()
        {
            return _method.GetInstructions().ToList();
        }

        public List<MethodBase> GetMethodDependencies()
        {
            var methodDependencies = GetILInstructions()
                .Where(i => (i.Operand as MethodInfo) != null || (i.Operand as ConstructorInfo) != null)
                .Select(i => (i.Operand as MethodBase));

            return methodDependencies.ToList();
        }
    }
}