﻿using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexPush : BCVertex
	{
		public BCExpression Value;

		public BCVertexPush(BCDirection d, Vec2i pos, BCExpression val)
			: base(d, new Vec2i[] { pos })
		{
			this.Value = val;
		}

		public BCVertexPush(BCDirection d, Vec2i[] pos, BCExpression val)
			: base(d, pos)
		{
			this.Value = val;
		}

		public override string ToString()
		{
			return "PUSH(" + Value + ")";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexPush(Direction, Positions, Value);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Value.listConstantVariableAccess();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Value.listDynamicVariableAccess();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			stackbuilder.Push(Value.Calculate(ci));

			if (Children.Count > 1)
				throw new ArgumentException("#");
			return Children.FirstOrDefault();
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

			if (prerequisite(Value))
			{
				Value = replacement(Value);
				found = true;
			}
			if (Value.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			return found;
		}

		public override bool IsOnlyStackManipulation()
		{
			return Value.isOnlyStackManipulation();
		}

		public override bool IsCodePathSplit()
		{
			return false;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return string.Format("sa({0});", Value.GenerateCodeCSharp(g));
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return string.Format("sa({0});", Value.GenerateCodeC(g));
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return string.Format("sa({0})", Value.GenerateCodePython(g));
		}
	}
}
