﻿using BefunCompile.Graph.Expression;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph.Optimizations.Unstackify
{
	public class UnstackifyValue
	{
		public List<UnstackifyValueAccess> AccessCounter;

		public ExpressionVariable Replacement;

		public bool IsPoisoned { get; private set; }

		private HashSet<UnstackifyValue> PoisonLinks = new HashSet<UnstackifyValue>();

		public UnstackifyValue()
		{
			AccessCounter = new List<UnstackifyValueAccess>();
			IsPoisoned = false;
		}

		public UnstackifyValue(UnstackifyValueAccess value)
		{
			AccessCounter = new List<UnstackifyValueAccess>();
			AddAccess(value);
		}

		public UnstackifyValue(BCVertex vx, UnstackifyValueAccessType type)
			: this(new UnstackifyValueAccess(vx, type)) { }

		public void AddAccess(UnstackifyValueAccess value)
		{
			AccessCounter.Add(value);

			value.Value = this;
		}

		public void AddAccess(BCVertex vx, UnstackifyValueAccessType type)
		{
			AddAccess(new UnstackifyValueAccess(vx, type));
		}

		public void AddAccess(BCVertex vx, UnstackifyValueAccessType type, UnstackifyValueAccessModifier mod)
		{
			AddAccess(new UnstackifyValueAccess(vx, type, mod));
		}

		public void Poison()
		{
			IsPoisoned = true;
		}

		public void UpdatePoison()
		{
			PoisonLinks.ToList().ForEach(p => p.Poison());
		}

		public void LinkPoison(UnstackifyValue other)
		{
			PoisonLinks.Add(other);
			other.PoisonLinks.Add(this);
		}

		public override string ToString()
		{
			return (IsPoisoned ? "[P]" : "[ ]") + "#" + AccessCounter.SelectMany(p => p.Vertex.Positions).Select(p => p.X*1000 + p.Y).Sum();
		}

	}
}
