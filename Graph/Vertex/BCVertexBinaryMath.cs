using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		public readonly BinaryMathType MathType;

		public BCVertexBinaryMath(BCDirection d, Vec2i pos, long type)
			: base(d, new[] { pos })
		{
			switch (type)
			{
				case '+':
					MathType = BinaryMathType.ADD;
					break;
				case '-':
					MathType = BinaryMathType.SUB;
					break;
				case '*':
					MathType = BinaryMathType.MUL;
					break;
				case '/':
					MathType = BinaryMathType.DIV;
					break;
				case '`':
					MathType = BinaryMathType.GT;
					break;
				case '%':
					MathType = BinaryMathType.MOD;
					break;
				default:
					throw new ArgumentException("Not a Math OP: " + type);
			}
		}

		private BCVertexBinaryMath(BCDirection d, Vec2i[] pos, BinaryMathType type)
			: base(d, pos)
		{
			MathType = type;
		}

		public override string ToString()
		{
			return MathType.ToString();
		}

		private long Calc(long a, long b) // Reihenfolge:   a  b  +
		{
			switch (MathType)
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
			return new BCVertexBinaryMath(Direction, Positions, MathType);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			var b = stackbuilder.Pop();
			var a = stackbuilder.Pop();

			stackbuilder.Push(Calc(a, b));

			if (Children.Count > 1)
				throw new ArgumentException("#");
			return Children.FirstOrDefault();
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return false;
		}

		public override bool IsNotGridAccess()
		{
			return true;
		}

		public override bool IsNotStackAccess()
		{
			return false;
		}

		public override bool IsNotVariableAccess()
		{
			return true;
		}

		public override bool IsCodePathSplit()
		{
			return false;
		}

		public override bool IsBlock()
		{
			return false;
		}

		public override bool IsRandom()
		{
			return false;
		}

		public override IEnumerable<ExpressionVariable> GetVariables()
		{
			return Enumerable.Empty<ExpressionVariable>();
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			return Enumerable.Empty<int>();
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			StringBuilder codebuilder = new StringBuilder();

			switch (MathType)
			{
				case BinaryMathType.ADD:
					codebuilder.AppendLine("sa(sp()+sp());");
					break;
				case BinaryMathType.SUB:
					codebuilder.AppendLine("{long v0=sp();sa(sp()-v0);}");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine("sa(sp()*sp());");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine("{long v0=sp();sa(td(sp(),v0));}");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("{long v0=sp();sa((sp()>v0)?1:0);}");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("{long v0=sp();sa(tm(sp(),v0));}");
					break;
				default:
					throw new Exception("uwotm8");
			}

			return codebuilder.ToString();
		}

		public override string GenerateCodeC(BCGraph g)
		{
			StringBuilder codebuilder = new StringBuilder();

			switch (MathType)
			{
				case BinaryMathType.ADD:
					codebuilder.AppendLine("sa(sp()+sp());");
					break;
				case BinaryMathType.SUB:
					codebuilder.AppendLine("{int64 v0=sp();sa(sp()-v0);}");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine("sa(sp()*sp());");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine("{int64 v0=sp();sa(td(sp(),v0));}");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("{int64 v0=sp();sa((sp()>v0)?1:0);}");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("{int64 v0=sp();sa(tm(sp(),v0));}");
					break;
				default:
					throw new Exception("uwotm8");
			}

			return codebuilder.ToString();
		}

		public override string GenerateCodePython(BCGraph g)
		{
			StringBuilder codebuilder = new StringBuilder();

			switch (MathType)
			{
				case BinaryMathType.ADD:
					codebuilder.AppendLine("sa(sp()+sp());");
					break;
				case BinaryMathType.SUB:
					codebuilder.AppendLine("v0=sp()");
					codebuilder.AppendLine("sa(sp()-v0)");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine("sa(sp()*sp());");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine("v0=sp()");
					codebuilder.AppendLine("sa(td(sp(),v0))");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("v0=sp()");
					codebuilder.AppendLine("sa((1)if(sp()>v0)else(0))");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("v0=sp()");
					codebuilder.AppendLine("sa(tm(sp(),v0))");
					break;
				default:
					throw new Exception("uwotm8");
			}

			return codebuilder.ToString();
		}
	}
}
