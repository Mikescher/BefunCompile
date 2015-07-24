
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexSwap : BCVertex
	{
		public BCVertexSwap(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public BCVertexSwap(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "SWAP";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexSwap(Direction, Positions);
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
			stackbuilder.Swap();

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
			return "{long v0=sp();long v1=sp();sa(v0);sa(v1);}";
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return "{int64 v0=sp();int64 v1=sp();sa(v0);sa(v1);}";
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return "v0=sp()" + Environment.NewLine + "v1=sp()" + Environment.NewLine + "sa(v0)" + Environment.NewLine + "sa(v1)";
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			state.Peek().AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.POS_TOP));
			state.Swap();
			state.Peek().AddAccess(new UnstackifyValueAccess(this, UnstackifyValueAccessType.READ, UnstackifyValueAccessModifier.POS_BOT));

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			var var_top = access.SingleOrDefault(p => p.Modifier == UnstackifyValueAccessModifier.POS_TOP);
			var var_bot = access.SingleOrDefault(p => p.Modifier == UnstackifyValueAccessModifier.POS_BOT);

			if (var_top == null || var_bot == null) return new BCVertexNOP(Direction, Positions);
			
			return this;
		}
	}
}
