using BefunCompile.Graph;
using System;

namespace BefunCompile.Math
{
	public class Vec2i
	{
		public readonly int X;
		public readonly int Y;

		public Vec2i(int xx, int yy)
		{
			this.X = xx;
			this.Y = yy;
		}

		public Vec2i Move(BCDirection direction, int width, int height, bool jump)
		{
			int nx = X;
			int ny = Y;

			switch (BCDirectionHelper.toNonSMDirection(direction))
			{
				case BCDirection.FROM_LEFT:
					nx++;
					if (jump)
						nx++;
					break;
				case BCDirection.FROM_TOP:
					ny++;
					if (jump)
						ny++;
					break;
				case BCDirection.FROM_RIGHT:
					nx--;
					if (jump)
						nx--;
					break;
				case BCDirection.FROM_BOTTOM:
					ny--;
					if (jump)
						ny--;
					break;
			}

			nx += width;
			nx %= width;

			ny += height;
			ny %= height;

			return new Vec2i(nx, ny);
		}

		public override bool Equals(Object obj)
		{
			var other = obj as Vec2i;
			if (other == null)
				return false;

			return Equals(other);
		}

		public override int GetHashCode()
		{
			return X.GetHashCode() ^ Y.GetHashCode();
		}

		public bool Equals(Vec2i other)
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

		public override string ToString()
		{
			return string.Format("{{{0}|{1}}}", X, Y);
		}
	}
}
