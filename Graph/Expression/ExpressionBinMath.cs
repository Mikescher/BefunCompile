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
			ValueA = a;
			ValueB = b;
			Type = t;
		}

		public static BCExpression Create(BCExpression a, BCExpression b, BinaryMathType t)
		{
			ExpressionBinMath r = new ExpressionBinMath(a, b, t);

			//########  C o C  ########

			if (a is ExpressionConstant && b is ExpressionConstant)
				return ExpressionConstant.Create(r.Calculate(null));

			//########  0/x  ########

			if (t == BinaryMathType.DIV && a is ExpressionConstant && a.Calculate(null) == 0)
				return ExpressionConstant.Create(0);

			//########  0*x  ########

			if (t == BinaryMathType.MUL && a is ExpressionConstant && a.Calculate(null) == 1)
				return b;

			//########  x*0  ########

			if (t == BinaryMathType.MUL && b is ExpressionConstant && b.Calculate(null) == 1)
				return a;

			//########  X*1  ########

			if (t == BinaryMathType.MUL && b is ExpressionConstant && b.Calculate(null) == 1)
				return a;

			//########  1*X  ########

			if (t == BinaryMathType.MUL && a is ExpressionConstant && a.Calculate(null) == 1)
				return b;

			//########  X/1  ########

			if (t == BinaryMathType.DIV && b is ExpressionConstant && b.Calculate(null) == 1)
				return a;

			//########  0%X  ########

			if (t == BinaryMathType.MOD && a is ExpressionConstant && a.Calculate(null) == 0)
				return ExpressionConstant.Create(0);

			//########  X+0  ########

			if (t == BinaryMathType.ADD && b is ExpressionConstant && b.Calculate(null) == 0)
				return a;

			//########  0+X  ########

			if (t == BinaryMathType.ADD && a is ExpressionConstant && a.Calculate(null) == 0)
				return b;

			//########  X-0  ########

			if (t == BinaryMathType.SUB && b is ExpressionConstant && b.Calculate(null) == 0)
				return a;

			//########  X o (X o X) || (X o X) o X  ########

			if ((t == BinaryMathType.ADD || t == BinaryMathType.SUB))
			{
				ExpressionConstant c1 = null;
				ExpressionConstant c2 = null;
				BCExpression x1 = null;
				bool x1_neg = false;

				// C o (? o ?)
				if (a is ExpressionConstant && b is ExpressionBinMath && ((b as ExpressionBinMath).Type == BinaryMathType.ADD || (b as ExpressionBinMath).Type == BinaryMathType.SUB))
				{
					ExpressionBinMath mathExpr = b as ExpressionBinMath;
					c1 = a as ExpressionConstant;

					// C o (C o X)
					if (mathExpr.ValueA is ExpressionConstant)
					{
						if (t == BinaryMathType.SUB)
							c2 = ExpressionConstant.Create(-mathExpr.ValueA.Calculate(null)) as ExpressionConstant;
						else
							c2 = mathExpr.ValueA as ExpressionConstant;

						x1 = mathExpr.ValueB;
						x1_neg = (t != mathExpr.Type);
					}
					// C o (X o C)
					else if (mathExpr.ValueB is ExpressionConstant)
					{
						if (t != mathExpr.Type)
							c2 = ExpressionConstant.Create(-mathExpr.ValueB.Calculate(null)) as ExpressionConstant;
						else
							c2 = mathExpr.ValueA as ExpressionConstant;

						x1 = mathExpr.ValueA;
						x1_neg = (t == BinaryMathType.SUB);
					}
				}
				// (? o ?) o C
				else if (b is ExpressionConstant && a is ExpressionBinMath && ((a as ExpressionBinMath).Type == BinaryMathType.ADD || (a as ExpressionBinMath).Type == BinaryMathType.SUB))
				{
					ExpressionBinMath mathExpr = a as ExpressionBinMath;

					// (C o X) o C
					if (mathExpr.ValueA is ExpressionConstant)
					{
						c1 = mathExpr.ValueA as ExpressionConstant;

						if (t == BinaryMathType.SUB)
							c2 = ExpressionConstant.Create(-b.Calculate(null)) as ExpressionConstant;
						else
							c2 = b as ExpressionConstant;

						x1 = mathExpr.ValueB;
						x1_neg = (mathExpr.Type == BinaryMathType.SUB);
					}
					// (X o C) o C 
					else if (mathExpr.ValueB is ExpressionConstant)
					{
						if (mathExpr.Type == BinaryMathType.SUB)
							c1 = ExpressionConstant.Create(-mathExpr.ValueB.Calculate(null)) as ExpressionConstant;
						else
							c1 = mathExpr.ValueB as ExpressionConstant;

						if (t == BinaryMathType.SUB)
							c2 = ExpressionConstant.Create(-b.Calculate(null)) as ExpressionConstant;
						else
							c2 = b as ExpressionConstant;

						x1 = mathExpr.ValueA;
						x1_neg = false;
					}
				}

				if (c1 != null && c2 != null && x1 != null)
				{
					long new_c = c1.Calculate(null) + c2.Calculate(null);

					BCExpression new_expr;

					if (x1_neg)
					{
						new_expr = ExpressionBinMath.Create(ExpressionConstant.Create(new_c), x1, BinaryMathType.SUB);
					}
					else
					{
						if (new_c < 0)
							new_expr = ExpressionBinMath.Create(x1, ExpressionConstant.Create(-new_c), BinaryMathType.SUB);
						else
							new_expr = ExpressionBinMath.Create(x1, ExpressionConstant.Create(new_c), BinaryMathType.ADD);
					}
					return new_expr;
				}
			}

			//########  C * (C * X)  ########

			if (t == BinaryMathType.MUL && b is ExpressionBinMath && (b as ExpressionBinMath).Type == BinaryMathType.MUL && a is ExpressionConstant && (b as ExpressionBinMath).ValueA is ExpressionConstant)
				return ExpressionBinMath.Create(ExpressionConstant.Create(a.Calculate(null) * (b as ExpressionBinMath).ValueA.Calculate(null)), (b as ExpressionBinMath).ValueB, t);

			//########  C * (X * C)  ########

			if (t == BinaryMathType.MUL && b is ExpressionBinMath && (b as ExpressionBinMath).Type == BinaryMathType.MUL && a is ExpressionConstant && (b as ExpressionBinMath).ValueB is ExpressionConstant)
				return ExpressionBinMath.Create(ExpressionConstant.Create(a.Calculate(null) * (b as ExpressionBinMath).ValueB.Calculate(null)), (b as ExpressionBinMath).ValueA, t);

			//########  (C * X) * C  ########

			if (t == BinaryMathType.MUL && a is ExpressionBinMath && (a as ExpressionBinMath).Type == BinaryMathType.MUL && b is ExpressionConstant && (a as ExpressionBinMath).ValueA is ExpressionConstant)
				return ExpressionBinMath.Create(ExpressionConstant.Create(b.Calculate(null) * (a as ExpressionBinMath).ValueA.Calculate(null)), (a as ExpressionBinMath).ValueB, t);

			//########  (X * C) * C  ########

			if (t == BinaryMathType.MUL && a is ExpressionBinMath && (a as ExpressionBinMath).Type == BinaryMathType.MUL && b is ExpressionConstant && (a as ExpressionBinMath).ValueB is ExpressionConstant)
				return ExpressionBinMath.Create(ExpressionConstant.Create(b.Calculate(null) * (a as ExpressionBinMath).ValueB.Calculate(null)), (a as ExpressionBinMath).ValueA, t);

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
				case BinaryMathType.LT:
					return (cA < cB) ? 1 : 0;
				case BinaryMathType.GET:
					return (cA >= cB) ? 1 : 0;
				case BinaryMathType.LET:
					return (cA <= cB) ? 1 : 0;
				case BinaryMathType.MOD:
					return (cB == 0) ? 0 : (cA % cB);
				default:
					throw new ArgumentException();
			}
		}

		public override string GetRepresentation()
		{
			var op = MathTypeToString(Type);
			return "(" + ValueA.GetRepresentation() + " " + op + " " + ValueB.GetRepresentation() + ")";
		}

		public static string MathTypeToString(BinaryMathType tp)
		{
			string op;
			switch (tp)
			{
				case BinaryMathType.ADD:
					op = "+";
					break;
				case BinaryMathType.SUB:
					op = "-";
					break;
				case BinaryMathType.MUL:
					op = "*";
					break;
				case BinaryMathType.DIV:
					op = "/";
					break;
				case BinaryMathType.GT:
					op = ">";
					break;
				case BinaryMathType.LT:
					op = "<";
					break;
				case BinaryMathType.GET:
					op = ">=";
					break;
				case BinaryMathType.LET:
					op = "<=";
					break;
				case BinaryMathType.MOD:
					op = "%";
					break;
				default:
					throw new ArgumentException();
			}
			return op;
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return ValueA.ListConstantVariableAccess().Concat(ValueB.ListConstantVariableAccess());
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return ValueA.ListDynamicVariableAccess().Concat(ValueB.ListDynamicVariableAccess());
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

		public override IEnumerable<ExpressionVariable> GetVariables()
		{
			return Enumerable.Empty<ExpressionVariable>();
		}

		private bool NeedsLSParen()
		{
			if (ValueA is ExpressionConstant)
				return false;

			if (ValueA is ExpressionGet)
				return false;

			if (ValueA is ExpressionVariable)
				return false;

			if (ValueA is ExpressionPeek)
				return false;

			if (Type == BinaryMathType.MUL && ValueA is ExpressionBinMath && ((ExpressionBinMath)ValueA).Type == BinaryMathType.MUL)
				return false;

			if (Type == BinaryMathType.ADD && ValueA is ExpressionBinMath && ((ExpressionBinMath)ValueA).Type == BinaryMathType.ADD)
				return false;

			if (Type == BinaryMathType.SUB && ValueA is ExpressionBinMath && ((ExpressionBinMath)ValueA).Type == BinaryMathType.SUB)
				return false;

			return true;
		}

		private bool NeedsRSParen()
		{
			if (ValueB is ExpressionConstant)
				return false;

			if (ValueB is ExpressionGet)
				return false;

			if (ValueB is ExpressionVariable)
				return false;

			if (ValueA is ExpressionPeek)
				return false;

			if (Type == BinaryMathType.MUL && ValueB is ExpressionBinMath && ((ExpressionBinMath)ValueB).Type == BinaryMathType.MUL)
				return false;

			if (Type == BinaryMathType.ADD && ValueB is ExpressionBinMath && ((ExpressionBinMath)ValueB).Type == BinaryMathType.ADD)
				return false;

			return true;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			switch (Type)
			{
				case BinaryMathType.ADD:
					return Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + '+' + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen());
				case BinaryMathType.SUB:
					return Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + '-' + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen());
				case BinaryMathType.MUL:
					return Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + '*' + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen());
				case BinaryMathType.DIV:
					return "td(" + ValueA.GenerateCodeCSharp(g) + "," + ValueB.GenerateCodeCSharp(g) + ")";
				case BinaryMathType.GT:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + ">" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen()) + "?1:0";
				case BinaryMathType.LT:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + "<" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen()) + "?1:0";
				case BinaryMathType.GET:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + ">=" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen()) + "?1:0";
				case BinaryMathType.LET:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + "<=" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen()) + "?1:0";
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
					return Paren(ValueA.GenerateCodeC(g), NeedsLSParen()) + '+' + Paren(ValueB.GenerateCodeC(g), NeedsRSParen());
				case BinaryMathType.SUB:
					return Paren(ValueA.GenerateCodeC(g), NeedsLSParen()) + '-' + Paren(ValueB.GenerateCodeC(g), NeedsRSParen());
				case BinaryMathType.MUL:
					return Paren(ValueA.GenerateCodeC(g), NeedsLSParen()) + '*' + Paren(ValueB.GenerateCodeC(g), NeedsRSParen());
				case BinaryMathType.DIV:
					return "td(" + ValueA.GenerateCodeC(g) + "," + ValueB.GenerateCodeC(g) + ")";
				case BinaryMathType.GT:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + ">" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen()) + "?1:0";
				case BinaryMathType.LT:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + "<" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen()) + "?1:0";
				case BinaryMathType.GET:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + ">=" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen()) + "?1:0";
				case BinaryMathType.LET:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + "<=" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen()) + "?1:0";
				case BinaryMathType.MOD:
					return "tm(" + ValueA.GenerateCodeC(g) + "," + ValueB.GenerateCodeC(g) + ")";
				default:
					throw new ArgumentException();
			}
		}

		public override string GenerateCodePython(BCGraph g)
		{
			switch (Type)
			{
				case BinaryMathType.ADD:
					return Paren(ValueA.GenerateCodePython(g), NeedsLSParen()) + '+' + Paren(ValueB.GenerateCodePython(g), NeedsRSParen());
				case BinaryMathType.SUB:
					return Paren(ValueA.GenerateCodePython(g), NeedsLSParen()) + '-' + Paren(ValueB.GenerateCodePython(g), NeedsRSParen());
				case BinaryMathType.MUL:
					return Paren(ValueA.GenerateCodePython(g), NeedsLSParen()) + '*' + Paren(ValueB.GenerateCodePython(g), NeedsRSParen());
				case BinaryMathType.DIV:
					return "td(" + ValueA.GenerateCodePython(g) + "," + ValueB.GenerateCodePython(g) + ")";
				case BinaryMathType.GT:
					return "(1)if(" + Paren(ValueA.GenerateCodePython(g), NeedsLSParen()) + ">" + Paren(ValueB.GenerateCodePython(g), NeedsRSParen()) + ")else(0)";
				case BinaryMathType.LT:
					return "(1)if(" + Paren(ValueA.GenerateCodePython(g), NeedsLSParen()) + "<" + Paren(ValueB.GenerateCodePython(g), NeedsRSParen()) + ")else(0)";
				case BinaryMathType.GET:
					return "(1)if(" + Paren(ValueA.GenerateCodePython(g), NeedsLSParen()) + ">=" + Paren(ValueB.GenerateCodePython(g), NeedsRSParen()) + ")else(0)";
				case BinaryMathType.LET:
					return "(1)if(" + Paren(ValueA.GenerateCodePython(g), NeedsLSParen()) + "<=" + Paren(ValueB.GenerateCodePython(g), NeedsRSParen()) + ")else(0)";
				case BinaryMathType.MOD:
					return "tm(" + ValueA.GenerateCodePython(g) + "," + ValueB.GenerateCodePython(g) + ")";
				default:
					throw new ArgumentException();
			}
		}

		public string GenerateDecisionCodeCSharp(BCGraph g)
		{
			switch (Type)
			{
				case BinaryMathType.ADD:
					return "(" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + '+' + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen()) + ")!=0";
				case BinaryMathType.SUB:
					return Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + "!=" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen());
				case BinaryMathType.MUL:
					return "(" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + '*' + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen()) + ")!=0";
				case BinaryMathType.DIV:
					return "td(" + ValueA.GenerateCodeCSharp(g) + "," + ValueB.GenerateCodeCSharp(g) + ")!=0";
				case BinaryMathType.GT:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + ">" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen());
				case BinaryMathType.LT:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + "<" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen());
				case BinaryMathType.GET:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + ">=" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen());
				case BinaryMathType.LET:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + "<=" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen());
				case BinaryMathType.MOD:
					return "tm(" + ValueA.GenerateCodeCSharp(g) + "," + ValueB.GenerateCodeCSharp(g) + ")!=0";
				default:
					throw new ArgumentException();
			}
		}

		public string GenerateDecisionCodeC(BCGraph g)
		{
			switch (Type)
			{
				case BinaryMathType.ADD:
					return "(" + Paren(ValueA.GenerateCodeC(g), NeedsLSParen()) + '+' + Paren(ValueB.GenerateCodeC(g), NeedsRSParen()) + ")!=0";
				case BinaryMathType.SUB:
					return Paren(ValueA.GenerateCodeC(g), NeedsLSParen()) + "!=" + Paren(ValueB.GenerateCodeC(g), NeedsRSParen());
				case BinaryMathType.MUL:
					return "(" + Paren(ValueA.GenerateCodeC(g), NeedsLSParen()) + '*' + Paren(ValueB.GenerateCodeC(g), NeedsRSParen()) + ")!=0";
				case BinaryMathType.DIV:
					return "td(" + ValueA.GenerateCodeC(g) + "," + ValueB.GenerateCodeC(g) + ")!=0";
				case BinaryMathType.GT:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + ">" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen());
				case BinaryMathType.LT:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + "<" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen());
				case BinaryMathType.GET:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + ">=" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen());
				case BinaryMathType.LET:
					return "" + Paren(ValueA.GenerateCodeCSharp(g), NeedsLSParen()) + "<=" + Paren(ValueB.GenerateCodeCSharp(g), NeedsRSParen());
				case BinaryMathType.MOD:
					return "tm(" + ValueA.GenerateCodeC(g) + "," + ValueB.GenerateCodeC(g) + ")!=0";
				default:
					throw new ArgumentException();
			}
		}

		public string GenerateDecisionCodePython(BCGraph g)
		{
			switch (Type)
			{
				case BinaryMathType.ADD:
					return "(" + Paren(ValueA.GenerateCodePython(g), NeedsLSParen()) + '+' + Paren(ValueB.GenerateCodePython(g), NeedsRSParen()) + ")!=0";
				case BinaryMathType.SUB:
					return Paren(ValueA.GenerateCodePython(g), NeedsLSParen()) + "!=" + Paren(ValueB.GenerateCodePython(g), NeedsRSParen());
				case BinaryMathType.MUL:
					return "(" + Paren(ValueA.GenerateCodePython(g), NeedsLSParen()) + '*' + Paren(ValueB.GenerateCodePython(g), NeedsRSParen()) + ")!=0";
				case BinaryMathType.DIV:
					return "td(" + ValueA.GenerateCodePython(g) + "," + ValueB.GenerateCodePython(g) + ")!=0";
				case BinaryMathType.GT:
					return "" + Paren(ValueA.GenerateCodePython(g), NeedsLSParen()) + ">" + Paren(ValueB.GenerateCodePython(g), NeedsRSParen());
				case BinaryMathType.LT:
					return "" + Paren(ValueA.GenerateCodePython(g), NeedsLSParen()) + "<" + Paren(ValueB.GenerateCodePython(g), NeedsRSParen());
				case BinaryMathType.GET:
					return "" + Paren(ValueA.GenerateCodePython(g), NeedsLSParen()) + ">=" + Paren(ValueB.GenerateCodePython(g), NeedsRSParen());
				case BinaryMathType.LET:
					return "" + Paren(ValueA.GenerateCodePython(g), NeedsLSParen()) + "<=" + Paren(ValueB.GenerateCodePython(g), NeedsRSParen());
				case BinaryMathType.MOD:
					return "tm(" + ValueA.GenerateCodePython(g) + "," + ValueB.GenerateCodePython(g) + ")!=0";
				default:
					throw new ArgumentException();
			}
		}

		public override bool IsNotGridAccess()
		{
			return ValueA.IsNotGridAccess() && ValueB.IsNotGridAccess();
		}

		public override bool IsNotStackAccess()
		{
			return ValueA.IsNotStackAccess() && ValueB.IsNotStackAccess();
		}

		public override bool IsNotVariableAccess()
		{
			return ValueA.IsNotVariableAccess() && ValueB.IsNotVariableAccess();
		}
	}
}
