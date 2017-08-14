using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using Mono.Reflection;

namespace Focks.IL
{
    internal class MethodDisassembler
    {
        private MethodBase _method;

        public MethodDisassembler(MethodBase method)
        {
            _method = method;
        }


        public List<MethodBase> GetMethodDependencies()
        {
            IList<Instruction> instructions = _method.GetInstructions();
            var methodDependencies = instructions
                .Where(i => (i.Operand as MethodInfo) != null || (i.Operand as ConstructorInfo) != null)
                .Select(i => (i.Operand as MethodBase));

            return methodDependencies.ToList();
        }
    }
}