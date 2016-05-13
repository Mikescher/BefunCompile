using BefunCompile.Graph;
using BefunCompile.Graph.Vertex;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.CodeGeneration.Generator
{
	class CodeGeneratorPython3 : CodeGeneratorPython
	{
		protected override OutputLanguage LANG => OutputLanguage.Python3;

		protected override string SHEBANG => @"#!/usr/bin/env python3";

		protected override IEnumerable<string> AdditionalImports => Enumerable.Empty<string>();

		protected override string GetBase64DecodeHeader() => @"import gzip, base64";

		protected override string GetGZipDecodeStatement()
		{
			var codebuilder = new SourceCodeBuilder();

			codebuilder.AppendLine(@"g = base64.b64decode(_g)[1:]");
			codebuilder.AppendLine(@"for i in range(base64.b64decode(_g)[0]):");
			codebuilder.AppendLine(@"    g = gzip.decompress(g)");
			codebuilder.AppendLine(@"g=list(g)");

			return codebuilder.ToString();
		}

		protected override string GenerateCodeBCVertexExprOutput(BCVertexExprOutput comp, BCGraph g)
		{
			if (comp.ModeInteger)
				return string.Format("print({0},end=\"\",flush=True)", comp.Value.GenerateCode(LANG, g, false));
			else
				return string.Format("print(chr({0}),end=\"\",flush=True)", comp.Value.GenerateCode(LANG, g, false));
		}

		protected override string GenerateCodeBCVertexOutput(BCVertexOutput comp, BCGraph g)
		{
			if (comp.ModeInteger)
				return string.Format("print({0},end=\"\",flush=True)", "sp()");
			else
				return string.Format("print(chr({0}),end=\"\",flush=True)", "sp()");
		}

		protected override string GenerateCodeBCVertexStringOutput(BCVertexStringOutput comp, BCGraph g)
		{
			return string.Format("print(\"{0}\",end=\"\",flush=True)", comp.Value);
		}
	}
}
