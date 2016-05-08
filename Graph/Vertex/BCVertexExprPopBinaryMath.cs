using BefunCompile.CodeGeneration;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexExprPopBinaryMath : BCVertex
	{
		public readonly BinaryMathType MathType;

		public BCExpression SecondExpression;

		public BCVertexExprPopBinaryMath(BCDirection d, Vec2i[] pos, BCExpression expr, BinaryMathType type)
			: base(d, pos)
		{
			SecondExpression = expr;
			MathType = type;
		}

		public override string ToString()
		{
			return "PUSH(#pop# " + ExpressionBinMath.MathTypeToString(MathType) + " " + SecondExpression + ")";
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
				case BinaryMathType.LT:
					return (a < b) ? 1 : 0;
				case BinaryMathType.GET:
					return (a >= b) ? 1 : 0;
				case BinaryMathType.LET:
					return (a <= b) ? 1 : 0;
				case BinaryMathType.MOD:
					return b == 0 ? 0 : (a % b);
				default:
					throw new Exception("uwotm8");
			}
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexExprPopBinaryMath(Direction, Positions, SecondExpression, MathType);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return SecondExpression.ListConstantVariableAccess();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return SecondExpression.ListDynamicVariableAccess();
		}

		public override bool TestVertex()
		{
			return base.TestVertex() && SecondExpression.IsNotStackAccess();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, ICalculateInterface ci)
		{
			var b = stackbuilder.Pop();
			var a = SecondExpression.Calculate(ci);

			stackbuilder.Push(Calc(a, b));

			if (Children.Count > 1)
				throw new ArgumentException("#");

			return Children.FirstOrDefault();
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

			if (prerequisite(SecondExpression))
			{
				SecondExpression = replacement(SecondExpression);
				found = true;
			}

			if (SecondExpression.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			return found;
		}

		public override bool IsOutput()
		{
			return false;
		}

		public override bool IsNotGridAccess()
		{
			return SecondExpression.IsNotGridAccess();
		}

		public override bool IsNotStackAccess()
		{
			return false;
		}

		public override bool IsNotVariableAccess()
		{
			return SecondExpression.IsNotVariableAccess();
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
			return SecondExpression.GetVariables();
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			return Enumerable.Empty<int>();
		}

		public bool NeedsParen()
		{
			if (SecondExpression is ExpressionConstant)
				return false;

			if (SecondExpression is ExpressionGet)
				return false;

			if (SecondExpression is ExpressionVariable)
				return false;

			if (MathType == BinaryMathType.MUL && SecondExpression is ExpressionBinMath && ((ExpressionBinMath)SecondExpression).Type == BinaryMathType.MUL)
				return false;

			if (MathType == BinaryMathType.ADD && SecondExpression is ExpressionBinMath && ((ExpressionBinMath)SecondExpression).Type == BinaryMathType.ADD)
				return false;

			return true;
		}
		

		public override string GenerateCode(OutputLanguage l, BCGraph g)
		{
			return CodeGenerator.GenerateCodeBCVertexExprPopBinaryMath(l, this, g);
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			state.Peek().AddAccess(this, UnstackifyValueAccessType.READWRITE);

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			var avar = access.Single();

			return new BCVertexExprVarSet(Direction, Positions, avar.Value.Replacement, ExpressionBinMath.Create(avar.Value.Replacement, SecondExpression, MathType));
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexExprPopBinaryMath;

			if (arg == null) return false;

			return this.MathType == arg.MathType && this.SecondExpression.IsIdentical(arg.SecondExpression);
		}
	}
}
