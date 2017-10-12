using System;

namespace BefunCompile.Graph
{
	[Flags]
	public enum BCModArea
	{
		None           = 0x00,

		Stack_Read     = 1 << 0x0,
		IO_Read        = 1 << 0x1,
		Grid_Read      = 1 << 0x2,
		Variable_Read  = 1 << 0x3,

		Stack_Write    = 1 << 0x8,
		IO_Write       = 1 << 0x9,
		Grid_Write     = 1 << 0xA,
		Variable_Write = 1 << 0xB,
		

		Any_Read  = Stack_Read  | IO_Read  | Grid_Read  | Variable_Read,
		Any_Write = Stack_Write | IO_Write | Grid_Write | Variable_Write,
		
		Any_Stack    = Stack_Read    | Stack_Write,
		Any_IO       = IO_Read       | IO_Write,
		Any_Grid     = Grid_Read     | Grid_Write,
		Any_Variable = Variable_Read | Variable_Write,
	}

	public static class BCModAreaHelper
	{
		public static bool Is(this BCModArea a, BCModArea test) => (a & test) != BCModArea.None;

		public static bool CanSwap(BCModArea a1, BCModArea a2)
		{
			if (a1.Is(BCModArea.Stack_Write) && a2.Is(BCModArea.Stack_Read))  return false;
			if (a1.Is(BCModArea.Stack_Write) && a2.Is(BCModArea.Stack_Write)) return false;
			if (a1.Is(BCModArea.Stack_Read)  && a2.Is(BCModArea.Stack_Write)) return false;

			if (a1.Is(BCModArea.Grid_Write) && a2.Is(BCModArea.Grid_Read))  return false;
			if (a1.Is(BCModArea.Grid_Write) && a2.Is(BCModArea.Grid_Write)) return false;
			if (a1.Is(BCModArea.Grid_Read)  && a2.Is(BCModArea.Grid_Write)) return false;

			if (a1.Is(BCModArea.Variable_Write) && a2.Is(BCModArea.Variable_Read))  return false;
			if (a1.Is(BCModArea.Variable_Write) && a2.Is(BCModArea.Variable_Write)) return false;
			if (a1.Is(BCModArea.Variable_Read)  && a2.Is(BCModArea.Variable_Write)) return false;

			if (a1.Is(BCModArea.Any_IO) && a2.Is(BCModArea.Any_IO)) return false;

			return true;
		}
	}
}
