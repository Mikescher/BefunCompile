using BefunCompile.Graph.Expression;
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
			return Value.ListConstantVariableAccess();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Value.ListDynamicVariableAccess();
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

		public override bool IsNotGridAccess()
		{
			return Value.IsNotGridAccess();
		}

		public override bool IsNotStackAccess()
		{
			return false;
		}

		public override bool IsNotVariableAccess()
		{
			return Value.IsNotVariableAccess();
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
			return Value.GetVariables();
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			return Enumerable.Empty<int>();
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
