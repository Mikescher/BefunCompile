
using BefunCompile.Math;
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

		public BCVertexBinaryMath(BCDirection d, Vec2i pos, char type)
			: base(d, new Vec2i[] { pos })
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

		public BCVertexBinaryMath(BCDirection d, Vec2i[] pos, char type)
			: base(d, pos)
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

		public BCVertexBinaryMath(BCDirection d, Vec2i[] pos, BinaryMathType type)
			: base(d, pos)
		{
			this.mtype = type;
		}

		public override string ToString()
		{
			return mtype.ToString();
		}


		public long Calc(BCVertexPush a, BCVertexPush b) // Reihenfolge:   a  b  +
		{
			return Calc(a.value, b.value);
		}

		public long Calc(long a, long b) // Reihenfolge:   a  b  +
		{
			switch (mtype)
			{
				case BinaryMathType.ADD:
					return a + b;
				case BinaryMathType.SUB:
					return a - b;
				case BinaryMathType.MUL:
					return a * b;
				case BinaryMathType.DIV:
					return b == 0 ? 0 : (a / b);
				case BinaryMathType.GT:
					return (a > b) ? 1 : 0;
				case BinaryMathType.MOD:
					return b == 0 ? 0 : (a % b);
				default:
					throw new Exception("uwotm8");
			}
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexBinaryMath(direction, positions, mtype);
		}
	}
}
