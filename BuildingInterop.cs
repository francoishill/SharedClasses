using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;

namespace SharedClasses
{
	public class VSBuildProject_NonAbstract : VSBuildProject
	{
		public VSBuildProject_NonAbstract(string ApplicationName) : base(ApplicationName) { }
	}

	public abstract class VSBuildProject
	{
		private static bool? ChecksAlreadyDone = null;

		public virtual string ApplicationName { get; set; }
		public virtual string LastBuildFeedback { get; set; }
		public virtual bool? LastBuildResult { get; set; }
		public virtual bool HasFeedbackText { get; set; }

		public string SolutionFullpath { get; protected set; }

		public VSBuildProject(string ApplicationName)
		{
			this.ApplicationName = Path.GetFileNameWithoutExtension(ApplicationName);
			this.LastBuildFeedback = null;
			this.HasFeedbackText = false;
			this.LastBuildResult = null;

			string err;
			this.SolutionFullpath = GetSolutionPathFromApplicationName(out err);
			if (err != null)
				UserMessages.ShowErrorMessage("Error getting solution path: " + err);
		}

		public virtual string GetSolutionPathFromApplicationName(out string errorIfFailed)
		{
			string solutionDir = Path.Combine(RootVSprojectsDir, this.ApplicationName);
			var solutionFiles = Directory.GetFiles(solutionDir, "*.sln");//Full paths
			if (solutionFiles.Length == 0)
			{
				errorIfFailed = "Cannot find solution file in dir: " + solutionDir;
				return null;
			}
			else
			{
				var solutionFilesSameNameAsApplicationName =
						solutionFiles.Where(f => Path.GetFileNameWithoutExtension(f).Equals(this.ApplicationName)).ToArray();
				if (solutionFiles.Length > 1 && solutionFilesSameNameAsApplicationName.Length > 1)
				{
					errorIfFailed = "Multiple solution files found for application "
						+ this.ApplicationName + ", none of them having same name as application:"
						+ Environment.NewLine
						+ string.Join(Environment.NewLine, solutionFiles);
					return null;

				}
				//else if (solutionFiles.Length == 1 || solutionFilesSameNameAsApplicationName.Length == 1)
				//{
				errorIfFailed = null;
				return solutionFiles.Length == 1 ? solutionFiles.First() : solutionFilesSameNameAsApplicationName.First();
			}
		}
		private string rootVSprojectsDir;
		public virtual string RootVSprojectsDir
		{
			get
			{
				if (rootVSprojectsDir == null)//Checks was not done yet
				{
					rootVSprojectsDir = @"C:\Francois\Dev\VSprojects";
					if (!Directory.Exists(rootVSprojectsDir))
					{
						UserMessages.ShowErrorMessage("Please ensure projects are in directory (dir not found): " + rootVSprojectsDir);
						rootVSprojectsDir = null;
					}
				}
				return rootVSprojectsDir;
			}
		}
		private class MyLogger : ILogger
		{
			private Action<BuildErrorEventArgs> OnBuildError;
			private Action<ProjectStartedEventArgs> OnProjectStarted;
			public MyLogger(Action<BuildErrorEventArgs> OnBuildError, Action<ProjectStartedEventArgs> OnProjectStarted)
			{
				this.OnBuildError = OnBuildError;
				this.OnProjectStarted = OnProjectStarted;
			}

			public void Initialize(IEventSource eventSource)
			{
				if (OnBuildError != null)
					eventSource.ErrorRaised += (snd, evt) => OnBuildError(evt);
				if (OnProjectStarted != null)
					eventSource.ProjectStarted += (snd, evt) => OnProjectStarted(evt);
			}

			public string Parameters { get; set; }

			public void Shutdown()
			{
				//
			}

			public LoggerVerbosity Verbosity { get; set; }
		}

