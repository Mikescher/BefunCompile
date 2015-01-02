using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexFullOutput : BCVertex
	{
		public readonly bool ModeInteger; // true = int | false = char
		public BCExpression Value;

		public BCVertexFullOutput(BCDirection d, Vec2i pos, char mode, BCExpression val)
			: base(d, new Vec2i[] { pos })
		{
			ModeInteger = (mode == '.');
			this.Value = val;
		}

		public BCVertexFullOutput(BCDirection d, Vec2i[] pos, char mode, BCExpression val)
			: base(d, pos)
		{
			ModeInteger = (mode == '.');
			this.Value = val;
		}

		public BCVertexFullOutput(BCDirection d, Vec2i[] pos, bool mode, BCExpression val)
			: base(d, pos)
		{
			ModeInteger = mode;
			this.Value = val;
		}

		public override string ToString()
		{
			return string.Format("OUT_{0}({1})", ModeInteger ? "INT" : "CHAR", Value.getRepresentation());
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexFullOutput(direction, positions, ModeInteger, Value);
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return Value.listConstantVariableAccess();
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			return Value.listDynamicVariableAccess();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			if (ModeInteger)
				outbuilder.Append(Value.Calculate(ci));
			else
				outbuilder.Append((char)(Value.Calculate(ci)));

			if (children.Count > 1)
				throw new ArgumentException("#");
			return children.FirstOrDefault();
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

		public override bool isOnlyStackManipulation()
		{
			return false;
		}

		public override string GenerateCode(BCGraph g)
		{
			return string.Format("Console.Out.WriteLine(({0})({1}));", ModeInteger ? "long" : "bool", Value.GenerateCode(g));
		}
	}
}
