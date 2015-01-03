﻿using BefunCompile.Graph.Expression;
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
			return new BCVertexRandom(direction, positions);
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
			if (children.Count != 4)
				throw new ArgumentException("#");
			return children[new Random().Next(4)];
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return false;
		}

		public override bool isOnlyStackManipulation()
		{
			return true;
		}

		public override string GenerateCode(BCGraph g)
		{
			return "if(rd()){if(rd()){goto g0;}else{goto g1;}}else{if(rd()){goto g2;}else{goto g3;}}"
				.Replace("g0", "_" + g.vertices.IndexOf(children[0]))
				.Replace("g1", "_" + g.vertices.IndexOf(children[1]))
				.Replace("g2", "_" + g.vertices.IndexOf(children[2]))
				.Replace("g3", "_" + g.vertices.IndexOf(children[3]));
		}
	}
}