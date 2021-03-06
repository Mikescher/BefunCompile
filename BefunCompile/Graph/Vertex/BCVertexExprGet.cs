﻿using BefunCompile.CodeGeneration;
using BefunCompile.CodeGeneration.Generator;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexExprGet : BCVertex, MemoryAccess
	{
		public BCExpression X;
		public BCExpression Y;

		public BCVertexExprGet(BCDirection d, Vec2i pos, BCExpression xx, BCExpression yy)
			: base(d, new [] { pos })
		{
			this.X = xx;
			this.Y = yy;
		}

		public BCVertexExprGet(BCDirection d, Vec2i[] pos, BCExpression xx, BCExpression yy)
			: base(d, pos)
		{
			this.X = xx;
			this.Y = yy;
		}

		public override string ToString()
		{
			return string.Format("GET({0}, {1})", X, Y);
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexExprGet(Direction, Positions, X, Y);
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, ICalculateInterface ci)
		{
			stackbuilder.Push(ci.GetGridValue(X.Calculate(ci), Y.Calculate(ci)));

			if (Children.Count > 1)
				throw new ArgumentException("#");
			return Children.FirstOrDefault();
		}

		public override int? GetStacksizePredictorDelta()
		{
			return 1;
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

		public BCExpression ToExpression()
		{
			return ExpressionGet.Create(X, Y);
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

		public override BCModArea GetSideEffects()
		{
			return X.GetSideEffects() | Y.GetSideEffects() | BCModArea.Grid_Read | BCModArea.Stack_Write;
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

		public override string GenerateCode(CodeGenerator cg)
		{
			return cg.GenerateCodeBCVertexExprGet(this);
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			if (X.IsStackAccess())
				state.Peek().AddAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.EXPR_GRIDX);

			if (Y.IsStackAccess())
				state.Peek().AddAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.EXPR_GRIDY);

			state.Push(new UnstackifyValue(this, UnstackifyValueAccessType.WRITE));

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			var var_write = access.Single(p => p.Type == UnstackifyValueAccessType.WRITE);
			var var_readx = access.SingleOrDefault(p => p.Type == UnstackifyValueAccessType.READ && p.Modifier == UnstackifyValueAccessModifier.EXPR_GRIDX);
			var var_ready = access.SingleOrDefault(p => p.Type == UnstackifyValueAccessType.READ && p.Modifier == UnstackifyValueAccessModifier.EXPR_GRIDY);

			var expr_x = (var_readx == null) ? X : X.ReplaceUnstackify(var_readx);
			var expr_y = (var_ready == null) ? Y : Y.ReplaceUnstackify(var_ready);

			return new BCVertexExprVarSet(Direction, Positions, var_write.Value.Replacement, ExpressionGet.Create(expr_x, expr_y));
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexExprGet;

			if (arg == null) return false;

			return this.X.IsIdentical(arg.X) && this.Y.IsIdentical(arg.Y);
		}
	}
}
