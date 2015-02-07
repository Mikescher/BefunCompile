using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexRandom : BCVertex
	{
		public BCVertexRandom(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public BCVertexRandom(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public override string ToString()
		{
			return "NOP";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexRandom(Direction, Positions);
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
			if (Children.Count != 4)
				throw new ArgumentException("#");
			return Children[new Random().Next(4)];
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return false;
		}

		public override bool IsOnlyStackManipulation()
		{
			return true;
		}

		public override bool IsCodePathSplit()
		{
			return true;
		}

		public override bool IsBlock()
		{
			return false;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return "if(rd()){if(rd()){goto g0;}else{goto g1;}}else{if(rd()){goto g2;}else{goto g3;}}"
				.Replace("g0", "_" + g.Vertices.IndexOf(Children[0]))
				.Replace("g1", "_" + g.Vertices.IndexOf(Children[1]))
				.Replace("g2", "_" + g.Vertices.IndexOf(Children[2]))
				.Replace("g3", "_" + g.Vertices.IndexOf(Children[3]));
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return "if(rd()){if(rd()){goto g0;}else{goto g1;}}else{if(rd()){goto g2;}else{goto g3;}}"
				.Replace("g0", "_" + g.Vertices.IndexOf(Children[0]))
				.Replace("g1", "_" + g.Vertices.IndexOf(Children[1]))
				.Replace("g2", "_" + g.Vertices.IndexOf(Children[2]))
				.Replace("g3", "_" + g.Vertices.IndexOf(Children[3]));
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return "return (((g0)if(rd())else(g1))if(rd())else((g2)if(rd())else(g3)))"
				.Replace("g0", "" + g.Vertices.IndexOf(Children[0]))
				.Replace("g1", "" + g.Vertices.IndexOf(Children[1]))
				.Replace("g2", "" + g.Vertices.IndexOf(Children[2]))
				.Replace("g3", "" + g.Vertices.IndexOf(Children[3]));
		}
	}
}
