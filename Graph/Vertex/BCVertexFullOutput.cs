﻿using BefunCompile.Graph.Expression;
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
			return new BCVertexFullOutput(Direction, Positions, ModeInteger, Value);
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
			if (ModeInteger)
				outbuilder.Append(Value.Calculate(ci));
			else
				outbuilder.Append((char)(Value.Calculate(ci)));

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
			return false;
		}

		public override bool IsCodePathSplit()
		{
			return false;
		}

		public override bool IsBlock()
		{
			return false;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return string.Format("System.Console.Out.Write(({0})({1}));",
				ModeInteger ? "long" : "char",
				Value.GenerateCodeCSharp(g));
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return string.Format("printf(\"{0}\", ({1})({2}));",
				ModeInteger ? "%lld" : "%c",
				ModeInteger ? "int64" : "char",
				Value.GenerateCodeC(g));
		}

		public override string GenerateCodePython(BCGraph g)
		{
			if (ModeInteger)
				return string.Format("print({0},end=\"\",flush=True)", Value.GenerateCodePython(g));
			else
				return string.Format("print(chr({0}),end=\"\",flush=True)", Value.GenerateCodePython(g));
		}
	}
}
