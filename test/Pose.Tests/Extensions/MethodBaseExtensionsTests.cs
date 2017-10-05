using System;
using System.Reflection;

using Pose.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pose.Tests
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
