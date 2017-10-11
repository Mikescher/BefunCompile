using BefunCompile.Graph.Expression;
using System;

namespace BefunCompile.Graph
{
	public class BCExprModRule
	{
		private Func<BCExpression, bool> prerequisite = null;
		private Func<BCExpression, BCExpression> replacement = null;

		public BCExprModRule()
		{
			//
		}

		public void SetPreq<T>() where T : BCExpression
		{
			this.prerequisite = v => v is T;
		}

		public void SetPreq<T>(Func<T, bool> p) where T : BCExpression
		{
			this.prerequisite = v => v is T && p((T)v);
		}

		public void SetPreq(Func<BCExpression, bool> p)
		{
			this.prerequisite = p;
		}

		public void SetRep(Func<BCExpression, BCExpression> r)
		{
			this.replacement = r;
		}

		public bool Execute(BCGraph g)
		{
			bool found = false;

			foreach (var vertex in g.Vertices)
			{
				if (vertex.SubsituteExpression(prerequisite, replacement))
				{
					found = true;
				}
			}

			return found;
		}
	}
}
