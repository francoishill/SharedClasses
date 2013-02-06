using System;
using NUnit.Framework;
using System.Collections.Generic;

namespace SharedClasses
{
	public static class TestsForSharedClasses
	{
		#region Tests
		[TestFixture]
		public class Tests
		{
			/*[Test]
			public void TestStringIndexOfIgnoreInsideStringOrChar()
			{
			}*/

			private readonly Dictionary<string, string> _removeCommentCasesDictionary = new Dictionary<string, string>()
			{
				{ "/**/", "" },
				{ "/**/ ", " " },
				{ "/*//In comment*/Outside comment", "Outside comment" },
				{ "/* This\r\nis\r\na\r\ntest*/Outside comment", "Outside comment" },
				{ "var a = \"apple\"; // test or /* test */", "var a = \"apple\"; " },
				{ "Before comment/* This // is a test /, or // This / is a test */Outside comment", "Before commentOutside comment" },
				{ "\"/* This is a test*/\"", "\"/* This is a test*/\"" },
				{ "var url = \"http://stackoverflow.com\";//This is a comment", "var url = \"http://stackoverflow.com\";" },
				{ "var abc = @\" this /* \r\n is a comment in quote\r\n*/\"//But this is a comment", "var abc = @\" this /* \r\n is a comment in quote\r\n*/\"" },
				{ "var abc = @\" this/*\r\n is a comment in quote\r\n */ \"//But this is a comment", "var abc = @\" this/*\r\n is a comment in quote\r\n */ \"" },
			};
			private static void TestCaseForRemovingComments(string fileContent, string expectedAfterCommentsRemoved)
			{
				var origContent = fileContent;
				OwnAppsInterop.RemoveCommentsInCsFile(ref fileContent);
				Console.Out.WriteLine("Testing case for RemoveCommentsInCsFile '" + origContent + "', expecting result '" + expectedAfterCommentsRemoved + "'");
				StringAssert.AreEqualIgnoringCase(fileContent, expectedAfterCommentsRemoved);
				/*Assert.IsTrue(fileContent == expectedAfterCommentsRemoved, 
					"Removing comments for case '" + origContent + "' failed, the result was '" + fileContent + "'.");*/
			}

			private readonly Dictionary<string, KeyValuePair<int, bool>> _indexIsInsideStringDictionary = new Dictionary<string, KeyValuePair<int, bool>>()
			{
				{ "\"This is a string\"", new KeyValuePair<int, bool>(7, true) },
				{ "\"\"This is not a string", new KeyValuePair<int, bool>(7, false) },
				{ "@\"This is a string\"", new KeyValuePair<int, bool>(7, true) },
				{ "@\"This is a string\r\nThis part is on a new line\"", new KeyValuePair<int, bool>(25, true) },
				{ "@\"\"This is not a string", new KeyValuePair<int, bool>(7, false) },
			};

			private static void TestCaseWhetherIndexIsInsideString(string fileContent, int needleIndex, bool expectedToBeInsideString)
			{
				Console.Out.WriteLine("Testing case for IsIndexInsideString '" + fileContent + "', index = " + needleIndex
					+ ", expected to " + (expectedToBeInsideString ? "" : "NOT ") + "be inside a string");
				var isIndexInsideString = OwnAppsInterop.IsIndexInsideString(OwnAppsInterop.StringTypes.Both, ref fileContent, needleIndex, 1);
				Assert.IsTrue(expectedToBeInsideString == isIndexInsideString,
					"Expected index to " + (expectedToBeInsideString ? "" : "NOT ") + "be inside a string, index = " + needleIndex
					+ ", fileContent = '" + fileContent + "'");
			}

			[Test(Description = "Testing whether the method 'RemoveCommentsInCsFile' works correct.")]
			public void TestRemoveCommentsInCsFile()
			{
				foreach (var tmpcase in _removeCommentCasesDictionary)
					TestCaseForRemovingComments(tmpcase.Key, tmpcase.Value);
			}

			[Test(Description = "Testing whether the method 'IsIndexInsideString' works correct.")]
			public void TestIsIndexInsideString()
			{
				foreach (var tmpcase in _indexIsInsideStringDictionary)
					TestCaseWhetherIndexIsInsideString(tmpcase.Key, tmpcase.Value.Key, tmpcase.Value.Value);
			}
		}
		#endregion Tests
	}
}