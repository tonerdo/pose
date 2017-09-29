using System;
using System.Linq.Expressions;
using System.Reflection;

using Focks.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static System.Console;

namespace Focks.Tests
{
    [TestClass]
    public class ShimHelperTests
    {
        [TestMethod]
        public void TestGetMethodFromExpressionThrowNotImplemented()
        {
            Expression<Func<bool>> expr = () => true;
            Expression<Func<DateTime>> expr1 = () => DateTime.MaxValue;

            Assert.ThrowsException<NotImplementedException>(
                () => ShimHelper.GetMethodFromExpression(expr.Body));

            Assert.ThrowsException<NotImplementedException>(
                () => ShimHelper.GetMethodFromExpression(expr1.Body));
        }

        [TestMethod]
        public void TestGetMethodFromExpressionValid()
        {
            Expression<Func<DateTime>> expr = () => DateTime.Now;
            Expression<Func<string>> expr1 = () => ReadLine();

            Assert.AreEqual<MethodBase>(typeof(DateTime).GetMethod("get_Now"),
                ShimHelper.GetMethodFromExpression(expr.Body));

            Assert.AreEqual<MethodBase>(typeof(Console).GetMethod("ReadLine"),
                ShimHelper.GetMethodFromExpression(expr1.Body));
        }

        [TestMethod]
        public void TestValidateReplacementMethodSignatureInValid()
        {
            MethodBase original = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) });
            MethodInfo replacement = new Action(() => {}).Method;

            Assert.AreEqual<bool>(false, ShimHelper.ValidateReplacementMethodSignature(original, replacement));

            original = typeof(DateTime).GetMethod("Add");
            Assert.AreEqual<bool>(false, ShimHelper.ValidateReplacementMethodSignature(original, replacement));
        }

        [TestMethod]
        public void TestValidateReplacementMethodSignatureValid()
        {
            MethodBase original = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) });
            MethodInfo replacement = new Action<string>((s) => {}).Method;

            Assert.AreEqual<bool>(true, ShimHelper.ValidateReplacementMethodSignature(original, replacement));

            original = typeof(string).GetMethod("Contains");
            replacement = new Func<string, string, bool>((d, t) => true).Method;
            Assert.AreEqual<bool>(true, ShimHelper.ValidateReplacementMethodSignature(original, replacement));
        }
    }
}
