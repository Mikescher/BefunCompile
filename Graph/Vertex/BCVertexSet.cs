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
	public class BCVertexSet : BCVertex, MemoryAccess
	{
		public BCVertexSet(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public BCVertexSet(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "SET";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexSet(Direction, Positions);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return new MemoryAccess[] { this };
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, ICalculateInterface ci)
		{
			var yy = stackbuilder.Pop();
			var xx = stackbuilder.Pop();
			var vv = stackbuilder.Pop();

			ci.SetGridValue(xx, yy, vv);

			if (Children.Count > 1)
				throw new ArgumentException("#");
			return Children.FirstOrDefault();
		}

		public BCExpression getX()
		{
			return null;
		}

		public BCExpression getY()
		{
			return null;
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
			return false;
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

		public override bool IsBlock()
		{
			return false;
		}

		public override string GenerateCode(OutputLanguage l, BCGraph g)
		{
			return CodeGenerator.GenerateCodeBCVertexSet(l, this, g);
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			var v_y = state.Pop();
			var v_x = state.Pop();
			var v_v = state.Pop();

			v_y.AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.EXPR_GRIDY));
			v_x.AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.EXPR_GRIDX));
			v_v.AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.EXPR_VALUE));

			v_y.LinkPoison(v_x);
			v_x.LinkPoison(v_v);
			v_v.LinkPoison(v_y);

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			var var_readx = access.Single(p => p.Type == UnstackifyValueAccessType.READ && p.Modifier == UnstackifyValueAccessModifier.EXPR_GRIDX);
			var var_ready = access.Single(p => p.Type == UnstackifyValueAccessType.READ && p.Modifier == UnstackifyValueAccessModifier.EXPR_GRIDY);
			var var_readv = access.Single(p => p.Type == UnstackifyValueAccessType.READ && p.Modifier == UnstackifyValueAccessModifier.EXPR_VALUE);

			return new BCVertexExprSet(Direction, Positions, var_readx.Value.Replacement, var_ready.Value.Replacement, var_readv.Value.Replacement);
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexSet;

			if (arg == null) return false;

			return true;
		}
	}
}
