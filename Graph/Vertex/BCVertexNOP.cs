using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexNOP : BCVertex
	{
		public BCVertexNOP(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public BCVertexNOP(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public override string ToString()
		{
			return "NOP";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexNOP(Direction, Positions);
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
			return "";
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return "";
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return "";
		}
	}
}
