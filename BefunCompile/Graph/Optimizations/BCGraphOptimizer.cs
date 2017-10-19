using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph.Optimizations
{
	public class BCGraphOptimizer
	{
		private class OptimizerStep
		{
			public string Name;
			public Func<BCGraph, bool> Action;
			public HashSet<int> Scope;
			public string LastRunInfo;

			public bool Run(BCGraph g) { LastRunInfo = ""; return Action(g); }
		}

		public const int MAX_OPTIMIZATIONS_PER_LEVEL = 1000;

		private delegate bool StepAction(BCGraph g, out string info);

		private List<OptimizerStep> AllSteps = new List<OptimizerStep>();

		private readonly Func<long, long, long> _gridGet;

		public BCGraphOptimizer(Func<long, long, long> gridGetter)
		{
			_gridGet = gridGetter;

			#region O:1 Minimize
			{
				Add("MinimizeNOP", MinimizeNOP, new[] { 1 });
				Add("MinimizeNOPSplit", MinimizeNOPSplit, new[] { 1 });
				Add("MinimizeNOPTail", MinimizeNOPTail, new[] { 1 });
				Add("MinimizeNOPDecision", MinimizeNOPDecision, new[] { 1 });
			}
			#endregion

			#region O:2 Substitute
			{
				var rule1 = new BCModRule();
				rule1.AddPreq<BCVertexExpression>();
				rule1.AddPreq<BCVertexExpression>();
				rule1.AddPreq<BCVertexBinaryMath>();
				rule1.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ExpressionBinMath.Create(((BCVertexExpression)l[0]).Expression, ((BCVertexExpression)l[1]).Expression, ((BCVertexBinaryMath)l[2]).MathType)));
				Add("CreateExpressionFromBinMath", rule1, new[] { 2, 3, 4, 5, 6 });

				var rule2 = new BCModRule();
				rule2.AddPreq<BCVertexExpression>();
				rule2.AddPreq<BCVertexNot>();
				rule2.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ExpressionNot.Create(((BCVertexExpression)l[0]).Expression)));
				Add("IncludeNotInExpression", rule2, new[] { 2, 3, 4, 5, 6 });

				var rule3 = new BCModRule();
				rule3.AddPreq<BCVertexExpression>(v => !v.IsStateModifying());
				rule3.AddPreq<BCVertexPop>();
				Add("RemoveDiscardedExpression", rule3, new[] { 2, 3, 4, 5, 6 });

				var rule4 = new BCModRule();
				rule4.AddPreq<BCVertexSwap>();
				rule4.AddPreq<BCVertexSwap>();
				Add("RemoveDoubleSwap", rule4, new[] { 2, 3, 4, 5, 6 });

				var rule5 = new BCModRule();
				rule5.AddPreq<BCVertexExpression>(v => !v.Expression.IsStackAccess());
				rule5.AddPreq<BCVertexDup>();
				rule5.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ((BCVertexExpression)l[0]).Expression));
				rule5.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ((BCVertexExpression)l[0]).Expression));
				Add("DuplicateExpressionInsteadOfResult", rule5, new[] { 2, 3, 4, 5, 6 });

				var rule6 = new BCModRule();
				rule6.AddPreq<BCVertexExpression>(v => !v.Expression.IsStackAccess());
				rule6.AddPreq<BCVertexExpression>(v => !v.Expression.IsStackAccess());
				rule6.AddPreq<BCVertexSwap>();
				rule6.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ((BCVertexExpression)l[1]).Expression));
				rule6.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ((BCVertexExpression)l[0]).Expression));
				Add("SwapExpressionsInsteadOfResults", rule6, new[] { 2, 3, 4, 5, 6 });

				var rule7_1 = new BCModRule();
				rule7_1.AddPreq<BCVertexSwap>();
				rule7_1.AddPreq<BCVertexBinaryMath>(v => v.MathType == BinaryMathType.GT);
				rule7_1.AddRep((l, p) => new BCVertexBinaryMath(BCDirection.UNKNOWN, p, BinaryMathType.LT));
				Add("SwapGreaterThanToLessThan", rule7_1, new[] { 2, 3, 4, 5, 6 });

				var rule7_2 = new BCModRule();
				rule7_2.AddPreq<BCVertexSwap>();
				rule7_2.AddPreq<BCVertexBinaryMath>(v => v.MathType == BinaryMathType.LT);
				rule7_2.AddRep((l, p) => new BCVertexBinaryMath(BCDirection.UNKNOWN, p, BinaryMathType.GT));
				Add("SwapLessThanToGreaterThan", rule7_2, new[] { 2, 3, 4, 5, 6 });

				var rule7_3 = new BCModRule();
				rule7_3.AddPreq<BCVertexSwap>();
				rule7_3.AddPreq<BCVertexBinaryMath>(v => v.MathType == BinaryMathType.GET);
				rule7_3.AddRep((l, p) => new BCVertexBinaryMath(BCDirection.UNKNOWN, p, BinaryMathType.LET));
				Add("SwapGreaterEqualsThanToLessEqualsThan", rule7_3, new[] { 2, 3, 4, 5, 6 });

				var rule7_4 = new BCModRule();
				rule7_4.AddPreq<BCVertexSwap>();
				rule7_4.AddPreq<BCVertexBinaryMath>(v => v.MathType == BinaryMathType.LET);
				rule7_4.AddRep((l, p) => new BCVertexBinaryMath(BCDirection.UNKNOWN, p, BinaryMathType.GET));
				Add("SwapLessEqualsThanToGreaterEqualsThan", rule7_4, new[] { 2, 3, 4, 5, 6 });
			}
			#endregion

			#region O:3 Flatten
			{
				var rule1 = new BCModRule();
				rule1.AddPreq<BCVertexExpression>();
				rule1.AddPreq<BCVertexExpression>(d => !d.IsStackRead());
				rule1.AddPreq<BCVertexGet>();
				rule1.AddRep((l, p) => new BCVertexExprGet(BCDirection.UNKNOWN, p, ((BCVertexExpression)l[0]).Expression, ((BCVertexExpression)l[1]).Expression));
				Add("CreateExprGetFromRawGet", rule1, new[] { 3, 4, 5, 6 });

				var rule2 = new BCModRule();
				rule2.AddPreq<BCVertexExpression>(d => !d.Expression.IsStackAccess());
				rule2.AddPreq<BCVertexExpression>(d => !d.Expression.IsStackAccess());
				rule2.AddPreq<BCVertexSet>();
				rule2.AddRep((l, p) => new BCVertexExprPopSet(BCDirection.UNKNOWN, p, ((BCVertexExpression)l[0]).Expression, ((BCVertexExpression)l[1]).Expression));
				Add("CreateExprSetFromRawGet", rule2, new[] { 3, 4, 5, 6 });

				var rule3 = new BCModRule();
				rule3.AddPreq<BCVertexExpression>();
				rule3.AddPreq<BCVertexExprPopSet>();
				rule3.AddRep((l, p) => new BCVertexExprSet(BCDirection.UNKNOWN, p, ((BCVertexExprPopSet)l[1]).X, ((BCVertexExprPopSet)l[1]).Y, ((BCVertexExpression)l[0]).Expression));
				Add("CreateExprSetFromExpressionAndPopSet", rule3, new[] { 3, 4, 5, 6 });

				var rule4 = new BCModRule();
				rule4.AddPreq<BCVertexExprGet>();
				rule4.AddPreq<BCVertexDup>();
				rule4.AddRep((l, p) => l[0].Duplicate());
				rule4.AddRep((l, p) => { var v = l[0].Duplicate(); v.Positions = p; return v; });
				Add("FlattenExprGetWithDuplicate", rule4, new[] { 3, 4, 5, 6 });

				var rule5 = new BCModRule(false, false, false);
				rule5.AddPreq(v => !v.IsCodePathSplit() &&  v.IsStackAccess());
				rule5.AddPreq(v => !v.IsCodePathSplit() && !v.IsStackAccess());
				rule5.AddRep((l, p) => l[1].Duplicate());
				rule5.AddRep((l, p) => l[0].Duplicate());
				rule5.AddCond(d => BCModAreaHelper.CanSwap(d[0].GetSideEffects(), d[1].GetSideEffects()));
				Add("SwapNonStackAccessBack", rule5, new[] { 3, 4, 5, 6 });

				var rule6 = new BCModRule();
				rule6.AddPreq<BCVertexExprGet>();
				rule6.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ((BCVertexExprGet)l[0]).ToExpression()));
				Add("ConvertRawGetToExprGet", rule6, new[] { 3, 4, 5, 6 });

				var rule7 = new BCModRule();
				rule7.AddPreq<BCVertexExpression>();
				rule7.AddPreq<BCVertexOutput>();
				rule7.AddRep((l, p) => new BCVertexExprOutput(BCDirection.UNKNOWN, p, (l[1] as BCVertexOutput).ModeInteger, (l[0] as BCVertexExpression).Expression));
				Add("CombineExpressionAndOutput", rule7, new[] { 3, 4, 5, 6 });

				var rule8 = new BCModRule();
				rule8.AddPreq<BCVertexExprOutput>(v => (v.Value as ExpressionConstant)?.IsSimpleASCIIChar() ?? false);
				rule8.AddPreq<BCVertexExprOutput>(v => (v.Value as ExpressionConstant)?.IsSimpleASCIIChar() ?? false);
				rule8.AddRep((l, p) => new BCVertexStringOutput(BCDirection.UNKNOWN, p, ((ExpressionConstant)((BCVertexExprOutput)l[0]).Value).AsSimpleASCIIChar() + "" + ((ExpressionConstant)((BCVertexExprOutput)l[1]).Value).AsSimpleASCIIChar()));
				Add("CombineTwoExprOutputToStringOutput", rule8, new[] { 3, 4, 5, 6 });

				var rule9 = new BCModRule();
				rule9.AddPreq<BCVertexExprOutput>(v => (v.Value as ExpressionConstant)?.IsSimpleASCIIChar() ?? false);
				rule9.AddPreq<BCVertexStringOutput>();
				rule9.AddRep((l, p) => new BCVertexStringOutput(BCDirection.UNKNOWN, p, ((ExpressionConstant)((BCVertexExprOutput)l[0]).Value).AsSimpleASCIIChar() + ((BCVertexStringOutput)l[1]).Value));
				Add("CombineExprOutputAndStringOutput", rule9, new[] { 3, 4, 5, 6 });

				var rule10 = new BCModRule();
				rule10.AddPreq<BCVertexStringOutput>();
				rule10.AddPreq<BCVertexExprOutput>(v => (v.Value as ExpressionConstant)?.IsSimpleASCIIChar() ?? false);
				rule10.AddRep((l, p) => new BCVertexStringOutput(BCDirection.UNKNOWN, p, ((BCVertexStringOutput)l[0]).Value + ((ExpressionConstant)((BCVertexExprOutput)l[1]).Value).AsSimpleASCIIChar()));
				Add("CombineStringOutputAndExprOutput", rule10, new[] { 3, 4, 5, 6 });

				Add("IntegrateDecisions", IntegrateDecisions, new[] { 3, 4, 5, 6 });

				var rule11 = new BCModRule(false, true, true);
				rule11.AddPreq<BCVertexNot>();
				rule11.AddPreq<BCVertexDecision>();
				rule11.AddRep((l, p) => new BCVertexDecision(BCDirection.UNKNOWN, p, ((BCVertexDecision)l[1]).EdgeTrue, ((BCVertexDecision)l[1]).EdgeFalse));
				Add("SwapDecisionsChildren", rule11, new[] { 3, 4, 5, 6 });
			}
			#endregion

			#region O:4 Variablize
			{
				Add("SubstituteConstMemoryAccess", SubstituteConstMemoryAccess, new[] { 4, 5, 6 });

				BCModRule combRule1 = new BCModRule();
				combRule1.AddPreq<BCVertexExpression>(p => !p.Expression.IsStackAccess());
				combRule1.AddPreq<BCVertexBinaryMath>();
				combRule1.AddRep((l, p) => new BCVertexExprPopBinaryMath(BCDirection.UNKNOWN, p, ((BCVertexExpression)l[0]).Expression, ((BCVertexBinaryMath)l[1]).MathType));
				Add("MergeExpressionIntoBinMath", combRule1, new[] { 4, 5, 6 });

				BCModRule combRule2 = new BCModRule(false);
				combRule2.AddPreq<BCVertexDup>();
				combRule2.AddPreq<BCVertexExprPopBinaryMath>(p => !p.SecondExpression.IsStackAccess());
				combRule2.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ExpressionPeek.Create(), ((BCVertexExprPopBinaryMath)l[1]).MathType, ((BCVertexExprPopBinaryMath)l[1]).SecondExpression));
				Add("DupAndPopBinMathToPeekBinMath", combRule2, new[] { 4, 5, 6 });
				
				BCModRule combRule5 = new BCModRule(true, true);
				combRule5.AddPreq<BCVertexExpression>();
				combRule5.AddPreq<BCVertexDecision>();
				combRule5.AddRep((l, p) => new BCVertexExprDecision(BCDirection.UNKNOWN, p, (l[1] as BCVertexDecision).EdgeTrue, (l[1] as BCVertexDecision).EdgeFalse, (l[0] as BCVertexExpression).Expression));
				Add("CombineExprAndDecisionToExprDecision", combRule5, new[] { 4, 5, 6 });

				BCModRule combRule6 = new BCModRule();
				combRule6.AddPreq<BCVertexExpression>();
				combRule6.AddPreq<BCVertexExprPopBinaryMath>(p => !p.SecondExpression.IsStackAccess());
				combRule6.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ExpressionBinMath.Create((l[0] as BCVertexExpression).Expression, (l[1] as BCVertexExprPopBinaryMath).SecondExpression, (l[1] as BCVertexExprPopBinaryMath).MathType)));
				Add("MergeExpressionIntoPopBinMath", combRule6, new[] { 4, 5, 6 });

				BCModRule combRule7 = new BCModRule();
				combRule7.AddPreq<BCVertexDup>();
				combRule7.AddPreq<BCVertexVarSet>();
				combRule7.AddRep((l, p) => new BCVertexExprVarSet(BCDirection.UNKNOWN, p, (l[1] as BCVertexVarSet).Variable, ExpressionPeek.Create()));
				Add("CombineDupAndVarSetToPeekVarSet", combRule7, new[] { 4, 5, 6 });

				BCModRule nopRule1 = new BCModRule();
				nopRule1.AddPreq<BCVertexExprPopBinaryMath>(p => p.MathType == BinaryMathType.MUL && p.SecondExpression.IsConstant(0));
				nopRule1.AddRep((l, p) => new BCVertexPop(BCDirection.UNKNOWN, p));
				nopRule1.AddRep((l, p) => new BCVertexExpression(BCDirection.UNKNOWN, p, ExpressionConstant.Create(0)));
				Add("MakeMultZeroConstant", nopRule1, new[] { 4, 5, 6 });
				
				Add("RemovePredeterminedDecisions_0", RemovePredeterminedDecisions_0, new[] { 4, 5, 6 });

				Add("RemovePredeterminedDecisions_1", RemovePredeterminedDecisions_1, new[] { 4, 5 ,6 });

				var condRule1 = new BCModRule(false, true, false);
				condRule1.AddPreq<BCVertexExprDecision>(p => (p.Value is ExpressionBinMath) && !BinaryMathTypeHelper.IsNativeBoolReturn(((ExpressionBinMath)p.Value).Type));
				condRule1.AddRep((l, p) => new BCVertexExprDecision(l[0].Direction, p, ((BCVertexExprDecision)l[0]).EdgeTrue, ((BCVertexExprDecision)l[0]).EdgeFalse, ExpressionBinMath.Create(((BCVertexExprDecision)l[0]).Value, ExpressionConstant.Create(0), BinaryMathType.NEQ)));
				Add("ExprDecisionWithBoolResult", condRule1, new[] { 4, 5, 6 });
			}
			#endregion

			#region O:5 Unstackify
			{
				Add("Unstackify", Unstackify, new[] { 5 });
			}
			#endregion

			#region O:6 Nopify
			{
				var rule1 = new BCModRule(false);
				rule1.AddPreq<BCVertexExprVarSet>(v => v.Variable == (v.Value as ExpressionVariable));
				rule1.AddRep((l, p) => new BCVertexNOP(BCDirection.UNKNOWN, p));
				Add("RemoveIdentityVarAsignment", rule1, new[] { 6 });

				Add("RemoveUnusedVariables", RemoveUnusedVariables, new[] { 6, 7, 8 });

				var ruleMinimize = new BCModRule(false);
				ruleMinimize.AddPreq<BCVertexNOP>();
				Add("RemoveGenericNOPs", ruleMinimize, new[] { 6, 7 });
			}
			#endregion

			#region O:7 Combine
			{
				var ruleSubstitute = new BCModRule(false);
				ruleSubstitute.AddPreq(v => v.Children.Count <= 1 && !(v is BCVertexBlock));
				ruleSubstitute.AddRep((l, p) => new BCVertexBlock(BCDirection.UNKNOWN, p, l[0]));
				Add("ConvertNodeToBlock", ruleSubstitute, new[] { 7, 8 });

				var ruleCombine = new BCModRule(false);
				ruleCombine.AddPreq<BCVertexBlock>();
				ruleCombine.AddPreq<BCVertexBlock>();
				ruleCombine.AddRep((l, p) => new BCVertexBlock(BCDirection.UNKNOWN, p, l[0] as BCVertexBlock, l[1] as BCVertexBlock));
				Add("CombineTwoBlocks", ruleCombine, new[] { 7, 8 });
			}
			#endregion

			#region O:8 Reduce
			{
				Add("ReplaceVariableIntializer", ReplaceVariableIntializer, new[] { 8 });
				Add("CombineIdenticalBlocks", CombineIdenticalBlocks, new[] { 8 });

				var ruleRepl1 = new BCModRule(true, true);
				ruleRepl1.AddPreq<BCVertexBlock>();
				ruleRepl1.AddPreq<BCVertexDecision>();
				ruleRepl1.AddRep((l, p) => new BCVertexDecisionBlock(BCDirection.UNKNOWN, l[0] as BCVertexBlock, l[1] as BCVertexDecision));
				Add("IntegrateRawDecisionIntoBlock", ruleRepl1, new[] { 8 });

				var ruleRepl2 = new BCModRule(true, true);
				ruleRepl2.AddPreq<BCVertexBlock>();
				ruleRepl2.AddPreq<BCVertexExprDecision>();
				ruleRepl2.AddRep((l, p) => new BCVertexExprDecisionBlock(BCDirection.UNKNOWN, l[0] as BCVertexBlock, l[1] as BCVertexExprDecision));
				Add("IntegrateExprDecisionIntoBlock", ruleRepl2, new[] { 8 });
				
			}
			#endregion
		}

		#region OptimizerMethods

		private bool MinimizeNOP(BCGraph g)
		{
			bool found = false;

			List<BCVertex> removed = new List<BCVertex>();
			foreach (var vertex in g.Vertices)
			{
				if (!(vertex is BCVertexNOP))
					continue;

				if (vertex.Children.Contains(vertex))
					continue;

				if (vertex.Parents.Contains(vertex))
					continue;

				if (vertex.Parents.Count == 1 && vertex.Children.Count == 1 && vertex.Parents[0].Children.Count == 1)
				{
					found = true;

					BCVertex prev = vertex.Parents[0];
					BCVertex next = vertex.Children[0];

					removed.Add(vertex);

					vertex.Children.Clear();
					vertex.Parents.Clear();

					prev.Children.Remove(vertex);
					next.Parents.Remove(vertex);

					prev.Children.Add(next);
					next.Parents.Add(prev);

					next.Positions = next.Positions.Concat(vertex.Positions).Distinct().ToArray();

					if (vertex == g.Root) g.Root = next;
				}
			}

			foreach (var rv in removed)
			{
				g.Vertices.Remove(rv);
			}

			return found;
		}

		private bool MinimizeNOPSplit(BCGraph g)
		{
			bool found = false;

			List<BCVertex> removed = new List<BCVertex>();
			foreach (var vertex in g.Vertices)
			{
				if (!(vertex is BCVertexNOP))
					continue;

				if (vertex.Parents.Count > 1 && vertex.Children.Count == 1 && vertex.Parents.All(p => !p.IsCodePathSplit()) && vertex.Parents.All(p => p != vertex) && vertex.Children.All(p => p != vertex))
				{
					found = true;

					BCVertex[] prev = vertex.Parents.ToArray();
					BCVertex next = vertex.Children[0];

					removed.Add(vertex);
					vertex.Children.Clear();
					vertex.Parents.Clear();

					next.Parents.Remove(vertex);

					foreach (var pvertex in prev)
					{
						pvertex.Children.Remove(vertex);

						pvertex.Children.Add(next);
						next.Parents.Add(pvertex);
					}

					next.Positions = next.Positions.Concat(vertex.Positions).Distinct().ToArray();

					if (vertex == g.Root) g.Root = next;
				}
			}

			foreach (var rv in removed)
			{
				g.Vertices.Remove(rv);
			}

			return found;
		}

		private bool MinimizeNOPTail(BCGraph g)
		{
			bool found = false;

			if (g.Root is BCVertexNOP && g.Root.Parents.Count == 0 && g.Root.Children.Count == 1)
			{
				found = true;

				BCVertex vertex = g.Root;

				g.Vertices.Remove(vertex);

				vertex.Children[0].Positions = vertex.Children[0].Positions.Concat(vertex.Positions).ToArray();

				vertex.Children[0].Parents.Remove(vertex);
				g.Root = vertex.Children[0];
			}

			List<BCVertex> removed = new List<BCVertex>();
			foreach (var vertex in g.Vertices)
			{
				if (!(vertex is BCVertexNOP))
					continue;

				if (vertex.Parents.Count == 1 && vertex.Children.Count == 0 && vertex.Parents[0].Children.Count == 1 && vertex != g.Root)
				{
					found = true;

					BCVertex prev = vertex.Parents[0];

					removed.Add(vertex);
					vertex.Children.Clear();
					vertex.Parents.Clear();

					prev.Positions = prev.Positions.Concat(vertex.Positions).Distinct().ToArray();

					prev.Children.Remove(vertex);
				}
			}

			foreach (var rv in removed)
			{
				g.Vertices.Remove(rv);
			}

			return found;
		}

		private bool MinimizeNOPDecision(BCGraph g)
		{
			bool found = false;

			List<BCVertex> removed = new List<BCVertex>();
			foreach (var vertex in g.Vertices)
			{
				if (!(vertex is BCVertexNOP))
					continue;

				if (vertex.Parents.Count == 1 && vertex.Children.Count == 1 && vertex.Parents[0] is BCVertexDecision)
				{
					found = true;

					BCVertexDecision prev = (BCVertexDecision)vertex.Parents[0];
					BCVertex next = vertex.Children[0];

					bool isDTrue = (prev.EdgeTrue == vertex);

					removed.Add(vertex);
					vertex.Children.Clear();
					vertex.Parents.Clear();

					prev.Children.Remove(vertex);
					next.Parents.Remove(vertex);

					prev.Children.Add(next);
					next.Parents.Add(prev);

					next.Positions = next.Positions.Concat(vertex.Positions).Distinct().ToArray();

					if (isDTrue)
						prev.EdgeTrue = next;
					else
						prev.EdgeFalse = next;

					if (vertex == g.Root) g.Root = next;
				}
			}

			foreach (var rv in removed)
			{
				g.Vertices.Remove(rv);
			}

			return found;
		}

		private bool IntegrateDecisions(BCGraph g)
		{
			var rule = new BCModRule();
			rule.AddPreq<BCVertexExpression>();
			rule.AddPreq<BCVertexDecision>();

			var chain = rule.GetMatchingChain(g);

			if (chain == null)
				return false;

			var prev = chain[0].Parents.ToList();
			var nextTrue = ((BCVertexDecision)chain[1]).EdgeTrue;
			var nextFalse = ((BCVertexDecision)chain[1]).EdgeFalse;

			if (prev.Any(p => p is BCVertexExprDecision))
				return false;

			if (chain[1].Parents.Count > 1)
				return false;

			chain[0].Children.Clear();
			chain[0].Parents.Clear();
			g.Vertices.Remove(chain[0]);

			chain[1].Children.Clear();
			chain[1].Parents.Clear();
			g.Vertices.Remove(chain[1]);

			var newnode = new BCVertexExprDecision(BCDirection.UNKNOWN, chain.SelectMany(p => p.Positions).ToArray(), nextTrue, nextFalse, ((BCVertexExpression)chain[0]).Expression);

			g.Vertices.Add(newnode);

			nextTrue.Parents.Remove(chain[1]);
			newnode.Children.Add(nextTrue);
			nextTrue.Parents.Add(newnode);

			nextFalse.Parents.Remove(chain[1]);
			newnode.Children.Add(nextFalse);
			nextFalse.Parents.Add(newnode);

			foreach (var p in prev)
			{
				p.Children.Remove(chain[0]);
				p.Children.Add(newnode);
				newnode.Parents.Add(p);

				if (p is IDecisionVertex)
				{
					if ((p as IDecisionVertex).EdgeTrue == chain[0])
						(p as IDecisionVertex).EdgeTrue = newnode;
					if ((p as IDecisionVertex).EdgeFalse == chain[0])
						(p as IDecisionVertex).EdgeFalse = newnode;
				}
			}

			if (g.Root == chain[0]) g.Root = newnode;

			return true;
		}

		private bool SubstituteConstMemoryAccess(BCGraph g)
		{
			var dvs = g.ListDynamicVariableAccess().ToList();
			var ios = g.ListConstantVariableAccess().ToList();

			if (dvs.Count > 0) return false;
			if (ios.Count == 0) return false;

			var newvars = ios
				.Select(p => new Vec2l(p.getX().Calculate(null), p.getY().Calculate(null)))
				.Distinct()
				.Select((p, i) => ExpressionVariable.CreateUserVariable(i, _gridGet(p.X, p.Y), p))
				.ToList();

			g.Variables.AddRange(newvars);

			var vardic = new Dictionary<Vec2l, ExpressionVariable>();
			newvars.ForEach(p => vardic.Add(p.position, p));

			BCModRule vertexRule1 = new BCModRule();
			vertexRule1.AddPreq<BCVertexExprGet>(p => ios.Contains(p));
			vertexRule1.AddRep((l, p) => new BCVertexVarGet(BCDirection.UNKNOWN, p, vardic[((BCVertexExprGet)l[0]).getConstantPos()]));

			BCModRule vertexRule2 = new BCModRule();
			vertexRule2.AddPreq<BCVertexExprPopSet>(p => ios.Contains(p));
			vertexRule2.AddRep((l, p) => new BCVertexVarSet(BCDirection.UNKNOWN, p, vardic[((BCVertexExprPopSet)l[0]).getConstantPos()]));

			BCModRule vertexRule3 = new BCModRule();
			vertexRule3.AddPreq<BCVertexExprSet>(p => ios.Contains(p));
			vertexRule3.AddRep((l, p) => new BCVertexExprVarSet(BCDirection.UNKNOWN, p, vardic[((BCVertexExprSet)l[0]).getConstantPos()], ((BCVertexExprSet)l[0]).Value));

			BCExprModRule exprRule1 = new BCExprModRule();
			exprRule1.SetPreq<ExpressionGet>(p => ios.Contains(p));
			exprRule1.SetRep(p => vardic[((ExpressionGet)p).getConstantPos()]);

			bool changed = true;

			int c = -1;
			while (changed)
			{
				c++;
				bool[] cb = new[]
				{
					vertexRule1.Execute(g),
					vertexRule2.Execute(g),
					vertexRule3.Execute(g),

					exprRule1.Execute(g),
				};

				changed = cb.Any(p => p);
			}

			return c > 0;
		}

		private bool RemovePredeterminedDecisions_0(BCGraph g)
		{
			List<BCVertex> chain = null;

			foreach (var v in g.Vertices.Where(p => p is BCVertexDecision))
			{
				var prev = v.Parents.FirstOrDefault(p => (p as BCVertexExpression)?.Expression is ExpressionConstant);
				if (prev == null)
					continue;
				if (prev.Parents.Count == 0)
					continue;

				// Without this we would go down the rabbithole of actually evaulating the program
				// So we only remove decisions that are predetermined on all paths
				if (!v.Parents.All(p => (p as BCVertexExpression)?.Expression is ExpressionConstant))
					continue;

				chain = new List<BCVertex>() { prev, v };
			}

			if (chain == null)
				return false;


			var Expression = chain[0] as BCVertexExpression;
			var Decision = chain[1] as BCVertexDecision;

			var Prev = Expression.Parents;
			var Next = Expression.Expression.Calculate(null) != 0 ? Decision.EdgeTrue : Decision.EdgeFalse;

			//######## REPLACE ########

			Next.Parents.AddRange(Prev);
			foreach (var node in Prev)
			{
				node.Children.Add(Next);
				node.Children.Remove(Expression);

				if (node is IDecisionVertex)
				{
					if ((node as IDecisionVertex).EdgeTrue == Expression)
						(node as IDecisionVertex).EdgeTrue = Next;
					if ((node as IDecisionVertex).EdgeFalse == Expression)
						(node as IDecisionVertex).EdgeFalse = Next;
				}
			}
			Decision.Parents.Remove(Expression);

			Expression.Parents.Clear();
			Expression.Children.Clear();
			g.Vertices.Remove(Expression);

			if (Decision.Parents.Count == 0)
			{
				Next.Parents.Remove(Decision);

				Decision.Parents.Clear();
				Decision.Children.ForEach(p => p.Parents.Remove(Decision));
				Decision.Children.Clear();
				g.Vertices.Remove(Decision);

				RemoveHeadlessPaths(g);
			}

			RemoveUnreachableNodes(g);

			return true;
		}

		private bool RemovePredeterminedDecisions_1(BCGraph g)
		{
			foreach (var vertex in g.Vertices)
			{
				if (!((vertex as BCVertexExprDecision)?.Value is ExpressionConstant))
					continue;

				BCVertexExprDecision decision = vertex as BCVertexExprDecision;

				if (decision.Value.Calculate(null) != 0)
				{
					decision.EdgeFalse.Parents.Remove(decision);
					decision.Children.Remove(decision.EdgeFalse);
					decision.EdgeFalse = null;
				}
				else
				{
					decision.EdgeTrue.Parents.Remove(decision);
					decision.Children.Remove(decision.EdgeTrue);
					decision.EdgeTrue = null;
				}

				var remRule = new BCModRule(false);
				remRule.AddPreq(p => p == decision);

				bool exec = remRule.Execute(g);

				if (!exec)
					throw new Exception("errrrrrrr");

				var included = g.WalkGraphByChildren();
				g.Vertices = g.Vertices.Where(p => included.Contains(p)).ToList();

				foreach (var v in g.Vertices)
				{
					v.Parents.Where(p => !included.Contains(p))
						.ToList()
						.ForEach(p => v.Parents.Remove(p));
				}

				RemoveHeadlessPaths(g);

				return true;
			}

			return false;
		}

		private void RemoveHeadlessPaths(BCGraph g)
		{
			for (;;)
			{
				var headless = g.Vertices.FirstOrDefault(p => p.Parents.Count == 0 && p != g.Root);
				if (headless == null)
					break;

				g.Vertices.Remove(headless);
				headless.Children.ForEach(p => p.Parents.Remove(headless));
			}
		}

		private void RemoveUnreachableNodes(BCGraph g)
		{
			var reachable = g.WalkGraphByChildren();

			for (int i = g.Vertices.Count - 1; i >= 0; i--)
			{
				if (!reachable.Contains(g.Vertices[i]))
				{
					foreach (var c in g.Vertices[i].Children) c.Parents.Remove(g.Vertices[i]);
					g.Vertices.RemoveAt(i);
				}
			}
		}

		private bool Unstackify(BCGraph g, out string info)
		{
			var r = g.Unstackifier.Run();
			info = string.Join(";", g.Unstackifier.LastRunInfo);
			return r;
		}

		private bool ReplaceVariableIntializer(BCGraph g)
		{
			if (g.Root.Parents.Count != 0)
				return false;

			if (g.Variables.Count == 0)
				return false;

			if (!(g.Root is BCVertexBlock))
				return false;

			BCVertexBlock BRoot = g.Root as BCVertexBlock;
			foreach (var variable in g.Variables.Where(p => p.isUserDefinied))
			{
				foreach (BCVertex node in BRoot.nodes)
				{
					if (node is BCVertexExprVarSet && (node as BCVertexExprVarSet).Variable == variable && (node as BCVertexExprVarSet).Value is ExpressionConstant)
					{
						long ivalue = ((node as BCVertexExprVarSet).Value as ExpressionConstant).Value;
						variable.initial = ivalue;

						BCVertex newnode = BRoot.GetWithRemovedNode(node);

						var ruleRepl = new BCModRule(false, false);
						ruleRepl.AddPreq(p => p == BRoot);
						var node1 = node;
						ruleRepl.AddRep((l, p) => (l[0] as BCVertexBlock).GetWithRemovedNode(node1));

						if (ruleRepl.Execute(g))
							return true;
						else
							break;
					}

					if (node.GetVariables().Contains(variable))
						break;
				}
			}

			return false;
		}

		private bool CombineIdenticalBlocks(BCGraph g)
		{
			foreach (var vx1 in g.Vertices)
			{
				foreach (var vx2 in g.Vertices)
				{
					if (vx1 != vx2 && vx1.IsIdentical(vx2) && vx1.IsIdenticalChildren(vx2))
					{
						MergeVertex(g, vx1, vx2);

						return true;
					}
				}
			}

			return false;
		}

		private void MergeVertex(BCGraph g, BCVertex master, BCVertex slave)
		{
			g.Vertices.Remove(slave);

			foreach (var parent in slave.Parents)
			{
				parent.Children.Remove(slave);
				parent.Children.Add(master);
				master.Parents.Add(parent);

				if (parent is IDecisionVertex)
				{
					if ((parent as IDecisionVertex).EdgeTrue == slave)
						(parent as IDecisionVertex).EdgeTrue = master;

					if ((parent as IDecisionVertex).EdgeFalse == slave)
						(parent as IDecisionVertex).EdgeFalse = master;
				}
			}

			foreach (var child in slave.Children)
			{
				child.Parents.Remove(slave);
			}
		}

		private bool RemoveUnusedVariables(BCGraph g, out string info)
		{
			foreach (var v in g.Variables.ToList())
			{
				var usage = g.Vertices.Where(gv => gv.GetVariables().Contains(v)).ToList();

				bool rem = true;
				foreach (var usg in usage)
				{
					if (usg is BCVertexVarSet)
					{
						continue;
					}
					else if (usg is BCVertexExprVarSet && ((BCVertexExprVarSet)usg).Variable == v)
					{
						var vtx = (BCVertexExprVarSet)usg;

						if (vtx.Value.IsStateModifying())
						{
							rem = false;
							break;
						}
					}
					else if (usg is BCVertexGetVarSet)
					{
						continue;
					}
					else
					{
						rem = false;
						break;
					}
				}

				if (rem)
				{
					foreach (var usg in usage)
					{
						if (usg is BCVertexVarSet)
						{
							g.ReplaceVertex(usg, new BCVertexPop(usg.Direction, usg.Positions));
						}
						else if (usg is BCVertexExprVarSet)
						{
							g.RemoveVertex(usg);
						}
						else if (usg is BCVertexGetVarSet)
						{
							var lst = new List<BCVertex>
							{
								new BCVertexPop(usg.Direction, usg.Positions),
								new BCVertexPop(usg.Direction, usg.Positions),
							};

							g.ReplaceVertex(usg, lst);
						}
						else
						{
							g.RemoveVertex(usg);
						}
					}

					g.Variables.Remove(v);
					info = v.Identifier;
					return true;
				}
			}

			info = string.Empty;
			return false;
		}

		#endregion

		private void Add(string name, BCModRule rule, int[] scope)
		{
			var os = new OptimizerStep();
			os.Name = name;
			os.Scope = new HashSet<int>(scope);
			os.Action = (g) => { var r = rule.Execute(g); os.LastRunInfo = string.Join(", ", rule.LastRunInfo.Distinct()); return r; };

			AllSteps.Add(os);
		}

		private void Add(string name, Func<BCGraph, bool> a, int[] scope)
		{
			AllSteps.Add(new OptimizerStep
			{
				Name = name,
				Action = a,
				Scope = new HashSet<int>(scope),
			});
		}

		private void Add(string name, StepAction a, int[] scope)
		{
			var os = new OptimizerStep();
			os.Name = name;
			os.Scope = new HashSet<int>(scope);
			os.Action = (g) => { string i; var r = a(g, out i); os.LastRunInfo = i; return r; };

			AllSteps.Add(os);
		}

		public bool Execute(BCGraph g, int level)
		{
			bool result = false;

			int count = 0;
			foreach (var step in AllSteps.Where(s => s.Scope.Contains(level)))
			{
				var r = step.Run(g);

#if DEBUG
				if (!g.TestGraph()) throw new Exception("Graph became invalid !");
#endif

				if (r) g.UsedOptimizations.Add($"[{level}] {step.Name} {{{step.LastRunInfo}}}");
				
				result = result || r;

				if (r)
				{
					count++;
					if (count > MAX_OPTIMIZATIONS_PER_LEVEL) return result;
				}

			}

			return result;
		}
	}
}
