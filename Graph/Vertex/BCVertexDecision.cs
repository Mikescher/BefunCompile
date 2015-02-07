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

			if (children.Count != 2)
				throw new Exception("Decision needs 2 children");

			edgeTrue = children.First(p => p.positions[0].X < positions[0].X || p.positions[0].Y < positions[0].Y);
			edgeFalse = children.First(p => p.positions[0].X > positions[0].X || p.positions[0].Y > positions[0].Y);
		}

		public override string ToString()
		{
			return "IF ?";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexDecision(direction, positions);
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
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

		public override bool isOnlyStackManipulation()
		{
			return false;
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
