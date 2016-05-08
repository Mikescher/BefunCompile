using BefunCompile.CodeGeneration;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BefunCompile.CodeGeneration.Generator;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexExprOutput : BCVertex
	{
		public readonly bool ModeInteger; // true = int | false = char
		public BCExpression Value;

		public BCVertexExprOutput(BCDirection d, Vec2i pos, char mode, BCExpression val)
			: base(d, new Vec2i[] { pos })
		{
			ModeInteger = (mode == '.');
			this.Value = val;
		}

		public BCVertexExprOutput(BCDirection d, Vec2i[] pos, char mode, BCExpression val)
			: base(d, pos)
		{
			ModeInteger = (mode == '.');
			this.Value = val;
		}

		public BCVertexExprOutput(BCDirection d, Vec2i[] pos, bool mode, BCExpression val)
			: base(d, pos)
		{
			ModeInteger = mode;
			this.Value = val;
		}

		public override string ToString()
		{
			return string.Format("OUT_{0}({1})", ModeInteger ? "INT" : "CHAR", Value.GetRepresentation());
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexExprOutput(Direction, Positions, ModeInteger, Value);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Value.ListConstantVariableAccess();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Value.ListDynamicVariableAccess();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, ICalculateInterface ci)
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

		public override bool IsOutput()
		{
			return true;
		}

		public override bool IsInput()
		{
			return true;
		}

		public override bool IsNotGridAccess()
		{
			return Value.IsNotGridAccess();
		}

		public override bool IsNotStackAccess()
		{
			return Value.IsNotStackAccess();
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

		public override string GenerateCode(OutputLanguage l, BCGraph g)
		{
			return CodeGenerator.GenerateCodeBCVertexExprOutput(l, this, g);
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			if (Value.IsNotStackAccess())
			{
				// nothing
			}
			else
			{
				state.Peek().AddAccess(this, UnstackifyValueAccessType.READ);
			}

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			return new BCVertexExprOutput(Direction, Positions, ModeInteger, access.Single().Value.Replacement);
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexExprOutput;

			if (arg == null) return false;

			return this.ModeInteger == arg.ModeInteger && this.Value.IsIdentical(arg.Value);
		}
	}
}
