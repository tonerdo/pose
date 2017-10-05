using System;
using System.Linq.Expressions;
using System.Reflection;

using Pose.IL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pose.Tests
{
    [TestClass]
    public class MethodDisassemblerTests
    {
        [TestMethod]
        public void TestGetILInstructions()
        {
            MethodDisassembler methodDisassembler
                = new MethodDisassembler(typeof(Console).GetMethod("Clear"));
            Assert.AreNotEqual(0, methodDisassembler.GetILInstructions().Count);
        }
    }
}