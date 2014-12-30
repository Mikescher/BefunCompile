
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
		MOD
	}

	public class BCVertexBinaryMath : BCVertex
	{
		public readonly BinaryMathType mtype;

		public BCVertexBinaryMath(BCDirection d, char type)
			: base(d)
		{
			switch (type)
			{
				case '+':
					this.mtype = BinaryMathType.ADD;
					break;
				case '-':
					this.mtype = BinaryMathType.SUB;
					break;
				case '*':
					this.mtype = BinaryMathType.MUL;
					break;
				case '/':
					this.mtype = BinaryMathType.DIV;
					break;
				case '`':
					this.mtype = BinaryMathType.GT;
					break;
				case '%':
					this.mtype = BinaryMathType.MOD;
					break;
				default:
					throw new ArgumentException("Not a Math OP: " + type);
			}
		}
	}
}
