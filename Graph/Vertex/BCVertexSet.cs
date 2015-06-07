using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
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

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return "{long v0=sp();long v1=sp();gw(v1,v0,sp());}";
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return "{int64 v0=sp();int64 v1=sp();gw(v1,v0,sp());}";
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return "v0=sp()" + Environment.NewLine + "v1=sp()" + Environment.NewLine + "gw(v1,v0,sp())";
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			state.Pop().AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.EXPR_GRIDY));
			state.Pop().AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.EXPR_GRIDX));
			state.Pop().AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.EXPR_VALUE));

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			var var_readx = access.SingleOrDefault(p => p.Type == UnstackifyValueAccessType.READ && p.Modifier == UnstackifyValueAccessModifier.EXPR_GRIDX);
			var var_ready = access.SingleOrDefault(p => p.Type == UnstackifyValueAccessType.READ && p.Modifier == UnstackifyValueAccessModifier.EXPR_GRIDY);
			var var_readv = access.SingleOrDefault(p => p.Type == UnstackifyValueAccessType.READ && p.Modifier == UnstackifyValueAccessModifier.EXPR_VALUE);

			return new BCVertexTotalSet(Direction, Positions, var_readx.Value.Replacement, var_ready.Value.Replacement, var_readv.Value.Replacement);
		}
	}
}
