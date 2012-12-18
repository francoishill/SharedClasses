using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace SharedClasses
{
	public static class RegexInterop
	{
		public delegate string ReplaceRegexMatch(string matchedString);

		public static string RegexReplaceMatches(ref string originalString, string pattern, ReplaceRegexMatch convertOrReplaceRegexMatch, out string errorIfFailed)
		{
			try
			{
				MatchCollection matchesForIncludes = Regex.Matches(originalString, pattern);

				List<Match> matchList = new List<Match>();
				for (int i = 0; i < matchesForIncludes.Count; i++)
					matchList.Add(matchesForIncludes[i]);

				if (matchList.Count > 0)
				{
					StringBuilder newContent = new StringBuilder();
					newContent.Append(originalString.Substring(0, matchList.First().Index));//The part before the first match
					newContent.Append(convertOrReplaceRegexMatch(originalString.Substring(matchList.First().Index, matchList.First().Length)));
					for (int i = 1; i < matchList.Count; i++)//start at 1 as we already used element 0
					{
						//The piece inbetween this match and previous match
						int substringStart = matchList[i - 1].Index + matchList[i - 1].Length;
						int substringLength = matchList[i].Index - substringStart;
						newContent.Append(originalString.Substring(substringStart, substringLength));
						newContent.Append(convertOrReplaceRegexMatch(originalString.Substring(matchList[i].Index, matchList[i].Length)));
					}
					//The final piece after the last match
					int tmpstart = matchList.Last().Index + matchList.Last().Length;
					newContent.Append(originalString.Substring(
						tmpstart,
						originalString.Length - tmpstart));

					errorIfFailed = null;
					return newContent.ToString();
				}
				else
				{
					errorIfFailed = null;
					return originalString;
				}
			}
			catch (Exception exc)
			{
				errorIfFailed = "Exception in RegexReplaceMatches: " + exc.Message;
				return null;
			}
		}
	}
}