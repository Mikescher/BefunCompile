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

		public BCVertexBinaryMath(BCDirection d, Vec2i[] pos, BinaryMathType type)
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

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, ICalculateInterface ci)
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

		public override bool IsOutput()
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

		public override string GenerateCode(OutputLanguage l, BCGraph g)
		{
			return CodeGenerator.GenerateCodeBCVertexBinaryMath(l, this);
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			state.Pop().AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.RIGHT_EXPR));
			state.Pop().AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.LEFT_EXPR));
			state.Push(new UnstackifyValue(this, UnstackifyValueAccessType.WRITE));

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			var var_target = access.SingleOrDefault(p => p.Type == UnstackifyValueAccessType.WRITE);
			var var_left = access.SingleOrDefault(p => p.Modifier == UnstackifyValueAccessModifier.LEFT_EXPR);
			var var_right = access.SingleOrDefault(p => p.Modifier == UnstackifyValueAccessModifier.RIGHT_EXPR);


			if (var_target == null && var_left == null && var_right != null) // 0 . 0 . 1
			{
				return new BCVertexExprPopBinaryMath(Direction, Positions, var_right.Value.Replacement, MathType);
			}

			if (var_target == null && var_left != null && var_right == null) // 0 . 1 . 0
			{
				var v_a = new BCVertexExpression(Direction, Positions, var_left.Value.Replacement);
				var v_b = new BCVertexSwap(Direction, Positions);
				var v_c = new BCVertexBinaryMath(Direction, Positions, MathType);

				return new BCVertexBlock(Direction, Positions, v_a, v_b, v_c);
			}

			if (var_target == null && var_left != null && var_right != null) // 0 . 1 . 1
			{
				var expr = ExpressionBinMath.Create(var_left.Value.Replacement, var_right.Value.Replacement, MathType);

				return new BCVertexExpression(Direction, Positions, expr);
			}

			if (var_target != null && var_left == null && var_right == null) // 1 . 0 . 0
			{
				var v_a = new BCVertexBinaryMath(Direction, Positions, MathType);
				var v_b = new BCVertexVarSet(Direction, Positions, var_target.Value.Replacement);

				return new BCVertexBlock(Direction, Positions, v_a, v_b);
			}

			if (var_target != null && var_left == null && var_right != null) // 1 . 0 . 1
			{
				var v_a = new BCVertexExprPopBinaryMath(Direction, Positions, var_right.Value.Replacement, MathType);
				var v_b = new BCVertexVarSet(Direction, Positions, var_target.Value.Replacement);

				return new BCVertexBlock(Direction, Positions, v_a, v_b);
			}

			if (var_target != null && var_left != null && var_right == null) // 1 . 1 . 0
			{
				var v_a = new BCVertexExpression(Direction, Positions, var_left.Value.Replacement);
				var v_b = new BCVertexSwap(Direction, Positions);
				var v_c = new BCVertexBinaryMath(Direction, Positions, MathType);
				var v_d = new BCVertexVarSet(Direction, Positions, var_target.Value.Replacement);

				return new BCVertexBlock(Direction, Positions, v_a, v_b, v_c, v_d);
			}

			if (var_target != null && var_left != null && var_right != null) // 1 . 1 . 1
			{
				var expr = ExpressionBinMath.Create(var_left.Value.Replacement, var_right.Value.Replacement, MathType);

				return new BCVertexExprVarSet(Direction, Positions, var_target.Value.Replacement, expr);
			}

			throw new Exception();
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexBinaryMath;

			if (arg == null) return false;

			return this.MathType == arg.MathType;
		}
	}
}