		private static bool IsBusyBuilding = false;//Can only run one build at time (microsoft limitation)
		/// <summary>
		/// Builds and returns error
		/// </summary>
		/// <param name="errorIfFail">Returns an error string if the result was FALSE</param>
		/// <returns>Returns null if succeeded, otherwise error</returns>
		public bool PerformBuild(out List<string> csprojectPaths, out string errorIfFail)
		{
			if (IsBusyBuilding)
			{
				errorIfFail = "Cannot build " + this.ApplicationName + ", another build is already in progress.";
				csprojectPaths = null;
				return false;
			}

			if (SolutionFullpath == null)
			{
				errorIfFail = "SolutionFullPath is null for application " + this.ApplicationName + ", cannot build project";
				csprojectPaths = null;
				return false;
			}

			IsBusyBuilding = true;

			try
			{
				this.LastBuildFeedback = null;
				this.HasFeedbackText = true;//Just for incase
				this.LastBuildResult = null;

				if (!ChecksAlreadyDone.HasValue)
					ChecksAlreadyDone = RootVSprojectsDir != null;
				if (ChecksAlreadyDone == false)
				{
					errorIfFail = "Cannot find RootVisualStudio path";
					csprojectPaths = null;
					return false;
				}

				string projectFileName = this.SolutionFullpath;//@"...\ConsoleApplication3\ConsoleApplication3.sln";
				ProjectCollection pc = new ProjectCollection();
				Dictionary<string, string> GlobalProperty = new Dictionary<string, string>();
				GlobalProperty.Add("Configuration", "Release");
				GlobalProperty.Add("Platform", "Any CPU");//"x86");

				BuildRequestData BuidlRequest = new BuildRequestData(projectFileName, GlobalProperty, null, new string[] { "Build" }, null);

				List<string> csprojectPathsCaughtMatchingSolutionName = new List<string>();
				List<string> buildErrorsCaught = new List<string>();
				BuildResult buildResult = BuildManager.DefaultBuildManager.Build(
					new BuildParameters(pc)
					{
						DetailedSummary = true,
						Loggers = new ILogger[]
					{
						new MyLogger(
							(builderr) =>
								buildErrorsCaught.Add(
								string.Format("Could not build '{0}',{1}Error in {2}: line {3},{1}Error message: '{4}'",
									builderr.ProjectFile, Environment.NewLine, builderr.File, builderr.LineNumber, builderr.Message)
								),
							(projstarted) =>
								csprojectPathsCaughtMatchingSolutionName.AddRange(projstarted.Items.Cast<DictionaryEntry>()
								.Where(de => de.Key is string && (de.Key ?? "").ToString().Equals("ProjectReference", StringComparison.InvariantCultureIgnoreCase))
								.Select(de => de.Value.ToString())
								.Where(csprojpath => Path.GetFileNameWithoutExtension(csprojpath).Equals(Path.GetFileNameWithoutExtension(SolutionFullpath), StringComparison.InvariantCultureIgnoreCase))))
					}
					},
					BuidlRequest);

				if (buildResult.OverallResult == BuildResultCode.Success && csprojectPathsCaughtMatchingSolutionName.Count > 0)
				{
					this.HasFeedbackText = false;
					errorIfFail = null;
					csprojectPaths = csprojectPathsCaughtMatchingSolutionName;
					return true;
				}
				else
				{
					this.HasFeedbackText = true;
					string nowString = DateTime.Now.ToString("HH:mm:ss.fff");
					if (csprojectPathsCaughtMatchingSolutionName.Count == 0 && buildResult.OverallResult == BuildResultCode.Success)//Build successfully but could not obtain csProject filepaths
						this.LastBuildFeedback = string.Format("[{0}] Build successfully but could not obtain .csproj path(s) for solution: {1}", nowString, SolutionFullpath);
					else if (buildErrorsCaught.Count == 0)
						this.LastBuildFeedback = string.Format("[{0}] Unknown error to build " + this.ApplicationName, nowString);
					else
						this.LastBuildFeedback = string.Format("[{0}] Build failed for " + this.ApplicationName, nowString)
							+ Environment.NewLine + string.Join(Environment.NewLine, buildErrorsCaught);
					errorIfFail = this.LastBuildFeedback;
					csprojectPaths = null;
					return false;
				}
			}
			finally
			{
				IsBusyBuilding = false;
			}
		}

		public bool PerformPublish(Action<string, FeedbackMessageTypes> actionOnMessage, Action<int> actionOnProgressPercentage)
		{
			int seeFollowingTodoItems;

			string outPublishedVersion;
			string resultSetupFilename;
			bool publishResult = PublishInterop.PerformPublish(
				this.ApplicationName,
				false,//TODO: What if required
				false,//TODO: What about QuickAccess
				true,
				true,//TODO: Always install locally?
				false,//Never run on startup?
				false,
				out outPublishedVersion,
				out resultSetupFilename,
				actionOnMessage,
				actionOnProgressPercentage);
			return publishResult;
		}

		public bool PerformPublishOnline(Action<string, FeedbackMessageTypes> actionOnMessage, Action<int> actionOnProgressPercentage)
		{
			string outPublishedVersion;
			string resultSetupFilename;
			bool publishResult = PublishInterop.PerformPublishOnline(
				this.ApplicationName,
				false,//TODO: What if required
				false,//TODO: What about QuickAccess
				true,
				true,//TODO: Always install locally?
				false,//Never run on startup?
				false,
				false,
				out outPublishedVersion,
				out resultSetupFilename,
				actionOnMessage,
				actionOnProgressPercentage);
			return publishResult;
		}
	}
}