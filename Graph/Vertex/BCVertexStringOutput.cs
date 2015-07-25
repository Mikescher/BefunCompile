using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexStringOutput : BCVertex
	{
		public readonly string Value;

		public BCVertexStringOutput(BCDirection d, Vec2i pos, string val)
			: base(d, new Vec2i[] { pos })
		{
			this.Value = val;
		}

		public BCVertexStringOutput(BCDirection d, Vec2i[] pos, string val)
			: base(d, pos)
		{
			this.Value = val;
		}

		public override string ToString()
		{
			return string.Format("OUT_(\"{1}\")", Value);
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexStringOutput(Direction, Positions, Value);
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
			outbuilder.Append(Value);

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
			return true;
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
			return string.Format("System.Console.Out.Write(\"{0}\");", Value);
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return string.Format("printf(\"{0}\");", Value);
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return string.Format("print(\"{0}\",end=\"\",flush=True)", Value);
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();
			
			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			return this;
		}
	}
}
