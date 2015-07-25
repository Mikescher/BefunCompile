using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BefunCompile.Exceptions;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexExprPopSet : BCVertex, MemoryAccess
	{
		public BCExpression X;
		public BCExpression Y;

		public BCVertexExprPopSet(BCDirection d, Vec2i pos, BCExpression xx, BCExpression yy)
			: base(d, new Vec2i[] { pos })
		{
			this.X = xx;
			this.Y = yy;
		}

		public BCVertexExprPopSet(BCDirection d, Vec2i[] pos, BCExpression xx, BCExpression yy)
			: base(d, pos)
		{
			this.X = xx;
			this.Y = yy;
		}

		public override string ToString()
		{
			return string.Format("SET({0}, {1})", X, Y);
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexExprPopSet(Direction, Positions, X, Y);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			if (X is ExpressionConstant && Y is ExpressionConstant)
				return new MemoryAccess[] { this }.Concat(X.ListConstantVariableAccess()).Concat(Y.ListConstantVariableAccess());
			else
				return X.ListConstantVariableAccess().Concat(Y.ListConstantVariableAccess());
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			if (X is ExpressionConstant && Y is ExpressionConstant)
				return X.ListDynamicVariableAccess().Concat(Y.ListDynamicVariableAccess());
			else
				return new MemoryAccess[] { this }.Concat(X.ListDynamicVariableAccess()).Concat(Y.ListDynamicVariableAccess());
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			ci.SetGridValue(X.Calculate(ci), Y.Calculate(ci), stackbuilder.Pop());

			if (Children.Count > 1)
				throw new ArgumentException("#");
			return Children.FirstOrDefault();
		}

		public BCExpression getX()
		{
			return X;
		}

		public BCExpression getY()
		{
			return Y;
		}

		public Vec2l getConstantPos()
		{
			BCExpression xx = getX();
			BCExpression yy = getY();

			if (xx == null || yy == null || !(xx is ExpressionConstant) || !(yy is ExpressionConstant))
				return null;
			else
				return new Vec2l(getX().Calculate(null), getY().Calculate(null));
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

			if (prerequisite(X))
			{
				X = replacement(X);
				found = true;
			}
			if (X.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			if (prerequisite(Y))
			{
				Y = replacement(Y);
				found = true;
			}
			if (Y.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			return found;
		}

		public override bool IsNotGridAccess()
		{
			return false;
		}

		public override bool IsNotStackAccess()
		{
			return false;
		}

		public override bool IsNotVariableAccess()
		{
			return X.IsNotVariableAccess() && Y.IsNotVariableAccess();
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
			return X.GetVariables().Concat(Y.GetVariables());
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			return Enumerable.Empty<int>();
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return string.Format("gw({0},{1},sp());", X.GenerateCodeCSharp(g, false), Y.GenerateCodeCSharp(g, false));
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return string.Format("gw({0},{1},sp());", X.GenerateCodeC(g, false), Y.GenerateCodeC(g, false));
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return string.Format("gw({0},{1},sp())", X.GenerateCodePython(g, false), Y.GenerateCodePython(g, false));
		}

		public override bool TestVertex()
		{
			if (!base.TestVertex()) return false;

			return X.IsNotStackAccess() && Y.IsNotStackAccess();
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			if (X.IsNotStackAccess() && Y.IsNotStackAccess())
			{
				state.Pop().AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.EXPR_VALUE));
			}
			else
			{
				throw new CodeGenException("Invalid Node state");
			}

			return state;
		}


		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			var var_value = access.Single(p => p.Type == UnstackifyValueAccessType.READ && p.Modifier == UnstackifyValueAccessModifier.EXPR_VALUE);

			return new BCVertexExprSet(Direction, Positions, X, Y, var_value.Value.Replacement);
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexExprPopSet;

			if (arg == null) return false;

			return this.X.IsIdentical(arg.X) && this.Y.IsIdentical(arg.Y);
		}
	}
}
