using System;

namespace BefunCompile.Graph
{
	public enum BCDirection
	{
		UNKNOWN,

		FROM_LEFT,
		FROM_TOP,
		FROM_RIGHT,
		FROM_BOTTOM,

		SM_FROM_LEFT,
		SM_FROM_TOP,
		SM_FROM_RIGHT,
		SM_FROM_BOTTOM,
	}

	public static class BCDirectionHelper
	{
		public static BCDirection switchSMDirection(BCDirection d)
		{
			if (isSMDirection(d))
				return toNonSMDirection(d);
			else
				return toSMDirection(d);
		}

		public static BCDirection toSMDirection(BCDirection d)
		{
			switch (d)
			{
				case BCDirection.FROM_LEFT:
				case BCDirection.SM_FROM_LEFT:
					return BCDirection.SM_FROM_LEFT;
				case BCDirection.FROM_TOP:
				case BCDirection.SM_FROM_TOP:
					return BCDirection.SM_FROM_TOP;
				case BCDirection.FROM_RIGHT:
				case BCDirection.SM_FROM_RIGHT:
					return BCDirection.SM_FROM_RIGHT;
				case BCDirection.FROM_BOTTOM:
				case BCDirection.SM_FROM_BOTTOM:
					return BCDirection.SM_FROM_BOTTOM;
				default:
					throw new Exception("u wot m8");
			}
		}

		public static BCDirection toNonSMDirection(BCDirection d)
		{
			switch (d)
			{
				case BCDirection.FROM_LEFT:
				case BCDirection.SM_FROM_LEFT:
					return BCDirection.FROM_LEFT;
				case BCDirection.FROM_TOP:
				case BCDirection.SM_FROM_TOP:
					return BCDirection.FROM_TOP;
				case BCDirection.FROM_RIGHT:
				case BCDirection.SM_FROM_RIGHT:
					return BCDirection.FROM_RIGHT;
				case BCDirection.FROM_BOTTOM:
				case BCDirection.SM_FROM_BOTTOM:
					return BCDirection.FROM_BOTTOM;
				default:
					throw new Exception("u wot m8");
			}
		}

		public static bool isSMDirection(BCDirection d)
		{
			switch (d)
			{
				case BCDirection.FROM_LEFT:
				case BCDirection.FROM_TOP:
				case BCDirection.FROM_RIGHT:
				case BCDirection.FROM_BOTTOM:
					return false;
				case BCDirection.SM_FROM_LEFT:
				case BCDirection.SM_FROM_TOP:
				case BCDirection.SM_FROM_RIGHT:
				case BCDirection.SM_FROM_BOTTOM:
					return true;
				default:
					throw new Exception("u wot m8");
			}
		}
	}
}
