using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexInput : BCVertex
	{
		private readonly bool modeInteger; // true = int | false = char

		public BCVertexInput(BCDirection d, Vec2i pos, long mode)
			: base(d, new Vec2i[] { pos })
		{
			modeInteger = mode == '&';
		}

		public BCVertexInput(BCDirection d, Vec2i[] pos, long mode)
			: base(d, pos)
		{
			modeInteger = mode == '&';
		}

		public BCVertexInput(BCDirection d, Vec2i[] pos, bool mode)
			: base(d, pos)
		{
			modeInteger = mode;
		}

		public override string ToString()
		{
			return string.Format("IN({0})", modeInteger ? "INT" : "CHAR");
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexInput(Direction, Positions, modeInteger);
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
			throw new System.NotImplementedException();
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
			if (modeInteger)
				return "{long v0;while(long.TryParse(System.Console.ReadLine(),out v0));sa(v0);}";
			else
				return "sa(System.Console.ReadLine());";
		}

		public override string GenerateCodeC(BCGraph g)
		{
			if (modeInteger)
				return "{char v0[128];int64 v1;fgets(v0,sizeof(v0),stdin);sscanf(v0,\"%lld\",&v1);sa(v1);}";
			else
				return "sa(getchar());";
		}

		public override string GenerateCodePython(BCGraph g)
		{
			if (modeInteger)
				return "sa(int(input(\"\")))";
			else
				return "sa(ord(input(\"\")[0]))";
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			state.Push(new UnstackifyValue(this, UnstackifyValueAccessType.WRITE));

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			return new BCVertexInputVarSet(Direction, Positions, access.Single().Value.Replacement, modeInteger);
		}
	}
}
