using BefunCompile.Graph.Expression;
using System.Collections.Generic;

namespace BefunCompile.Graph.Optimizations.Unstackify
{
	public class UnstackifyValue
	{
		public List<UnstackifyValueAccess> AccessCounter;

		public ExpressionVariable Replacement;

		public bool IsPoisoned { get; private set; }

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
	}
}
