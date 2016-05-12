using BefunCompile.Graph;
using BefunCompile.Graph.Vertex;
using System.Collections.Generic;

namespace BefunCompile.CodeGeneration.Generator
{
	class CodeGeneratorPython2 : CodeGeneratorPython
	{
		protected override OutputLanguage LANG => OutputLanguage.Python2;

		protected override string SHEBANG => @"#!/usr/bin/env python2";
		
		protected override IEnumerable<string> AdditionalImports => new[] { @"import sys" };

		protected override string GetBase64DecodeRangeExpression() => @"range(ord(base64.b64decode(_g)[0]))";

		protected override string GetBase64DecodeHeader() => @"import zlib, base64";

		protected override string GetGZipDecodeStatement() => @"g = zlib.decompress(g, 16+zlib.MAX_WBITS)";

		protected override string GenerateCodeBCVertexExprOutput(BCVertexExprOutput comp, BCGraph g)
		{
			var builder = new SourceCodeBuilder();

			if (comp.ModeInteger)
				builder.AppendLine(string.Format("sys.stdout.write(str({0}))", comp.Value.GenerateCode(LANG, g, false)));
			else
				builder.AppendLine(string.Format("sys.stdout.write(chr({0}))", comp.Value.GenerateCode(LANG, g, false)));

			builder.AppendLine("sys.stdout.flush()");

			return builder.ToString();
		}

		protected override string GenerateCodeBCVertexOutput(BCVertexOutput comp, BCGraph g)
		{
			var builder = new SourceCodeBuilder();

			if (comp.ModeInteger)
				builder.AppendLine(string.Format("sys.stdout.write(str({0}))", "sp()"));
			else
				builder.AppendLine(string.Format("sys.stdout.write(chr({0}))", "sp()"));

			builder.AppendLine("sys.stdout.flush()");

			return builder.ToString();
		}

		protected override string GenerateCodeBCVertexStringOutput(BCVertexStringOutput comp, BCGraph g)
		{
			var builder = new SourceCodeBuilder();

			builder.AppendLine(string.Format("sys.stdout.write(\"{0}\")", comp.Value));

			builder.AppendLine("sys.stdout.flush()");

			return builder.ToString();
		}
	}
}
