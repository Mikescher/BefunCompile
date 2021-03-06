﻿using BefunCompile.CodeGeneration.Generator;
using BefunCompile.Exceptions;
using BefunCompile.Graph.Optimizations.Unstackify;
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

			if (t == BinaryMathType.DIV && a.IsConstant(0))
				return ExpressionConstant.Create(0);

			//########  0*x  ########

			if (t == BinaryMathType.MUL && a.IsConstant(1))
				return b;

			//########  x*0  ########

			if (t == BinaryMathType.MUL && b.IsConstant(1))
				return a;

			//########  X*1  ########

			if (t == BinaryMathType.MUL && b.IsConstant(1))
				return a;

			//########  1*X  ########

			if (t == BinaryMathType.MUL && a.IsConstant(1))
				return b;

			//########  X/1  ########

			if (t == BinaryMathType.DIV && b.IsConstant(1))
				return a;

			//########  0%X  ########

			if (t == BinaryMathType.MOD && a.IsConstant(0))
				return ExpressionConstant.Create(0);

			//########  X+0  ########

			if (t == BinaryMathType.ADD && b.IsConstant(0))
				return a;

			//########  0+X  ########

			if (t == BinaryMathType.ADD && a.IsConstant(0))
				return b;

			//########  X-0  ########

			if (t == BinaryMathType.SUB && b.IsConstant(0))
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

			//########  Variables if possible as first operand  ########

			if ((t == BinaryMathType.ADD || t == BinaryMathType.MUL) && b is ExpressionVariable && !(a is ExpressionVariable))
			{
				ExpressionBinMath.Create(b, a, t);
			}

			//########  (X - C) [==,<>] C  ########

			if ((t == BinaryMathType.EQ || t == BinaryMathType.NEQ) && a is ExpressionBinMath && (a as ExpressionBinMath).Type == BinaryMathType.SUB && b is ExpressionConstant && (a as ExpressionBinMath).ValueB is ExpressionConstant)
				return ExpressionBinMath.Create((a as ExpressionBinMath).ValueA, ExpressionConstant.Create(b.Calculate(null) + (a as ExpressionBinMath).ValueB.Calculate(null)), t);

			//########  (C - X) [==,<>] C  ########

			if ((t == BinaryMathType.EQ || t == BinaryMathType.NEQ) && a is ExpressionBinMath && (a as ExpressionBinMath).Type == BinaryMathType.SUB && b is ExpressionConstant && (a as ExpressionBinMath).ValueA is ExpressionConstant)
				return ExpressionBinMath.Create((a as ExpressionBinMath).ValueB, ExpressionConstant.Create((a as ExpressionBinMath).ValueA.Calculate(null) - b.Calculate(null)), t);

			//########  (X + C) [==,<>] C  ########

			if ((t == BinaryMathType.EQ || t == BinaryMathType.NEQ) && a is ExpressionBinMath && (a as ExpressionBinMath).Type == BinaryMathType.ADD && b is ExpressionConstant && (a as ExpressionBinMath).ValueB is ExpressionConstant)
				return ExpressionBinMath.Create((a as ExpressionBinMath).ValueA, ExpressionConstant.Create(b.Calculate(null) - (a as ExpressionBinMath).ValueB.Calculate(null)), t);

			//########  (C + X) [==,<>] C  ########

			if ((t == BinaryMathType.EQ || t == BinaryMathType.NEQ) && a is ExpressionBinMath && (a as ExpressionBinMath).Type == BinaryMathType.ADD && b is ExpressionConstant && (a as ExpressionBinMath).ValueA is ExpressionConstant)
				return ExpressionBinMath.Create((a as ExpressionBinMath).ValueB, ExpressionConstant.Create(b.Calculate(null) - (a as ExpressionBinMath).ValueA.Calculate(null)), t);

			//########  C [==,<>] (X - C)  ########

			if ((t == BinaryMathType.EQ || t == BinaryMathType.NEQ) && b is ExpressionBinMath && (b as ExpressionBinMath).Type == BinaryMathType.SUB && a is ExpressionConstant && (b as ExpressionBinMath).ValueB is ExpressionConstant)
				return ExpressionBinMath.Create(ExpressionConstant.Create(a.Calculate(null) + (b as ExpressionBinMath).ValueB.Calculate(null)), (b as ExpressionBinMath).ValueA, t);

			//########  C [==,<>] (C - X)  ########

			if ((t == BinaryMathType.EQ || t == BinaryMathType.NEQ) && b is ExpressionBinMath && (b as ExpressionBinMath).Type == BinaryMathType.SUB && a is ExpressionConstant && (b as ExpressionBinMath).ValueA is ExpressionConstant)
				return ExpressionBinMath.Create(ExpressionConstant.Create((b as ExpressionBinMath).ValueA.Calculate(null) - a.Calculate(null)), (b as ExpressionBinMath).ValueA, t);

			//########  C [==,<>] (X + C)  ########

			if ((t == BinaryMathType.EQ || t == BinaryMathType.NEQ) && b is ExpressionBinMath && (b as ExpressionBinMath).Type == BinaryMathType.ADD && a is ExpressionConstant && (b as ExpressionBinMath).ValueB is ExpressionConstant)
				return ExpressionBinMath.Create(ExpressionConstant.Create(a.Calculate(null) - (b as ExpressionBinMath).ValueB.Calculate(null)), (b as ExpressionBinMath).ValueA, t);

			//########  C [==,<>] (C + X)  ########

			if ((t == BinaryMathType.EQ || t == BinaryMathType.NEQ) && b is ExpressionBinMath && (b as ExpressionBinMath).Type == BinaryMathType.ADD && a is ExpressionConstant && (b as ExpressionBinMath).ValueA is ExpressionConstant)
				return ExpressionBinMath.Create(ExpressionConstant.Create(a.Calculate(null) - (b as ExpressionBinMath).ValueA.Calculate(null)), (b as ExpressionBinMath).ValueB, t);

			return r;
		}

		public override long Calculate(ICalculateInterface ci)
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
				case BinaryMathType.ADD: return "+";
				case BinaryMathType.SUB: return "-";
				case BinaryMathType.MUL: return "*";
				case BinaryMathType.DIV: return "/";
				case BinaryMathType.GT:  return ">";
				case BinaryMathType.LT:  return "<";
				case BinaryMathType.GET: return ">=";
				case BinaryMathType.LET: return "<=";
				case BinaryMathType.MOD: return "%";
				case BinaryMathType.EQ:  return "==";
				case BinaryMathType.NEQ: return "<>";
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
			return Enumerable.Concat(ValueA.GetVariables(), ValueB.GetVariables());
		}

		public bool NeedsLSParen()
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

		public bool NeedsRSParen()
		{
			if (ValueB is ExpressionConstant)
				return false;

			if (ValueB is ExpressionGet)
				return false;

			if (ValueB is ExpressionVariable)
				return false;

			if (ValueB is ExpressionPeek)
				return false;

			if (Type == BinaryMathType.MUL && ValueB is ExpressionBinMath && ((ExpressionBinMath)ValueB).Type == BinaryMathType.MUL)
				return false;

			if (Type == BinaryMathType.ADD && ValueB is ExpressionBinMath && ((ExpressionBinMath)ValueB).Type == BinaryMathType.ADD)
				return false;

			return true;
		}

		public bool RightSideCanBeZero()
		{
			var expr = ValueB as ExpressionConstant;
			if (expr != null && expr.Value != 0) return false;
			return true;
		}

		public override bool IsAlwaysLongReturn()
		{
			switch (Type)
			{
				case BinaryMathType.ADD:
				case BinaryMathType.SUB:
				case BinaryMathType.MUL:
				case BinaryMathType.DIV:
				case BinaryMathType.MOD:
					return true;
				case BinaryMathType.GT:
				case BinaryMathType.LT:
				case BinaryMathType.GET:
				case BinaryMathType.LET:
				case BinaryMathType.NEQ:
				case BinaryMathType.EQ:
					return false;
			}

			throw new CodeGenException();
		}

		public bool ForceLongReturnLeft()
		{
			return false;
		}

		public bool ForceLongReturnRight()
		{
			if (ValueA.IsAlwaysLongReturn()) return false;

			return true;
		}

		public override string GenerateCode(CodeGenerator cg, bool forceLongReturn)
		{
			return cg.GenerateCodeExpressionBinMath(this, forceLongReturn);
		}

		public string GenerateCodeDecision(CodeGenerator cg, bool forceLongReturn)
		{
			return cg.GenerateCodeExpressionBinMathDecision(this, forceLongReturn);
		}

		public override BCModArea GetSideEffects()
		{
			return ValueA.GetSideEffects() | ValueB.GetSideEffects();
		}
		
		public override BCExpression ReplaceUnstackify(UnstackifyValueAccess access)
		{
			return ExpressionBinMath.Create(ValueA.ReplaceUnstackify(access), ValueB.ReplaceUnstackify(access), Type);
		}

		public override bool IsIdentical(BCExpression other)
		{
			var arg = other as ExpressionBinMath;

			if (arg == null) return false;

			return ValueA.IsIdentical(arg.ValueA) && ValueB.IsIdentical(arg.ValueB) && (Type == arg.Type);
		}

		public override bool IsConstant(int value)
		{
			return false;
		}
	}
}
