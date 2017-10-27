using BefunCompile.Graph.Expression;
using System;
using System.Collections.Generic;

namespace BefunCompile.Graph
{
	public class BCExprModRule
	{
		private Func<BCExpression, bool> prerequisite = null;
		private Func<BCExpression, BCExpression> replacement = null;

		public List<string> LastRunInfo = new List<string>();

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

			LastRunInfo.Clear();

			foreach (var vertex in g.Vertices)
			{
				var prev = vertex.ToOneLineString();
				if (vertex.SubsituteExpression(prerequisite, replacement))
				{
					found = true;

					LastRunInfo.Add($"[{prev}] --> [{vertex.ToOneLineString()}]");
				}
			}

			return found;
		}
	}
}
