using BefunCompile.Graph.Expression;
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
			return string.Format("OUT({0})", modeInteger ? "INT" : "CHAR");
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

		public override string GenerateCode(BCGraph g)
		{
			if (modeInteger)
				return "sa(int.Parse(System.Console.ReadLine()));";
			else
				return "sa(System.Console.ReadLine());";
		}
	}
}
