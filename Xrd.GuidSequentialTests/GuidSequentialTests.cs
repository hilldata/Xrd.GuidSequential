using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xrd.Tests {
	[TestClass()]
	public class GuidSequentialTests {
		[TestMethod()]
		public void GetCreateTime_Test_CTime_Defaults() {
			//Arrange
			DateTime now = DateTime.UtcNow;
			Guid g1 = GuidSequential.New();

			// Act
			DateTime? cTime = g1.GetCreateTime();

			// Assert
			Assert.IsTrue(cTime.HasValue);
			Assert.AreEqual(now.Date, cTime.Value.Date);
			Assert.AreEqual(now.Hour, cTime.Value.Hour);
			Assert.AreEqual(now.Minute, cTime.Value.Minute);
			Assert.AreEqual(now.Second, cTime.Value.Second);
			// We skip testing Milliseconds due to rounding errors from
			// the division by 3.33333
		}

		[TestMethod()]
		public void GetCreateTime_Test_CTime_NotSet_NotMS() {
			//Arrange
			DateTime now = DateTime.UtcNow;
			Guid g1 = GuidSequential.New(forMsSql: false);

			// Act
			DateTime? cTime = g1.GetCreateTime(false);

			// Assert
			Assert.IsTrue(cTime.HasValue);
			Assert.AreEqual(now.Date, cTime.Value.Date);
			Assert.AreEqual(now.Hour, cTime.Value.Hour);
			Assert.AreEqual(now.Minute, cTime.Value.Minute);
			Assert.AreEqual(now.Second, cTime.Value.Second);
			// We skip testing Milliseconds due to rounding errors from
			// the division by 3.33333
		}

		[TestMethod()]
		public void GetCreateTime_Test_CTimeProvided() {
			DateTime now = new DateTime(2010, 1, 2, 12, 30, 9, 10);
			Guid g1 = GuidSequential.New(cTime: now);

			// Act
			DateTime? cTime = g1.GetCreateTime();

			// Assert
			Assert.IsTrue(cTime.HasValue);
			Assert.AreEqual(now.Date, cTime.Value.Date);
			Assert.AreEqual(now.Hour, cTime.Value.Hour);
			Assert.AreEqual(now.Minute, cTime.Value.Minute);
			Assert.AreEqual(now.Second, cTime.Value.Second);
			// We skip testing Milliseconds due to rounding errors from
			// the division by 3.33333
		}

		[TestMethod()]
		public void GetCreateTime_Test_CTimeProvided_NotMS() {
			DateTime now = new DateTime(2010, 1, 2);
			Guid g1 = GuidSequential.New(cTime: now, forMsSql: false);

			// Act
			DateTime? cTime = g1.GetCreateTime(false);

			// Assert
			Assert.IsTrue(cTime.HasValue);
			Assert.AreEqual(now.Date, cTime.Value.Date);
			Assert.AreEqual(now.Hour, cTime.Value.Hour);
			Assert.AreEqual(now.Minute, cTime.Value.Minute);
			Assert.AreEqual(now.Second, cTime.Value.Second);
		}

		[TestMethod()]
		public void New_TestSeed_NotEqual() {
			// Arrange
			Guid g1 = GuidSequential.New(10);
			Guid g2 = GuidSequential.New(101);

			// Assert
			Assert.AreNotEqual(g1, g2);
		}

		[TestMethod()]
		public void New_TestSeed_NotEqual_noCTimeProvided() {
			// Arrange
			Guid g1 = GuidSequential.New(10);
			System.Threading.Thread.Sleep(10);
			Guid g2 = GuidSequential.New(10);

			Assert.AreNotEqual(g1, g2);
		}

		[TestMethod()]
		public void New_TestSeed_Equal_CTimeProvided() {
			int seed = 10202;
			DateTime cTime = DateTime.UtcNow;

			Guid g1 = GuidSequential.New(seed, cTime);
			Guid g2 = GuidSequential.New(seed, cTime);

			Assert.AreEqual(g1, g2);
		}

		[TestMethod()]
		public void New_TestSource() {
			Guid source = Guid.NewGuid();

			Guid g1 = GuidSequential.New(source);
			System.Threading.Thread.Sleep(10);
			Guid g2 = GuidSequential.New(source);

			Assert.AreEqual(15, countSameBytes(g1, g2));

			g1 = GuidSequential.New(source, forMsSql: false);
			System.Threading.Thread.Sleep(10);
			g2 = GuidSequential.New(source, forMsSql: false);
			Assert.AreEqual(15, countSameBytes(g1, g2));

			DateTime now = DateTime.UtcNow;
			DateTime next = now.AddYears(150).AddMonths(5).AddDays(1).AddMinutes(124).AddSeconds(15);

			g1 = GuidSequential.New(source, now);
			g2 = GuidSequential.New(source, next);
			Assert.AreEqual(11, countSameBytes(g1, g2));
		}

		private int countSameBytes(Guid g1, Guid g2) {
			byte[] a = g1.ToByteArray();
			byte[] b = g2.ToByteArray();

			int c = 0;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] == b[i])
					c++;
			}
			return c;
		}
	}
}