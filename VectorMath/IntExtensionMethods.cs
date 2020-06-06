using System;

namespace MatterHackers.VectorMath
{
    public static class IntExtensionMethods
    {
		public static ulong GetLongHashCode(this int data, ulong hash = 14695981039346656037)
		{
			return ComputeHash(BitConverter.GetBytes(data), hash);
		}

		// FNV-1a (64-bit) non-cryptographic hash function.
		// Adapted from: http://github.com/jakedouglas/fnv-java
		public static ulong ComputeHash(byte[] bytes, ulong hash = 14695981039346656037)
		{
			const ulong fnv64Prime = 0x100000001b3;

			for (var i = 0; i < bytes.Length; i++)
			{
				hash = hash ^ bytes[i];
				hash *= fnv64Prime;
			}

			return hash;
		}
	}
}
