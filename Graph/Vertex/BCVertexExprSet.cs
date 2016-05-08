using BefunCompile.CodeGeneration;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BefunCompile.CodeGeneration.Generator;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexExprSet : BCVertex, MemoryAccess
	{
		public BCExpression X;
		public BCExpression Y;
		public BCExpression Value;

		public BCVertexExprSet(BCDirection d, Vec2i pos, BCExpression xx, BCExpression yy, BCExpression val)
			: base(d, new Vec2i[] { pos })
		{
			this.X = xx;
			this.Y = yy;
			this.Value = val;
		}

		public BCVertexExprSet(BCDirection d, Vec2i[] pos, BCExpression xx, BCExpression yy, BCExpression val)
			: base(d, pos)
		{
			this.X = xx;
			this.Y = yy;
			this.Value = val;
		}

		public override string ToString()
		{
			return string.Format("SET({0}, {1}) = {2}", X, Y, Value);
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexExprSet(Direction, Positions, X, Y, Value);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{

			if (X is ExpressionConstant && Y is ExpressionConstant)
				return new MemoryAccess[] { this }
					.Concat(X.ListConstantVariableAccess())
					.Concat(Y.ListConstantVariableAccess())
					.Concat(Value.ListConstantVariableAccess());
			else
				return X.ListConstantVariableAccess()
					.Concat(Y.ListConstantVariableAccess())
					.Concat(Value.ListConstantVariableAccess());
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{

			if (X is ExpressionConstant && Y is ExpressionConstant)
				return X.ListDynamicVariableAccess()
					.Concat(Y.ListDynamicVariableAccess())
					.Concat(Value.ListDynamicVariableAccess());
			else
				return new MemoryAccess[] { this }
					.Concat(X.ListDynamicVariableAccess())
					.Concat(Y.ListDynamicVariableAccess())
					.Concat(Value.ListDynamicVariableAccess());
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, ICalculateInterface ci)
		{
			ci.SetGridValue(X.Calculate(ci), Y.Calculate(ci), Value.Calculate(ci));

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

			if (prerequisite(Value))
			{
				Value = replacement(Value);
				found = true;
			}
			if (Value.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			return found;
		}

		public override bool IsOutput()
		{
			return false;
		}

		public override bool IsInput()
		{
			return false;
		}

		public override bool IsNotGridAccess()
		{
			return false;
		}

		public override bool IsNotStackAccess()
		{
			return X.IsNotStackAccess() && Y.IsNotStackAccess() && Value.IsNotStackAccess();
		}

		public override bool IsNotVariableAccess()
		{
			return X.IsNotVariableAccess() && Y.IsNotVariableAccess() && Value.IsNotVariableAccess();
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
			return X.GetVariables().Concat(Y.GetVariables()).Concat(Value.GetVariables());
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			return Enumerable.Empty<int>();
		}

		public override string GenerateCode(OutputLanguage l, BCGraph g)
		{
			return CodeGenerator.GenerateCodeBCVertexExprSet(l, this, g);
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			if (X.IsNotStackAccess() && Y.IsNotStackAccess() && Value.IsNotStackAccess())
			{
				// all is good
			}
			else
			{
				if (!X.IsNotStackAccess())
					state.Peek().AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.EXPR_GRIDX));

				if (!Y.IsNotStackAccess())
					state.Peek().AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.EXPR_GRIDY));

				if (!Value.IsNotStackAccess())
					state.Peek().AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.EXPR_VALUE));
			}

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			var var_readx = access.SingleOrDefault(p => p.Type == UnstackifyValueAccessType.READ && p.Modifier == UnstackifyValueAccessModifier.EXPR_GRIDX);
			var var_ready = access.SingleOrDefault(p => p.Type == UnstackifyValueAccessType.READ && p.Modifier == UnstackifyValueAccessModifier.EXPR_GRIDY);
			var var_readv = access.SingleOrDefault(p => p.Type == UnstackifyValueAccessType.READ && p.Modifier == UnstackifyValueAccessModifier.EXPR_VALUE);

			var expr_x = (var_readx == null) ? X : X.ReplaceUnstackify(var_readx);
			var expr_y = (var_ready == null) ? Y : Y.ReplaceUnstackify(var_ready);
			var expr_v = (var_readv == null) ? Value : Value.ReplaceUnstackify(var_readv);

			return new BCVertexExprSet(Direction, Positions, expr_x, expr_y, expr_v);
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexExprSet;

			if (arg == null) return false;

			return this.X.IsIdentical(arg.X) && this.Y.IsIdentical(arg.Y) && this.Value.IsIdentical(arg.Value);
		}
	}
}
