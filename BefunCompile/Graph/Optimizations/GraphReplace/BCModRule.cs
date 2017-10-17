using BefunCompile.Exceptions;
using BefunCompile.Graph.Vertex;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph
{
	public class BCModRule
	{
		private enum ReorderMode { Impossible, MoveSignificantBackwards, MoveSignificantForwards }

		private readonly List<Func<BCVertex, bool>> prerequisites = new List<Func<BCVertex, bool>>();
		private readonly List<Func<BCVertex[], Vec2i[], BCVertex>> replacements = new List<Func<BCVertex[], Vec2i[], BCVertex>>();
		private readonly List<Func<BCVertex[], bool>> conditions = new List<Func<BCVertex[], bool>>();

		private readonly bool allowPathExtraction;
		private readonly bool allowSplitCodePathReplacement;
		private readonly bool allowNodeReordering;

		public List<string> LastRunInfo = new List<string>();

		public BCModRule(bool allowExtr = true, bool allowSPC = false, bool allowReorder = true)
		{
			allowPathExtraction = allowExtr;
			allowSplitCodePathReplacement = allowSPC;
			allowNodeReordering = allowReorder;
		}

		public void AddPreq<T>() where T : BCVertex
		{
			prerequisites.Add(v => v is T);
		}

		public void AddPreq<T>(Func<T, bool> p) where T : BCVertex
		{
			prerequisites.Add(v => v is T && p((T)v));
		}

		public void AddPreq(params Func<BCVertex, bool>[] p)
		{
			foreach (var preq in p)
			{
				prerequisites.Add(preq);
			}
		}

		public void AddRep(params Func<BCVertex[], Vec2i[], BCVertex>[] r)
		{
			foreach (var rep in r)
			{
				replacements.Add(rep);
			}
		}

		public void AddCond(params Func<BCVertex[], bool>[] c)
		{
			foreach (var cond in c)
			{
				conditions.Add(cond);
			}
		}

		public bool ArrayExecute(BCGraph g)
		{
			foreach (var curr in g.Vertices)
			{
				if (Execute(g, curr, false))
					return true;
			}

			return false;
		}

		public bool Execute(BCGraph g)
		{
			LastRunInfo.Clear();

			HashSet<BCVertex> travelled = new HashSet<BCVertex>();
			Stack<BCVertex> untravelled = new Stack<BCVertex>();
			untravelled.Push(g.Root);

			while (untravelled.Count > 0)
			{
				BCVertex curr = untravelled.Pop();

				if (Execute(g, curr, false))
					return true;

				travelled.Add(curr);

				foreach (var child in curr.Children.Where(child => !travelled.Contains(child)))
				{
					untravelled.Push(child);
				}
			}

			return false;
		}

		public bool Debug(BCGraph g)
		{
			HashSet<BCVertex> travelled = new HashSet<BCVertex>();
			Stack<BCVertex> untravelled = new Stack<BCVertex>();
			untravelled.Push(g.Root);

			while (untravelled.Count > 0)
			{
				BCVertex curr = untravelled.Pop();

				if (Execute(g, curr, true))
					return true;

				travelled.Add(curr);

				foreach (var child in curr.Children.Where(child => !travelled.Contains(child)))
				{
					untravelled.Push(child);
				}
			}

			return false;
		}

		private bool Execute(BCGraph g, BCVertex v, bool debug)
		{
			var chainlist = allowPathExtraction ? GetMatchingExtractedChain(g, v) : GetMatchingChain(v);

			if (chainlist == null && allowNodeReordering) chainlist = GetMatchingReorderedChain(g, v);

			if (chainlist == null)
				return false;

			//##################################################################################

			BCVertex[] chain = chainlist.ToArray();
			Vec2i[] posarr = chain.SelectMany(p => p.Positions).Distinct().ToArray();
			BCVertex[] repChain = replacements.Select(p => p(chain, posarr)).ToArray();

			if (chain.Skip(1).Any(p => p.Parents.Count > 1))
			{
				return false;
			}

			if (chain.SkipLastN(1).Any(p => p.Children.Count > 1))
			{
				return false;
			}

			BCVertex chainFirst = chain[0];
			BCVertex chainLast = chain.Last();

			bool isRoot = (chainFirst == g.Root);
			bool isLeaf = (chainLast.Children.Count == 0);

			if (repChain.Length == 0 && (isRoot || isLeaf))
				repChain = new BCVertex[] { new BCVertexNOP(BCDirection.UNKNOWN, posarr) };

			if (chainLast.Children.Contains(chainFirst))
				return false;

			BCVertex repChainFirst = repChain.FirstOrDefault();
			BCVertex repChainLast = repChain.LastOrDefault();

			BCVertex[] prev = chainFirst.Parents.ToArray();
			BCVertex[] next = chainLast.Children.ToArray();

			if (repChain.Length == 0 && prev.Any(p => p.IsCodePathSplit()) && next.Length == 0)
				repChain = new BCVertex[] { new BCVertexNOP(BCDirection.UNKNOWN, posarr) };

			if (next.Length > 1 && !allowSplitCodePathReplacement)
				return false;

			if (repChain.Length == 1 && chain.Length == 1 && repChain[0] is BCVertexNOP && chain[0] is BCVertexNOP)
				return false;

			for (int i = 0; i < repChain.Length - 1; i++)
			{
				repChain[i].Children.Add(repChain[i + 1]);
			}
			for (int i = 1; i < repChain.Length; i++)
			{
				repChain[i].Parents.Add(repChain[i - 1]);
			}

			if (repChain.Length > 0)
			{
				repChainFirst.Parents.AddRange(prev);
				repChainLast.Children.AddRange(next);

				foreach (var snext in next)
				{
					snext.Parents.Remove(chainLast);
					snext.Parents.Add(repChainLast);
				}
				foreach (var sprev in prev)
				{
					sprev.Children.Remove(chainFirst);
					sprev.Children.Add(repChainFirst);

					if (sprev is IDecisionVertex)
					{
						if ((sprev as IDecisionVertex).EdgeTrue == chainFirst)
							(sprev as IDecisionVertex).EdgeTrue = repChainFirst;
						else if ((sprev as IDecisionVertex).EdgeFalse == chainFirst)
							(sprev as IDecisionVertex).EdgeFalse = repChainFirst;
						else
							throw new CodeGenException();
					}
				}

				if (isRoot)
					g.Root = repChainFirst;
			}
			else
			{
				foreach (var snext in next)
				{
					snext.Parents.Remove(chainLast);
					snext.Parents.AddRange(prev);
				}
				foreach (var sprev in prev)
				{
					sprev.Children.Remove(chainFirst);
					sprev.Children.AddRange(next);

					if (sprev is BCVertexDecision)
					{
						if ((sprev as BCVertexDecision).EdgeTrue == chainFirst)
							(sprev as BCVertexDecision).EdgeTrue = next[0];
						else if ((sprev as BCVertexDecision).EdgeFalse == chainFirst)
							(sprev as BCVertexDecision).EdgeFalse = next[0];
						else
							throw new CodeGenException();
					}

					if (sprev is BCVertexExprDecision)
					{
						if ((sprev as BCVertexExprDecision).EdgeTrue == chainFirst)
							(sprev as BCVertexExprDecision).EdgeTrue = next[0];
						else if ((sprev as BCVertexExprDecision).EdgeFalse == chainFirst)
							(sprev as BCVertexExprDecision).EdgeFalse = next[0];
						else
							throw new CodeGenException();
					}
				}

				if (isRoot)
					g.Root = next[0];
			}

			g.Vertices.RemoveAll(p => chain.Contains(p));
			g.Vertices.AddRange(repChain);

			if (repChain.Length == 0)
			{
				if (next.Length > 0)
					next[0].Positions = next[0].Positions.Concat(posarr).Distinct().ToArray();
				else if (prev.Length > 0)
					prev[0].Positions = prev[0].Positions.Concat(posarr).Distinct().ToArray();
				else
					throw new ArgumentException("We lost a code point :( ");
			}

			if (debug)
			{
				Console.Out.WriteLine("######## BEFORE ########");
				chain.ToList().ForEach(e => Console.Out.WriteLine(e.ToString()));
				Console.Out.WriteLine("######## AFTER #########");
				repChain.ToList().ForEach(e => Console.Out.WriteLine(e.ToString()));
				Console.Out.WriteLine();
			}

			return true;
		}

		public List<BCVertex> GetMatchingChain(BCGraph g)
		{
			HashSet<BCVertex> travelled = new HashSet<BCVertex>();
			Stack<BCVertex> untravelled = new Stack<BCVertex>();
			untravelled.Push(g.Root);

			while (untravelled.Count > 0)
			{
				BCVertex curr = untravelled.Pop();

				var chain = GetMatchingChain(0, curr);

				if (chain != null)
					return chain;

				travelled.Add(curr);

				foreach (var child in curr.Children)
				{
					if (!travelled.Contains(child))
						untravelled.Push(child);
				}
			}

			return null;
		}

		private List<BCVertex> GetMatchingChain(BCVertex v)
		{
			List<BCVertex> chain = GetMatchingChain(0, v);

			if (chain == null || !conditions.All(c => c(chain.ToArray()))) return null;

			return chain;
		}

		private List<BCVertex> GetMatchingExtractedChain(BCGraph g, BCVertex v)
		{
			List<BCVertex> chain = GetMatchingChain(0, v);

			if (chain == null || !conditions.All(c => c(chain.ToArray()))) return null;

			chain = ExtractChain(g, chain);
			if (chain != null) LastRunInfo.Add("EXTRACTED");

			return chain;
		}

		private List<BCVertex> GetMatchingChain(int pos, BCVertex v)
		{
			if (pos >= prerequisites.Count)
				return null;

			if (prerequisites[pos](v))
			{
				if (pos + 1 == prerequisites.Count)
				{
					return new List<BCVertex>() { v };
				}
				else
				{
					List<BCVertex> tail = v.Children
						.Aggregate<BCVertex, List<BCVertex>>(null, (current, child) => current ?? GetMatchingChain(pos + 1, child));

					if (tail == null)
					{
						return null;
					}
					else
					{
						tail.Insert(0, v);
						return tail;
					}
				}
			}

			return null;
		}

		private List<BCVertex> GetMatchingReorderedChain(BCGraph g, BCVertex v)
		{
			if (prerequisites.Count == 0 || prerequisites.Count == 1) return null;

			var match0 = prerequisites[0](v);
			if (!match0) return null;

			var rawchain = new List<Tuple<BCVertex, bool, int>>();
			rawchain.Add(Tuple.Create(v, true, 0));
			
			var vertex = v;

			var position = 1;
			for (;;)
			{
				if (vertex.Children.Count != 1) return null;
				vertex = vertex.Children.Single();

				if (prerequisites[position](vertex))
				{
					rawchain.Add(Tuple.Create(vertex, true, position));
					position++;
					if (position >= prerequisites.Count) break;
				}
				else
				{
					rawchain.Add(Tuple.Create(vertex, false, -1));
				}
			}

			var realchain = rawchain.Where(c => c.Item2).Select(c => c.Item1).ToArray();
			if (!conditions.All(c => c(realchain))) return null;
			if (realchain.Length == rawchain.Count) return null;

			if (!CanReorderChain(rawchain, out ReorderMode reom)) return null;

			bool needsExtraction = rawchain.Skip(1).Any(p => p.Item1.Parents.Count > 1);

			if (needsExtraction)
			{
				if (!allowPathExtraction) return null;

				var newchain = ExtractChain(g, rawchain.Select(c => c.Item1).ToList());
				var rawchain2 = new List<Tuple<BCVertex, bool, int>>();
				for (int i = 0; i < rawchain.Count; i++)
				{
					rawchain2.Add(Tuple.Create(newchain[i], rawchain[i].Item2, rawchain[i].Item3));
				}
				LastRunInfo.Add("EXTRACTED");
				rawchain = rawchain2;
			}

			LastRunInfo.Add("REORDERED");
			return ReorderChain(g, rawchain, reom);
		}

		private bool CanReorderChain(List<Tuple<BCVertex, bool, int>> chain, out ReorderMode mode)
		{
			if (CanReorderChainForward(chain))
			{
				mode = ReorderMode.MoveSignificantForwards;
				return true;
			}

			if (CanReorderChainBackward(chain))
			{
				mode = ReorderMode.MoveSignificantBackwards;
				return true;
			}

			mode = ReorderMode.Impossible;
			return false;
		}

		private bool CanReorderChainForward(List<Tuple<BCVertex, bool, int>> chain)
		{
			// Move used vertices to higher indizies
			
			if (chain.Any(p => p.Item1.IsCodePathSplit())) return false;

			if (!chain.First().Item2) return false;
			if (!chain.Last().Item2) return false;

			for (int i = 0; i < chain.Count-1; i++)
			{
				if (!chain[i].Item2) continue;

				var se = chain[i].Item1.GetSideEffects();

				if (chain.Skip(i).Where(c => !c.Item2).Any(c => !BCModAreaHelper.CanSwap(se, c.Item1.GetSideEffects()))) return false;
			}

			return true;
		}

		private bool CanReorderChainBackward(List<Tuple<BCVertex, bool, int>> chain)
		{
			// Move used vertices to lower indizies

			if (chain.Any(p => p.Item1.IsCodePathSplit())) return false;

			if (!chain.First().Item2) return false;
			if (!chain.Last().Item2) return false;

			for (int i = 1; i < chain.Count; i++)
			{
				if (!chain[i].Item2) continue;

				var se = chain[i].Item1.GetSideEffects();

				if (chain.Take(i).Where(c => !c.Item2).Any(c => !BCModAreaHelper.CanSwap(se, c.Item1.GetSideEffects()))) return false;
			}

			return true;
		}

		private List<BCVertex> ReorderChain(BCGraph g, List<Tuple<BCVertex, bool, int>> chain, ReorderMode mode)
		{
			if (chain.Skip(1).Any(p => p.Item1.Parents.Count > 2)) throw new Exception("Cannot reorder multi-parent vertices");

			var parents = chain.First().Item1.Parents.ToList();
			var children = chain.Last().Item1.Children.ToList();
			var isRoot = (g.Root == chain.First().Item1);

			List<Tuple<BCVertex, bool, int>> ordered;

			if (mode == ReorderMode.MoveSignificantForwards)
				ordered = chain.Where(c1 => !c1.Item2).Concat(chain.Where(c2 => c2.Item2)).ToList();
			else if (mode == ReorderMode.MoveSignificantBackwards)
				ordered = chain.Where(c1 => c1.Item2).Concat(chain.Where(c2 => !c2.Item2)).ToList();
			else
				throw new Exception("Invalid order mode: " + mode);

			foreach (var o in ordered)
			{
				o.Item1.Parents.Clear();
				o.Item1.Children.Clear();
			}

			foreach (var pp in parents)
			{
				pp.Children.Remove(chain.First().Item1);
				pp.Children.Add(ordered.First().Item1);

				var dec = pp as IDecisionVertex;

				if (dec != null)
				{
					if (dec.EdgeFalse == chain.First().Item1)
						dec.EdgeFalse = ordered.First().Item1;

					if (dec.EdgeTrue == chain.First().Item1)
						dec.EdgeTrue = ordered.First().Item1;
				}

				ordered.First().Item1.Parents.Add(pp);
			}

			for (int i = 1; i < ordered.Count; i++)
			{
				ordered[i].Item1.Parents.Add(ordered[i - 1].Item1);
			}

			for (int i = 0; i < ordered.Count-1; i++)
			{
				ordered[i].Item1.Children.Add(ordered[i + 1].Item1);
			}

			foreach (var cc in children)
			{
				cc.Parents.Remove(chain.Last().Item1);
				cc.Parents.Add(ordered.Last().Item1);

				ordered.Last().Item1.Children.Add(cc);
			}

			if (isRoot)
			{
				g.Root = ordered.First().Item1;
			}

			return ordered.Where(p => p.Item2).Select(p => p.Item1).ToList();
		}

		private List<BCVertex> ExtractChain(BCGraph g, List<BCVertex> chain)
		{
			if (chain == null)
				return null;

			if (!chain.Skip(1).Any(p => p.Parents.Count > 1))
				return chain;

			if (chain.Count <= 1)
				return chain;

			if (chain.Last().Children.Count > 1)
				return chain;

			int cutIndex = chain.FindIndex(p => p.Parents.Count > 1 && p != chain[0]);

			BCVertex cut = chain[cutIndex];
			BCVertex cutPrev = cut.Parents.First(p => p != chain[cutIndex - 1]);
			BCVertex next = chain.Last().Children.FirstOrDefault();

			cutPrev.Children.Remove(cut);
			cut.Parents.Remove(cutPrev);

			BCVertex cutCurr = cutPrev;
			for (int i = cutIndex; i < chain.Count; i++)
			{
				BCVertex newVertex = chain[i].Duplicate();
				g.Vertices.Add(newVertex);

				cutCurr.Children.Add(newVertex);
				newVertex.Parents.Add(cutCurr);

				if (cutCurr is IDecisionVertex)
				{
					if ((cutCurr as IDecisionVertex).EdgeTrue == cut)
						(cutCurr as IDecisionVertex).EdgeTrue = newVertex;
					else if ((cutCurr as IDecisionVertex).EdgeFalse == cut)
						(cutCurr as IDecisionVertex).EdgeFalse = newVertex;
					else
						throw new ArgumentException("We lost a code point :( ");
				}

				cutCurr = newVertex;
			}
			if (next != null)
			{
				cutCurr.Children.Add(next);
				next.Parents.Add(cutCurr);
			}

			return ExtractChain(g, chain);
		}
	}
}
