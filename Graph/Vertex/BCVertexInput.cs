﻿using BefunCompile.Graph.Expression;
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
			return new BCVertexInput(direction, positions, modeInteger);
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
			throw new System.NotImplementedException();
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return false;
		}

		public override bool isOnlyStackManipulation()
		{
			return true;
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
	}
}
