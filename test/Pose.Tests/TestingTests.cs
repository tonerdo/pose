using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using static Pose.PoseContext;

namespace Pose.Tests
{
	[TestClass]
	public class TestingTests
	{
		[TestMethod]
		public void IsolateWithoutShimShouldAllowUnitTestAssertions()
		{
			Isolate(() => { Assert.AreEqual(1, 1); });
		}

		[TestMethod]
		public void IsolateWithShimShouldAllowUnitTestAssertions()
		{
			Isolate(() => { Assert.AreEqual(1, 1); }, SimpleShim);
		}

		[TestMethod]
		public void IsolateWithoutShimShouldAllowShouldlyAssertions()
		{
			Isolate(() => { 1.ShouldBe(1); });
		}

		[TestMethod]
		public void IsolateWithShimShouldAllowShouldlyAssertions()
		{
			Isolate(() => { 1.ShouldBe(1); }, SimpleShim);
		}

		[TestMethod]
		public void IsolateWithoutShimShouldNotAffectFailedUnitTestMessages()
		{
			Should.NotThrow(() =>
			{
				try
				{
					Isolate(() => { 1.ShouldBe(2); });
				}
				catch (ShouldAssertException)
				{
					//This one is expected
				}
			});
		}

		[TestMethod]
		public void IsolateWithShimShouldNotAffectFailedUnitTestMessages()
		{
			Should.NotThrow(() =>
			{
				try
				{
					Isolate(() => { 1.ShouldBe(2); }, SimpleShim);
				}
				catch (ShouldAssertException)
				{
					//This one is expected
				}
			});
		}

		private static Shim SimpleShim => Shim.Replace(() => Console.Clear()).With(Console.Clear);
	}
}