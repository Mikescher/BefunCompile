using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexGet : BCVertex, MemoryAccess
	{
		public BCVertexGet(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public BCVertexGet(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "GET";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexGet(Direction, Positions);
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

			stackbuilder.Push(ci.GetGridValue(xx, yy));

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
			return "{long v0=sp();sa(gr(sp(),v0));}";
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return "{int64 v0=sp();sa(gr(sp(),v0));}";
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return "v0=sp()" + Environment.NewLine + "sa(gr(sp(),v0))";
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			UnstackifyValue state_y = state.Pop();
			UnstackifyValue state_x = state.Pop();


			state_y.AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.EXPR_GRIDY));
			state_x.AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.EXPR_GRIDX));
			state.Push(new UnstackifyValue(this, UnstackifyValueAccessType.WRITE));

			state_y.LinkPoison(state_x);

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			var var_write = access.SingleOrDefault(p => p.Type == UnstackifyValueAccessType.WRITE);
			var var_readx = access.SingleOrDefault(p => p.Type == UnstackifyValueAccessType.READ && p.Modifier == UnstackifyValueAccessModifier.EXPR_GRIDX);
			var var_ready = access.SingleOrDefault(p => p.Type == UnstackifyValueAccessType.READ && p.Modifier == UnstackifyValueAccessModifier.EXPR_GRIDY);

			if (var_write != null && var_readx != null)
			{
				return new BCVertexExprVarSet(Direction, Positions, var_write.Value.Replacement, ExpressionGet.Create(var_readx.Value.Replacement, var_ready.Value.Replacement));
			}

			if (var_write != null)
			{
				return new BCVertexGetVarSet(Direction, Positions, var_write.Value.Replacement);
			}

			if (var_readx != null)
			{
				return new BCVertexExpression(Direction, Positions, ExpressionGet.Create(var_readx.Value.Replacement, var_ready.Value.Replacement));
			}

			throw new Exception();
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexGet;

			if (arg == null) return false;

			return true;
		}
	}
}
