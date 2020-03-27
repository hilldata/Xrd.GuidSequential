using System;

namespace Xrd {
	/// <summary>
	/// Static class used to generate "Sequential" Guids.
	/// </summary>
	/// <remarks>
	/// As a result of the nature of the sequence algorithm, this Guid embeds the 
	/// CreateTime into the resulting Guid and allows it to be extracted for journaling purposes.
	/// </remarks>
	public static class GuidSequential {
		#region constants
		/// <summary>
		/// The base date
		/// </summary>
		/// <remarks>
		/// Used to limit the size of the data needed to record the initialization date
		/// </remarks>
		public static readonly DateTime BASE_DATE = new DateTime(2000, 1, 1);
		/// <summary>
		/// The Ticks for the base date.
		/// </summary>
		private static readonly long BASE_DATE_TICKS = new DateTime(2000, 1, 1).Ticks;
		#endregion

		/// <summary>
		/// Generate a new "sequential" <see cref="Guid"/> for use in Databases
		/// </summary>
		/// <param name="source">
		/// The (optional) existing <see cref="Guid"/> to use as the base for the result.
		/// </param>
		/// <param name="cTime">
		/// The (optional) Create/Initialized Date/Time value to embed in the result.
		/// If no value is provided (default), the current UTC date/time is used.
		/// </param>
		/// <param name="forMsSql">
		/// Should the resulting Guid be optimized for MS Sql Server? 
		/// (Guids are stored in reverse in Ms Sql Server)
		/// </param>
		/// <returns>A "Sequential" <see cref="Guid"/> suitable for use in non-clustered indices.</returns>
		public static Guid New(Guid? source = null, DateTime? cTime = null, bool forMsSql = true) {
			byte[] arrGuid;
			if (source.HasValue)
				arrGuid = source.Value.ToByteArray();
			else
				arrGuid = generateBaseGuid();
			insertDT(ref arrGuid, cTime, forMsSql);
			return new Guid(arrGuid);
		}

		/// <summary>
		/// Generate a new "sequential" <see cref="Guid"/> for use in Databases
		/// </summary>
		/// <param name="randomSeed">
		/// The (optional) seed to provide to a <see cref="Random"/> 
		/// instance used to generate the base byte array for the result.
		/// </param>
		/// <param name="cTime">
		/// The (optional) Create/Initialized Date/Time value to embed in the result.
		/// If no value is provided (default), the current UTC date/time is used.
		/// </param>
		/// <param name="forMsSql">
		/// Should the resulting Guid be optimized for MS Sql Server? 
		/// (Guids are stored in reverse in Ms Sql Server)
		/// </param>
		/// <returns>A "Sequential" <see cref="Guid"/> suitable for use in non-clustered indices.</returns>
		public static Guid New(int randomSeed, DateTime? cTime = null, bool forMsSql = true) {
			byte[] arrGuid = generateBaseGuid(randomSeed);
			insertDT(ref arrGuid, cTime, forMsSql);
			return new Guid(arrGuid);
		}

		// Generate a new Guid and return its byte array. 
		// If a randomSeed is provided, then a new Random is created to fill a 16-byte array.
		// Else, the .NET Framework's Guid.NewGuid() method is used.
		private static byte[] generateBaseGuid(int? randomSeed = null) {
			if (randomSeed.HasValue) {
				Random r = new Random(randomSeed.Value);
				byte[] vs = new byte[16];
				r.NextBytes(vs);
				return vs;
			} else {
				return Guid.NewGuid().ToByteArray();
			}
		}

		// Calculate the days offset and time of day and copy to the Guid's array.
		private static void insertDT(ref byte[] arrGuid, DateTime? cTime = null, bool forMsSql = true) {
			if (!cTime.HasValue)
				cTime = DateTime.UtcNow;
			// Get the days and milliseconds, which will be used to buile the byte arrays
			TimeSpan days = new TimeSpan(cTime.Value.Ticks - BASE_DATE_TICKS);
			TimeSpan mSec = cTime.Value.TimeOfDay;

			// Convert to byte arrays.
			byte[] arrDays = BitConverter.GetBytes(days.Days);
			// Note that MS Sql Server is accurate to 1/300th of a millisecond, so we divide by 3.33333
			long vMSec = (long)(mSec.TotalMilliseconds / 3.33333);
			byte[] arrMSec = BitConverter.GetBytes(vMSec);

			copyDT(ref arrGuid, arrDays, arrMSec, forMsSql);
		}

		// Copy the Days offset/MSec time of day arrays to the result Guid array
		private static void copyDT(ref byte[] arrGuid, byte[] arrDays, byte[] arrMSec, bool forMsSql = true) {
			// MS Sql Server stores unique identifiers in reverse byte order, so the
			// DT arrays need to be reversed and then copied to the end of the guid.
			if (forMsSql) {
				Array.Reverse(arrDays);
				Array.Reverse(arrMSec);
			}

			// We only need two bytes for the Days offsett count, due to using the BASE_DATE
			// We only need four bytes for the MSec, as it only records the time of day.
			if (forMsSql) {
				Array.Copy(arrDays, arrDays.Length - 2, arrGuid, arrGuid.Length - 2, 2);
				Array.Copy(arrMSec, arrMSec.Length - 4, arrGuid, arrGuid.Length - 6, 4);
			} else {
				Array.Copy(arrDays, 0, arrGuid, 0, 2);
				Array.Copy(arrMSec, 0, arrGuid, 2, 4);
			}
		}

		/// <summary>
		/// Extract the 'CreateTime'/'Initialization Date/Time' from a 
		/// <see cref="GuidSequential"/>-generated <see cref="Guid"/> value.
		/// </summary>
		/// <param name="sequentialGuid">A <see cref="Guid"/> value created by the <see 
		/// cref="GuidSequential"/> class</param>
		/// <param name="forMsSql">Was the <see cref="Guid"/> optimized for MS Sql Server?</param>
		/// <returns>If successful, the original create time; 
		/// else <see langword="null"/> if a value could not be extracted</returns>
		public static DateTime? GetCreateTime(this Guid sequentialGuid, bool forMsSql = true) {
			if (sequentialGuid.Equals(Guid.Empty))
				return null;

			byte[] arrGuid = sequentialGuid.ToByteArray();
			byte[] arrDays = new byte[4];
			byte[] arrMSec = new byte[8];

			if (forMsSql) {
				Array.Copy(arrGuid, arrGuid.Length - 2, arrDays, 2, 2);
				Array.Copy(arrGuid, arrGuid.Length - 6, arrMSec, 4, 4);
				Array.Reverse(arrDays);
				Array.Reverse(arrMSec);
			} else {
				Array.Copy(arrGuid, 0, arrDays, 0, 2);
				Array.Copy(arrGuid, 2, arrMSec, 0, 4);
			}

			int days;
			int mSec;
			try {
				days = BitConverter.ToInt32(arrDays, 0);
			} catch {
				return null;
			}
			try {
				mSec = BitConverter.ToInt32(arrMSec, 0);
				mSec = (int)(mSec * 3.33333);
			} catch {
				return null;
			}

			try {
				return new DateTime(BASE_DATE_TICKS, DateTimeKind.Utc)
					+ new TimeSpan(days, 0, 0, 0, mSec);
			} catch {
				return null;
			}
		}
	}
}