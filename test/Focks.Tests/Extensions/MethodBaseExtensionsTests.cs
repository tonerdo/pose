using System;
using System.Reflection;

using Focks.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Focks.Tests
{
    [TestClass]
    public class MethodBaseExtensionsTests
    {
        [TestMethod]
        public void TestIsForValueType()
        {
            Assert.AreEqual<bool>(true, typeof(DateTime).GetMethod("Add").IsForValueType());
            Assert.AreEqual<bool>(false, typeof(Console).GetMethod("Clear").IsForValueType());
        }
    }
}
