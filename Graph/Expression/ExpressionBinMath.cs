using BefunCompile.Graph.Vertex;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph.Expression
{
	public class ExpressionBinMath : BCExpression
	{
		public BCExpression ValueA;
		public BCExpression ValueB;

		public readonly BinaryMathType Type;

		private ExpressionBinMath(BCExpression a, BCExpression b, BinaryMathType t)
		{
			this.ValueA = a;
			this.ValueB = b;
			this.Type = t;
		}

		public static BCExpression Create(BCExpression a, BCExpression b, BinaryMathType t)
		{
			ExpressionBinMath r = new ExpressionBinMath(a, b, t);

			if (a is ExpressionConstant && b is ExpressionConstant)
				return ExpressionConstant.Create(r.Calculate(null));
			else
				return r;
		}

		public override long Calculate(CalculateInterface ci)
		{
			long cA = ValueA.Calculate(ci);
			long cB = ValueB.Calculate(ci);

			switch (Type)
			{
				case BinaryMathType.ADD:
					return cA + cB;
				case BinaryMathType.SUB:
					return cA - cB;
				case BinaryMathType.MUL:
					return cA * cB;
				case BinaryMathType.DIV:
					return (cB == 0) ? 0 : (cA / cB);
				case BinaryMathType.GT:
					return (cA > cB) ? 1 : 0;
				case BinaryMathType.MOD:
					return (cB == 0) ? 0 : (cA % cB);
				default:
					throw new ArgumentException();
			}
		}

		public override string getRepresentation()
		{
			char op;
			switch (Type)
			{
				case BinaryMathType.ADD:
					op = '+';
					break;
				case BinaryMathType.SUB:
					op = '-';
					break;
				case BinaryMathType.MUL:
					op = '*';
					break;
				case BinaryMathType.DIV:
					op = '/';
					break;
				case BinaryMathType.GT:
					op = '>';
					break;
				case BinaryMathType.MOD:
					op = '%';
					break;
				default:
					throw new ArgumentException();
			}
			return "(" + ValueA.getRepresentation() + " " + op.ToString() + " " + ValueB.getRepresentation() + ")";
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return ValueA.listConstantVariableAccess().Concat(ValueB.listConstantVariableAccess());
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			return ValueA.listDynamicVariableAccess().Concat(ValueB.listDynamicVariableAccess());
		}

		public override bool Subsitute(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

			if (prerequisite(ValueA))
			{
				ValueA = replacement(ValueA);
				found = true;
			}
			if (ValueA.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			if (prerequisite(ValueB))
			{
				ValueB = replacement(ValueB);
				found = true;
			}
			if (ValueB.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			return found;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			switch (Type)
			{
				case BinaryMathType.ADD:
					return "((" + ValueA.GenerateCodeCSharp(g) + ")+(" + ValueB.GenerateCodeCSharp(g) + "))";
				case BinaryMathType.SUB:
					return "((" + ValueA.GenerateCodeCSharp(g) + ")-(" + ValueB.GenerateCodeCSharp(g) + "))";
				case BinaryMathType.MUL:
					return "((" + ValueA.GenerateCodeCSharp(g) + ")*(" + ValueB.GenerateCodeCSharp(g) + "))";
				case BinaryMathType.DIV:
					return "td(" + ValueA.GenerateCodeCSharp(g) + "," + ValueB.GenerateCodeCSharp(g) + ")";
				case BinaryMathType.GT:
					return "(((" + ValueA.GenerateCodeCSharp(g) + ")>(" + ValueB.GenerateCodeCSharp(g) + "))?1:0)";
				case BinaryMathType.MOD:
					return "tm(" + ValueA.GenerateCodeCSharp(g) + "," + ValueB.GenerateCodeCSharp(g) + ")";
				default:
					throw new ArgumentException();
			}
		}

		public override string GenerateCodeC(BCGraph g)
		{
			switch (Type)
			{
				case BinaryMathType.ADD:
					return "((" + ValueA.GenerateCodeCSharp(g) + ")+(" + ValueB.GenerateCodeCSharp(g) + "))";
				case BinaryMathType.SUB:
					return "((" + ValueA.GenerateCodeCSharp(g) + ")-(" + ValueB.GenerateCodeCSharp(g) + "))";
				case BinaryMathType.MUL:
					return "((" + ValueA.GenerateCodeCSharp(g) + ")*(" + ValueB.GenerateCodeCSharp(g) + "))";
				case BinaryMathType.DIV:
					return "td(" + ValueA.GenerateCodeCSharp(g) + "," + ValueB.GenerateCodeCSharp(g) + ")";
				case BinaryMathType.GT:
					return "(((" + ValueA.GenerateCodeCSharp(g) + ")>(" + ValueB.GenerateCodeCSharp(g) + "))?1:0)";
				case BinaryMathType.MOD:
					return "tm(" + ValueA.GenerateCodeCSharp(g) + "," + ValueB.GenerateCodeCSharp(g) + ")";
				default:
					throw new ArgumentException();
			}
		}

		public override bool isOnlyStackManipulation()
		{
			return ValueA.isOnlyStackManipulation() && ValueB.isOnlyStackManipulation();
		}
	}
}
