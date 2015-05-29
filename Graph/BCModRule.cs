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
		private readonly List<Func<BCVertex, bool>> prerequisites = new List<Func<BCVertex, bool>>();
		private readonly List<Func<BCVertex[], Vec2i[], BCVertex>> replacements = new List<Func<BCVertex[], Vec2i[], BCVertex>>();

		private readonly bool allowPathExtraction;
		private readonly bool allowSplitCodePathReplacement;

		public BCModRule(bool allowExtr = true, bool allowSPC = false)
		{
			allowPathExtraction = allowExtr;
			allowSplitCodePathReplacement = allowSPC;
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
			return GetMatchingChain(0, v);
		}

		private List<BCVertex> GetMatchingExtractedChain(BCGraph g, BCVertex v)
		{
			List<BCVertex> chain = GetMatchingChain(0, v);

			chain = ExtractChain(g, chain);

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
