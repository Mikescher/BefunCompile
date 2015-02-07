using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexOutput : BCVertex
	{
		public readonly bool ModeInteger; // true = int | false = char

		public BCVertexOutput(BCDirection d, Vec2i pos, long mode)
			: base(d, new Vec2i[] { pos })
		{
			ModeInteger = (mode == '.');
		}

		public BCVertexOutput(BCDirection d, Vec2i[] pos, long mode)
			: base(d, pos)
		{
			ModeInteger = (mode == '.');
		}

		public BCVertexOutput(BCDirection d, Vec2i[] pos, bool mode)
			: base(d, pos)
		{
			ModeInteger = mode;
		}

		public override string ToString()
		{
			return string.Format("OUT_{0}", ModeInteger ? "INT" : "CHAR");
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexOutput(Direction, Positions, ModeInteger);
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
			var c = stackbuilder.Pop();

			if (ModeInteger)
				outbuilder.Append(c);
			else
				outbuilder.Append((char)c);

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
			return string.Format("System.Console.Out.Write(({0})(sp()));", ModeInteger ? "long" : "char");
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return string.Format("printf(\"{0}\", ({1})(sp()));",
				ModeInteger ? "%lld" : "%c",
				ModeInteger ? "int64" : "char");
		}

		public override string GenerateCodePython(BCGraph g)
		{
			if (ModeInteger)
				return string.Format("print({0},end=\"\",flush=True)", "sp()");
			else
				return string.Format("print(chr({0}),end=\"\",flush=True)", "sp()");
		}
	}
}
