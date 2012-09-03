///Need to have Wayloop.Highlight dll as reference
///Also required .NET full, because must have System.Web
using System;
using Wayloop.Highlight;

namespace SharedClasses
{
	public static class HighlightToHtml
	{
		private static string GetHtmlFromHighlighterResult(string definitionName, string source)
		{
			Highlighter highlighter = new Highlighter();
			string returnString =
				highlighter.Highlight(definitionName, source)
				.Replace("\r\n", "\n")
				.Replace("\n\r", "\n")
				.Replace("\n", "<br/>")
				.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");
			highlighter = null;
			return returnString;
		}

		public static string FromCSharpCode(string csharpSourceCode)
		{
			return GetHtmlFromHighlighterResult("C#", csharpSourceCode);
		}

		public static string FromHTMLcode(string htmlSourceCode)
		{
			return GetHtmlFromHighlighterResult("HTML", htmlSourceCode);
		}

		public static string FromJava(string javaSourceCode)
		{
			return GetHtmlFromHighlighterResult("Java", javaSourceCode);
		}

		public static string FromJavaStript(string javascriptSourceCode)
		{
			return GetHtmlFromHighlighterResult("JavaScript", javascriptSourceCode);
		}

		public static string FromPascal(string pascalSourceCode)
		{
			return GetHtmlFromHighlighterResult("Pascal", pascalSourceCode);
		}

		public static string FromPhp(string phpSourceCode)
		{
			return GetHtmlFromHighlighterResult("PHP", phpSourceCode);
		}

		public static string FromSQL(string sqlSourceCode)
		{
			return GetHtmlFromHighlighterResult("SQL", sqlSourceCode);
		}

		public static string FromXML(string xmlSourceCode)
		{
			return GetHtmlFromHighlighterResult("XML", xmlSourceCode);
		}
	}
}