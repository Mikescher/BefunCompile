using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexExprBinaryMath : BCVertex
	{
		public readonly BinaryMathType MathType;

		public BCExpression FirstExpression;

		public BCVertexExprBinaryMath(BCDirection d, Vec2i[] pos, BCExpression expr, BinaryMathType type)
			: base(d, pos)
		{
			FirstExpression = expr;
			MathType = type;
		}

		public override string ToString()
		{
			return "PUSH(" + FirstExpression + " " + ExpressionBinMath.MathTypeToChar(MathType) + " #pop#)";
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
			return new BCVertexExprBinaryMath(Direction, Positions, FirstExpression, MathType);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return FirstExpression.ListConstantVariableAccess();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return FirstExpression.ListDynamicVariableAccess();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			var b = stackbuilder.Pop();
			var a = FirstExpression.Calculate(ci);

			stackbuilder.Push(Calc(a, b));

			if (Children.Count > 1)
				throw new ArgumentException("#");

			return Children.FirstOrDefault();
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

			if (prerequisite(FirstExpression))
			{
				FirstExpression = replacement(FirstExpression);
				found = true;
			}

			if (FirstExpression.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			return found;
		}

		public override bool IsOnlyStackManipulation()
		{
			return FirstExpression.IsOnlyStackManipulation();
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
			return FirstExpression.GetVariables();
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			return Enumerable.Empty<int>();
		}

		private bool NeedsParen()
		{
			if (FirstExpression is ExpressionConstant)
				return false;

			if (FirstExpression is ExpressionGet)
				return false;

			if (FirstExpression is ExpressionVariable)
				return false;

			if (MathType == BinaryMathType.MUL && FirstExpression is ExpressionBinMath && ((ExpressionBinMath)FirstExpression).Type == BinaryMathType.MUL)
				return false;

			if (MathType == BinaryMathType.ADD && FirstExpression is ExpressionBinMath && ((ExpressionBinMath)FirstExpression).Type == BinaryMathType.ADD)
				return false;

			return true;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			StringBuilder codebuilder = new StringBuilder();

			switch (MathType)
			{
				case BinaryMathType.ADD:
					codebuilder.AppendLine("sa(sp()+" + Paren(FirstExpression.GenerateCodeCSharp(g), NeedsParen()) + ");");
					break;
				case BinaryMathType.SUB:
					codebuilder.AppendLine("sa(sp()-" + Paren(FirstExpression.GenerateCodeCSharp(g), NeedsParen()) + ");");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine("sa(sp()*" + Paren(FirstExpression.GenerateCodeCSharp(g), NeedsParen()) + ");");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine("{long v0=" + FirstExpression.GenerateCodeCSharp(g) + ";sa((v0==0)?0:(sp()/v0));}");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("{long v0=" + FirstExpression.GenerateCodeCSharp(g) + ";sa((sp()>v0)?1:0);}");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("{long v0=" + FirstExpression.GenerateCodeCSharp(g) + ";sa((v0==0)?0:(sp()%v0));}");
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
					codebuilder.AppendLine("sa(sp()+" + Paren(FirstExpression.GenerateCodeC(g), NeedsParen()) + ");");
					break;
				case BinaryMathType.SUB:
					codebuilder.AppendLine("sa(sp()-" + Paren(FirstExpression.GenerateCodeC(g), NeedsParen()) + ");");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine("sa(sp()*" + Paren(FirstExpression.GenerateCodeC(g), NeedsParen()) + ");");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine("{int64 v0=" + FirstExpression.GenerateCodeC(g) + ";sa((v0==0)?0:(sp()/v0));}");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("{int64 v0=" + FirstExpression.GenerateCodeC(g) + ";sa((sp()>v0)?1:0);}");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("{int64 v0=" + FirstExpression.GenerateCodeC(g) + ";sa((v0==0)?0:(sp()%v0));}");
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
					codebuilder.AppendLine("sa(sp()+" + Paren(FirstExpression.GenerateCodePython(g), NeedsParen()) + ");");
					break;
				case BinaryMathType.SUB:
					codebuilder.AppendLine("sa(sp()-" + Paren(FirstExpression.GenerateCodePython(g), NeedsParen())) + ");");
					break;
				case BinaryMathType.MUL:
					codebuilder.AppendLine("sa(sp()*" + Paren(FirstExpression.GenerateCodePython(g), NeedsParen()) + ");");
					break;
				case BinaryMathType.DIV:
					codebuilder.AppendLine("sa(td(sp()," + FirstExpression.GenerateCodePython(g) + "))");
					break;
				case BinaryMathType.GT:
					codebuilder.AppendLine("sa((1)if(sp()>(" + FirstExpression.GenerateCodePython(g) + "))else(0))");
					break;
				case BinaryMathType.MOD:
					codebuilder.AppendLine("sa(tm(sp()," + FirstExpression.GenerateCodePython(g) + "))");
					break;
				default:
					throw new Exception("uwotm8");
			}

			return codebuilder.ToString();
		}
	}
}
