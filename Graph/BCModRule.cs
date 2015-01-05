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
		private List<Func<BCVertex, bool>> prerequisites = new List<Func<BCVertex, bool>>();
		private List<Func<BCVertex[], Vec2i[], BCVertex>> replacements = new List<Func<BCVertex[], Vec2i[], BCVertex>>();

		private readonly bool allowPathExtraction;

		public BCModRule(bool allowExtr = true)
		{
			this.allowPathExtraction = allowExtr;
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

		public bool Execute(BCGraph g)
		{
			HashSet<BCVertex> travelled = new HashSet<BCVertex>();
			Stack<BCVertex> untravelled = new Stack<BCVertex>();
			untravelled.Push(g.root);

			while (untravelled.Count > 0)
			{
				BCVertex curr = untravelled.Pop();

				if (Execute(g, curr))
					return true;

				travelled.Add(curr);

				foreach (var child in curr.children)
				{
					if (!travelled.Contains(child))
						untravelled.Push(child);
				}
			}

			return false;
		}

		public bool Execute(BCGraph g, BCVertex v)
		{
			var chainlist = allowPathExtraction ? GetMatchingExtractedChain(g, v) : GetMatchingChain(v);

			if (chainlist == null)
				return false;

			//##################################################################################

			BCVertex[] chain = chainlist.ToArray();
			Vec2i[] posarr = chain.SelectMany(p => p.positions).Distinct().ToArray();
			BCVertex[] repChain = replacements.Select(p => p(chain, posarr)).ToArray();

			if (chain.Skip(1).Any(p => p.parents.Count > 1))
			{
				return false;
			}

			if (chain.SkipLastN(1).Any(p => p.children.Count > 1))
			{
				return false;
			}

			BCVertex chainFirst = chain[0];
			BCVertex chainLast = chain.Last();

			bool isRoot = (chainFirst == g.root);
			bool isLeaf = (chainLast.children.Count == 0);

			if (repChain.Length == 0 && (isRoot || isLeaf))
				repChain = new BCVertex[] { new BCVertexNOP(BCDirection.UNKNOWN, posarr) };

			if (chainLast.children.Contains(chainFirst))
				return false;

			BCVertex repChainFirst = repChain.FirstOrDefault();
			BCVertex repChainLast = repChain.LastOrDefault();

			BCVertex[] prev = chainFirst.parents.ToArray();
			BCVertex[] next = chainLast.children.ToArray();

			if (repChain.Length == 0 && prev.Any(p => p is BCVertexDecision || p is BCVertexFullDecision) && next.Length == 0)
				repChain = new BCVertex[] { new BCVertexNOP(BCDirection.UNKNOWN, posarr) };

			if (next.Length > 1)
				return false;

			for (int i = 0; i < repChain.Length - 1; i++)
			{
				repChain[i].children.Add(repChain[i + 1]);
			}
			for (int i = 1; i < repChain.Length; i++)
			{
				repChain[i].parents.Add(repChain[i - 1]);
			}

			if (repChain.Length > 0)
			{
				repChainFirst.parents.AddRange(prev);
				repChainLast.children.AddRange(next);

				foreach (var snext in next)
				{
					snext.parents.Remove(chainLast);
					snext.parents.Add(repChainLast);
				}
				foreach (var sprev in prev)
				{
					sprev.children.Remove(chainFirst);
					sprev.children.Add(repChainFirst);

					if (sprev is BCVertexDecision)
					{
						if ((sprev as BCVertexDecision).edgeTrue == chainFirst)
							(sprev as BCVertexDecision).edgeTrue = repChainFirst;
						else if ((sprev as BCVertexDecision).edgeFalse == chainFirst)
							(sprev as BCVertexDecision).edgeFalse = repChainFirst;
						else
							throw new CodeGenException();
					}

					if (sprev is BCVertexFullDecision)
					{
						if ((sprev as BCVertexFullDecision).edgeTrue == chainFirst)
							(sprev as BCVertexFullDecision).edgeTrue = repChainFirst;
						else if ((sprev as BCVertexFullDecision).edgeFalse == chainFirst)
							(sprev as BCVertexFullDecision).edgeFalse = repChainFirst;
						else
							throw new CodeGenException();
					}
				}

				if (isRoot)
					g.root = repChainFirst;
			}
			else
			{
				foreach (var snext in next)
				{
					snext.parents.Remove(chainLast);
					snext.parents.AddRange(prev);
				}
				foreach (var sprev in prev)
				{
					sprev.children.Remove(chainFirst);
					sprev.children.AddRange(next);

					if (sprev is BCVertexDecision)
					{
						if ((sprev as BCVertexDecision).edgeTrue == chainFirst)
							(sprev as BCVertexDecision).edgeTrue = next[0];
						else if ((sprev as BCVertexDecision).edgeFalse == chainFirst)
							(sprev as BCVertexDecision).edgeFalse = next[0];
						else
							throw new CodeGenException();
					}

					if (sprev is BCVertexFullDecision)
					{
						if ((sprev as BCVertexFullDecision).edgeTrue == chainFirst)
							(sprev as BCVertexFullDecision).edgeTrue = next[0];
						else if ((sprev as BCVertexFullDecision).edgeFalse == chainFirst)
							(sprev as BCVertexFullDecision).edgeFalse = next[0];
						else
							throw new CodeGenException();
					}
				}

				if (isRoot)
					g.root = next[0];
			}

			g.vertices.RemoveAll(p => chain.Contains(p));
			g.vertices.AddRange(repChain);

			if (repChain.Length == 0)
			{
				if (next.Length > 0)
					next[0].positions = next[0].positions.Concat(posarr).Distinct().ToArray();
				else if (prev.Length > 0)
					prev[0].positions = prev[0].positions.Concat(posarr).Distinct().ToArray();
				else
					throw new ArgumentException("We lost a code point :( ");
			}

			return true;
		}

		public List<BCVertex> GetMatchingChain(BCGraph g)
		{
			HashSet<BCVertex> travelled = new HashSet<BCVertex>();
			Stack<BCVertex> untravelled = new Stack<BCVertex>();
			untravelled.Push(g.root);

			while (untravelled.Count > 0)
			{
				BCVertex curr = untravelled.Pop();

				var chain = GetMatchingChain(0, curr);

				if (chain != null)
					return chain;

				travelled.Add(curr);

				foreach (var child in curr.children)
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
					List<BCVertex> tail = null;

					foreach (var child in v.children)
					{
						tail = tail ?? GetMatchingChain(pos + 1, child);
					}

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

			if (!chain.Skip(1).Any(p => p.parents.Count > 1))
				return chain;

			if (chain.Count <= 1)
				return chain;

			if (chain.Last().children.Count > 1)
				return chain;

			bool isRoot = chain[0] == g.root;
			int cutIndex = chain.FindIndex(p => p.parents.Count > 1 && p != chain[0]);

			BCVertex cut = chain[cutIndex];
			BCVertex cutPrev = cut.parents.Where(p => p != chain[cutIndex - 1]).First();
			BCVertex next = chain.Last().children.FirstOrDefault();

			cutPrev.children.Remove(cut);
			cut.parents.Remove(cutPrev);

			BCVertex cutCurr = cutPrev;
			for (int i = cutIndex; i < chain.Count; i++)
			{
				BCVertex newVertex = chain[i].Duplicate();
				g.vertices.Add(newVertex);

				cutCurr.children.Add(newVertex);
				newVertex.parents.Add(cutCurr);

				if (cutCurr is BCVertexDecision)
				{
					if ((cutCurr as BCVertexDecision).edgeTrue == cut)
						(cutCurr as BCVertexDecision).edgeTrue = newVertex;
					else if ((cutCurr as BCVertexDecision).edgeFalse == cut)
						(cutCurr as BCVertexDecision).edgeFalse = newVertex;
					else
						throw new ArgumentException("We lost a code point :( ");
				}

				cutCurr = newVertex;
			}
			if (next != null)
			{
				cutCurr.children.Add(next);
				next.parents.Add(cutCurr);
			}

			return ExtractChain(g, chain);
		}
	}
}
