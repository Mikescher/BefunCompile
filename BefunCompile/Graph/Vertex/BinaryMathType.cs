using System;

namespace BefunCompile.Graph.Vertex
{
	public enum BinaryMathType
	{
		ADD,
		SUB,
		MUL,
		DIV,
		GT,
		LT,
		GET,
		LET,
		MOD,
		EQ,
		NEQ,
	}

	public static class BinaryMathTypeHelper
	{
		public static bool IsNativeBoolReturn(BinaryMathType t)
		{
			switch (t)
			{
				case BinaryMathType.ADD:
				case BinaryMathType.SUB:
				case BinaryMathType.MUL:
				case BinaryMathType.DIV:
				case BinaryMathType.MOD:
					return false;

				case BinaryMathType.GT:
				case BinaryMathType.LT:
				case BinaryMathType.GET:
				case BinaryMathType.LET:
				case BinaryMathType.EQ:
				case BinaryMathType.NEQ:
					return true;

				default:
					throw new Exception("uwotm8");
			}
		}
	}
}
