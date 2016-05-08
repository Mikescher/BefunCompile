
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
	public class BCVertexSwap : BCVertex
	{
		public BCVertexSwap(BCDirection d, Vec2i pos)
			: base(d, new [] { pos })
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

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, ICalculateInterface ci)
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
			return CodeGenerator.GenerateCodeBCVertexSwap(l, this, g);
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

			if (var_top == null && var_bot == null) return this;

			return new BCVertexNOP(Direction, Positions);
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexSwap;

			if (arg == null) return false;

			return true;
		}
	}
}
