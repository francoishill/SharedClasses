using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
		public virtual bool HasErrors { get; set; }

		protected string SolutionFullpath;

		public VSBuildProject(string ApplicationName)
		{
			this.ApplicationName = Path.GetFileNameWithoutExtension(ApplicationName);
			this.LastBuildFeedback = null;
			this.HasErrors = false;
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
		public class MyLogger : ILogger
		{
			private Action<BuildErrorEventArgs> OnBuildError;
			public MyLogger(Action<BuildErrorEventArgs> OnBuildError)
			{
				this.OnBuildError = OnBuildError;
			}

			public void Initialize(IEventSource eventSource)
			{
				if (OnBuildError != null)
					eventSource.ErrorRaised += (snd, evt) => OnBuildError(evt);
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
		/// <returns>Returns null if succeeded, otherwise error</returns>
		public string PerformBuild()
		{
			if (IsBusyBuilding)
				return "Cannot build " + this.ApplicationName + ", another build is already in progress.";

			if (SolutionFullpath == null)
				return null;

			IsBusyBuilding = true;

			try
			{
				this.LastBuildFeedback = null;
				this.HasErrors = true;//Just for incase
				this.LastBuildResult = null;

				if (!ChecksAlreadyDone.HasValue)
					ChecksAlreadyDone = RootVSprojectsDir != null;
				if (ChecksAlreadyDone == false)
					return "Cannot find RootVisualStudio path";

				string projectFileName = this.SolutionFullpath;//@"...\ConsoleApplication3\ConsoleApplication3.sln";
				ProjectCollection pc = new ProjectCollection();
				Dictionary<string, string> GlobalProperty = new Dictionary<string, string>();
				GlobalProperty.Add("Configuration", "Release");
				GlobalProperty.Add("Platform", "Any CPU");//"x86");

				BuildRequestData BuidlRequest = new BuildRequestData(projectFileName, GlobalProperty, null, new string[] { "Build" }, null);

				List<string> buildErrorsCatched = new List<string>();
				BuildResult buildResult = BuildManager.DefaultBuildManager.Build(
					new BuildParameters(pc)
					{
						DetailedSummary = true,
						Loggers = new ILogger[]
					{
						new MyLogger((builderr)
							=> buildErrorsCatched.Add(
							string.Format("Could not build '{0}',{1}Error in {2}: line {3},{1}Error message: '{4}'",
								builderr.ProjectFile, Environment.NewLine, builderr.File, builderr.LineNumber, builderr.Message)
							)) 
					}
					},
					BuidlRequest);

				if (buildResult.OverallResult == BuildResultCode.Success)
				{
					this.HasErrors = false;
					return null;
				}
				else
				{
					this.HasErrors = true;
					string nowString = DateTime.Now.ToString("HH:mm:ss.fff");
					if (buildErrorsCatched.Count == 0)
						this.LastBuildFeedback = string.Format("[{0}] Unknown error to build " + this.ApplicationName, nowString);
					else
						this.LastBuildFeedback = string.Format("[{0}] Build failed for " + this.ApplicationName, nowString)
							+ Environment.NewLine + string.Join(Environment.NewLine, buildErrorsCatched);
					return this.LastBuildFeedback;
				}

				//return null;
			}
			finally
			{
				IsBusyBuilding = false;
			}
		}
	}
}