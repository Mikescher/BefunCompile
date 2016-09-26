using System;

namespace BefunCompile.Math
{
	public class Vec2l
	{
		public readonly long X;
		public readonly long Y;

		public Vec2l(long xx, long yy)
		{
			this.X = xx;
			this.Y = yy;
		}

		public override bool Equals(Object obj)
		{
			var other = obj as Vec2l;
			if (other == null)
				return false;

			return Equals(other);
		}

		public override int GetHashCode()
		{
			return X.GetHashCode() ^ Y.GetHashCode();
		}

		public bool Equals(Vec2l other)
		{
			if (other == null)
			{
				return false;
			}

			if (other.X == X && other.Y == Y)
			{
				return true;
			}

			return false;
		}
	}
}
