﻿using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexPop : BCVertex
	{
		public BCVertexPop(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public BCVertexPop(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "POP";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexPop(Direction, Positions);
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
			stackbuilder.Pop();

			if (Children.Count > 1)
				throw new ArgumentException("#");
			return Children.FirstOrDefault();
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
			return false;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return "sp();";
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return "sp();";
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return "sp()";
		}
	}
}
