using System;

namespace BefunCompile.Graph
{
	[Flags]
	public enum BCModArea
	{
		None        = 0x00,

		Stack       = 1 << 0,
		Input       = 1 << 1,
		Output      = 1 << 2,
		Grid        = 1 << 3,
		Variable    = 1 << 4,
	}
}
