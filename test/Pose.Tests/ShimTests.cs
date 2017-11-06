using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

using Pose.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using static System.Console;

namespace Pose.Tests
{
    [TestClass]
    public class ShimTests
    {
        [TestMethod]
        public void TestReplace()
        {
            Shim shim = Shim.Replace(() => Console.WriteLine(""));

            Assert.AreEqual(typeof(Console).GetMethod("WriteLine", new[] { typeof(string) }), shim.Original);
            Assert.IsNull(shim.Replacement);
        }

        [TestMethod]
        public void TestReplaceWithInstanceVariable()
        {
            ShimTests shimTests = new ShimTests();
            Shim shim = Shim.Replace(() => shimTests.TestReplace());

            Assert.AreEqual(typeof(ShimTests).GetMethod("TestReplace"), shim.Original);
            Assert.AreSame(shimTests, shim.Instance);
            Assert.IsNull(shim.Replacement);
        }

        [TestMethod]
        public void TestShimReplaceWithInvalidSignature()
        {
            ShimTests shimTests = new ShimTests();
            Shim shim = Shim.Replace(() => shimTests.TestReplace());
            Assert.ThrowsException<Exception>(
                () => Shim.Replace(() => shimTests.TestReplace()).With(() => { }));
            Assert.ThrowsException<Exception>(
                () => Shim.Replace(() => Console.WriteLine(Is.A<string>())).With(() => { }));
        }

        [TestMethod]
        public void TestShimReplaceWith()
        {
            ShimTests shimTests = new ShimTests();
            Action action = new Action(() => { });
            Action<ShimTests> actionInstance = new Action<ShimTests>((s) => { });

            Shim shim = Shim.Replace(() => Console.WriteLine()).With(action);
            Shim shim1 = Shim.Replace(() => shimTests.TestReplace()).With(actionInstance);

            Assert.AreEqual(typeof(Console).GetMethod("WriteLine", Type.EmptyTypes), shim.Original);
            Assert.AreEqual(action, shim.Replacement);

            Assert.AreEqual(typeof(ShimTests).GetMethod("TestReplace"), shim1.Original);
            Assert.AreSame(shimTests, shim1.Instance);
            Assert.AreEqual(actionInstance, shim1.Replacement);
        }

        [TestMethod]
        public void TestReplacePropertyGetter()
        {
            Shim shim = Shim.Replace(() => Thread.CurrentThread.CurrentCulture);

            Assert.AreEqual(typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo)).GetMethod, shim.Original);
            Assert.IsNull(shim.Replacement);
        }

        /// <summary>
        /// The C# compiler currently does not allow poperty setters in lambda expressions.
        /// This helper method works around that by combining a property getter expression
        /// and a value into a setter expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lvalue"></param>
        /// <param name="rvalue"></param>
        /// <returns></returns>
        private static Expression<Action> Assignment<T>(Expression<Func<T>> lvalue, T rvalue)
        {
            var value = Expression.Constant(rvalue, typeof(T));
            var assign = Expression.Assign(lvalue.Body, value);
            return Expression.Lambda<Action>(assign);
        }

        [TestMethod]
        public void TestReplacePropertySetter()
        {
            Shim shim = Shim.Replace(Assignment(() => Is.A<Thread>().CurrentCulture, Is.A<CultureInfo>()));

            Assert.AreEqual(typeof(Thread).GetProperty(nameof(Thread.CurrentCulture), typeof(CultureInfo)).SetMethod, shim.Original);
            Assert.IsNull(shim.Replacement);
        }
    }
}