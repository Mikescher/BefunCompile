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

		public void setPreq(Func<BCExpression, bool> p)
		{
			this.prerequisite = p;
		}

		public void setRep(Func<BCExpression, BCExpression> r)
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
