using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexDecision : BCVertex
	{
		public BCVertex edgeTrue = null;
		public BCVertex edgeFalse = null;

		public BCVertexDecision(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public BCVertexDecision(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public override void AfterGen()
		{
			base.AfterGen();

			if (Children.Count != 2)
				throw new Exception("Decision needs 2 children");

			edgeTrue = Children.First(p => p.Positions[0].X < Positions[0].X || p.Positions[0].Y < Positions[0].Y);
			edgeFalse = Children.First(p => p.Positions[0].X > Positions[0].X || p.Positions[0].Y > Positions[0].Y);
		}

		public override string ToString()
		{
			return "IF ?";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexDecision(Direction, Positions);
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
			var v = stackbuilder.PopBool();

			return v ? edgeTrue : edgeFalse;
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return false;
		}

		public override bool IsOnlyStackManipulation()
		{
			return false;
		}

		public override bool IsCodePathSplit()
		{
			return true;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return string.Format("if(sp()!=0)goto _{0}; else goto _{1};", g.Vertices.IndexOf(edgeTrue), g.Vertices.IndexOf(edgeFalse));
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return string.Format("if(sp()!=0)goto _{0}; else goto _{1};", g.Vertices.IndexOf(edgeTrue), g.Vertices.IndexOf(edgeFalse));
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return string.Format("return ({0})if(sp()!=0)else({1})", g.Vertices.IndexOf(edgeTrue), g.Vertices.IndexOf(edgeFalse));
		}
	}
}
