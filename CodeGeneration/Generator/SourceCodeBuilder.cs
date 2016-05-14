using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BefunCompile.CodeGeneration.Generator
{
	public class SourceCodeBuilder
	{
		private readonly StringBuilder builder = new StringBuilder();
		private readonly bool skipEmptyLines;

		public SourceCodeBuilder(bool removeEmptyLines = false)
		{
			skipEmptyLines = removeEmptyLines;
		}

		public void AppendLine()
		{
			builder.AppendLine();
		}

		public void AppendLine(string line)
		{
			if (!string.IsNullOrWhiteSpace(line))
				builder.AppendLine(line.TrimEnd());
		}

		public void Append(long value)
		{
			builder.Append(value);
		}

		public void Append(string text)
		{
			builder.Append(text);
		}

		public void Append(char chr)
		{
			builder.Append(chr);
		}

		public override string ToString()
		{
			if (skipEmptyLines)
				return string.Join(Environment.NewLine, Regex.Split(builder.ToString(), @"\r?\n").Where(l => !string.IsNullOrWhiteSpace(l)));
			else
				return builder.ToString();
		}
	}
}
