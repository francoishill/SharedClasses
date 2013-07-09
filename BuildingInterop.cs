using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;
using System.Threading.Tasks;

namespace SharedClasses
{
	public class VSBuildProject_NonAbstract : VSBuildProject
	{
		public VSBuildProject_NonAbstract(string ApplicationName, string CsprojOrSolutionFullpath = null) : base(ApplicationName, CsprojOrSolutionFullpath) { }
	}

	public abstract class VSBuildProject
	{
		/* Additional dependencies for this file:
			Class: SharedClassesSettings
			Assembly: Microsoft.Build
			Assembly: Microsoft.Build.Framework*/

		private readonly static Dictionary<string, string> GlobalBuildProperties = new Dictionary<string, string>()
		{
			{ "Configuration", "Release" },
			{ "Platform", "Any CPU" },//"x86" },
		};

		private static bool? ChecksAlreadyDone = null;

		public virtual string ApplicationName { get; set; }
		public virtual string LastBuildFeedback { get; set; }
		public virtual bool? LastBuildResult { get; set; }
		public virtual bool HasFeedbackText { get; set; }

		public string PublishedSetupPath { get; private set; }

		public string SolutionFullpath { get; protected set; }
		public string GetSolutionDirectory() { return Path.GetDirectoryName(SolutionFullpath); }

		public VSBuildProject(string ApplicationName, string CsprojOrSolutionFullpath = null, Action<string> actionOnError = null)
		{
			if (actionOnError == null) actionOnError = errms => UserMessages.ShowErrorMessage(errms);

			this.ApplicationName = Path.GetFileNameWithoutExtension(ApplicationName);
			this.LastBuildFeedback = null;
			this.HasFeedbackText = false;
			this.LastBuildResult = null;

			string err = null;
			this.SolutionFullpath = CsprojOrSolutionFullpath ?? OwnAppsInterop.GetSolutionPathFromApplicationName(this.ApplicationName, out err);
			if (err != null)
				actionOnError(err);
		}

		private class MyLogger : ILogger
		{
			private Action<BuildErrorEventArgs> OnBuildError;
			private Action<ProjectStartedEventArgs> OnProjectStarted;
			public MyLogger(Action<BuildErrorEventArgs> OnBuildError, Action<ProjectStartedEventArgs> OnProjectStarted)
			{
				this.OnBuildError = OnBuildError ?? delegate { };
				this.OnProjectStarted = OnProjectStarted ?? delegate { };
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

		public static Dictionary<VSBuildProject, bool> PerformMultipleBuild(IEnumerable<VSBuildProject> projects, out Dictionary<VSBuildProject, string> errorsIfFail, Action<VSBuildProject> onBuildStart, Action<VSBuildProject, bool> onBuildComplete)
		{
			if (onBuildStart == null) onBuildStart = delegate { };
			if (onBuildComplete == null) onBuildComplete = delegate { };

			errorsIfFail = new Dictionary<VSBuildProject, string>();
			foreach (var app in projects)
				errorsIfFail.Add(app, string.Empty);

			Dictionary<int, VSBuildProject> submissionIDs = new Dictionary<int, VSBuildProject>();
			Dictionary<VSBuildProject, List<string>> buildErrorsCaught = new Dictionary<VSBuildProject, List<string>>();
			foreach (var app in projects)
				buildErrorsCaught.Add(app, new List<string>());

			ProjectCollection pc = new ProjectCollection();
			BuildManager.DefaultBuildManager.BeginBuild(
				new BuildParameters(pc)
				{
					DetailedSummary = true,
					Loggers = new ILogger[]
					{
						new MyLogger(
							(builderr) =>
							{
								buildErrorsCaught[submissionIDs[builderr.BuildEventContext.SubmissionId]].Add(
								string.Format("Could not build '{0}',{1}Error in {2}: line {3},{1}Error message: '{4}'",
									builderr.ProjectFile, Environment.NewLine, builderr.File, builderr.LineNumber, builderr.Message));
							},
							null//projstarted
							)
					}
				});
			Dictionary<VSBuildProject, BuildSubmission> submissions = new Dictionary<VSBuildProject, BuildSubmission>();

			//string parallelErrIfFail = string.Empty;
			//var projArray = projects.ToArray();
			foreach (var proj in projects)
			//Parallel.For(0, projArray.Length - 1, (i) =>
			{
				onBuildStart(proj);

				//var proj = projArray[i];
				if (!ChecksAlreadyDone.HasValue)
					ChecksAlreadyDone = OwnAppsInterop.RootVSprojectsDir != null;
				if (ChecksAlreadyDone == false)
				{
					errorsIfFail[proj] += "Cannot find RootVisualStudio path" + Environment.NewLine;
					continue;
				}

				if (proj.SolutionFullpath == null)
				{
					errorsIfFail[proj] += "SolutionFullPath is null for application " + proj.ApplicationName + ", cannot build project" + Environment.NewLine;
					continue;
				}

				proj.LastBuildFeedback = null;
				proj.HasFeedbackText = true;//Just for incase
				proj.LastBuildResult = null;

				string projectFileName = proj.SolutionFullpath;//@"...\ConsoleApplication3\ConsoleApplication3.sln";
				Dictionary<string, string> GlobalProperty = new Dictionary<string, string>();
				foreach (var key in GlobalBuildProperties.Keys)
					GlobalProperty.Add(key, GlobalBuildProperties[key]);

				BuildRequestData buildRequest = new BuildRequestData(projectFileName, GlobalProperty, null, new string[] { "Build" }, null);
				var submission = BuildManager.DefaultBuildManager.PendBuildRequest(buildRequest);
				submissionIDs.Add(submission.SubmissionId, proj);
				submissions.Add(proj, submission);
				submission.ExecuteAsync(null, null);

				/*List<string> buildErrorsCaught = new List<string>();
				//var buildManager = new BuildManager();
				BuildResult buildResult = BuildManager.DefaultBuildManager.Build(
					//BuildResult buildResult = buildManager.Build(
					new BuildParameters(pc)
					{
						DetailedSummary = true
					},
					buildRequest);*/

				/*if (buildResult.OverallResult == BuildResultCode.Success && csprojectPathsCaughtMatchingSolutionName.Count > 0)
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
				}*/
			}//);

			var tmpResult = new Dictionary<VSBuildProject, bool>();
			var submissionKeys = submissions.Keys.ToList();
			while (submissionKeys.Count > 0)// && !submissions[0].IsCompleted)
			{
				for (int i = submissionKeys.Count - 1; i >= 0; i--)
					if (submissions[submissionKeys[i]].IsCompleted)
					{
						var proj = submissionIDs[submissions[submissionKeys[i]].SubmissionId];

						if (submissions[proj].BuildResult.OverallResult == BuildResultCode.Success)// && csprojectPathsCaughtMatchingSolutionName.Count > 0)
						{
							proj.HasFeedbackText = false;
							//errorIfFail = null;
							//csprojectPaths = csprojectPathsCaughtMatchingSolutionName;
							//return true;
							tmpResult.Add(proj, true);
						}
						else
						{
							//int incorrect;
							//The following is not correct
							//the buildErrorsCaught is a global list for all apps, now setting LastBuildFeedback of all apps

							proj.HasFeedbackText = true;
							string nowString = DateTime.Now.ToString("HH:mm:ss.fff");
							/*if (csprojectPathsCaughtMatchingSolutionName.Count == 0 && buildResult.OverallResult == BuildResultCode.Success)//Build successfully but could not obtain csProject filepaths
								this.LastBuildFeedback = string.Format("[{0}] Build successfully but could not obtain .csproj path(s) for solution: {1}", nowString, SolutionFullpath);
							else */
							if (buildErrorsCaught[proj].Count == 0)
								proj.LastBuildFeedback = string.Format("[{0}] Unknown error to build " + proj.ApplicationName, nowString);
							else
								proj.LastBuildFeedback = string.Format("[{0}] Build failed for " + proj.ApplicationName, nowString)
							 + Environment.NewLine + string.Join(Environment.NewLine, buildErrorsCaught[proj]);
							errorsIfFail[proj] += proj.LastBuildFeedback + Environment.NewLine;
							//csprojectPaths = null;
							tmpResult.Add(proj, false);
						}

						onBuildComplete(
							proj,
							submissions[proj].BuildResult.OverallResult == BuildResultCode.Success);

						submissionKeys.RemoveAt(i);
					}
				//if (submissions[submissionKeys[0]].IsCompleted)
				//    submissionKeys.RemoveAt(0);//Only removing from key-list, still needs submissions down below
			}

			BuildManager.DefaultBuildManager.EndBuild();

			/*submissionKeys = submissions.Keys.ToList();
			foreach (var proj in submissionKeys)
			{
				if (submissions[proj].BuildResult.OverallResult == BuildResultCode.Success)// && csprojectPathsCaughtMatchingSolutionName.Count > 0)
				{
					proj.HasFeedbackText = false;
					//errorIfFail = null;
					//csprojectPaths = csprojectPathsCaughtMatchingSolutionName;
					//return true;
					tmpResult.Add(proj, true);
				}
				else
				{
					int incorrect;
					//The following is not correct
					//the buildErrorsCaught is a global list for all apps, now setting LastBuildFeedback of all apps

					proj.HasFeedbackText = true;
					string nowString = DateTime.Now.ToString("HH:mm:ss.fff");
					//if (csprojectPathsCaughtMatchingSolutionName.Count == 0 && buildResult.OverallResult == BuildResultCode.Success)//Build successfully but could not obtain csProject filepaths
					//    this.LastBuildFeedback = string.Format("[{0}] Build successfully but could not obtain .csproj path(s) for solution: {1}", nowString, SolutionFullpath);
					//else 
					if (buildErrorsCaught.Count == 0)
						proj.LastBuildFeedback = string.Format("[{0}] Unknown error to build " + proj.ApplicationName, nowString);
					else
						proj.LastBuildFeedback = string.Format("[{0}] Build failed for " + proj.ApplicationName, nowString)
					 + Environment.NewLine + string.Join(Environment.NewLine, buildErrorsCaught);
					errorsIfFail[proj] += proj.LastBuildFeedback + Environment.NewLine;
					//csprojectPaths = null;
					tmpResult.Add(proj, false);
				}
			}*/

			//errorIfFail = parallelErrIfFail;
			{
				var tmpErrKeys = errorsIfFail.Keys.ToList();
				foreach (var k in tmpErrKeys)
					errorsIfFail[k] = errorsIfFail[k].TrimEnd('\n', '\r');
			}

			return tmpResult;
		}

		//private static bool IsBusyBuilding = false;//Can only run one build at time (microsoft limitation)
		/// <summary>
		/// Builds and returns error
		/// </summary>
		/// <param name="errorIfFail">Returns an error string if the result was FALSE</param>
		/// <returns>Returns null if succeeded, otherwise error</returns>
		public bool PerformBuild(Action<string, FeedbackMessageTypes> onMessage, out List<string> csprojectPaths)
		{
			if (onMessage == null) onMessage = delegate { };

			/*if (IsBusyBuilding)
			{
				errorIfFail = "Cannot build " + this.ApplicationName + ", another build is already in progress.";
				csprojectPaths = null;
				return false;
			}*/

			if (SolutionFullpath == null)
			{
				onMessage("SolutionFullPath is null for application " + this.ApplicationName + ", cannot build project", FeedbackMessageTypes.Error);
				csprojectPaths = null;
				return false;
			}

			//IsBusyBuilding = true;

			//try
			//{
			this.LastBuildFeedback = null;
			this.HasFeedbackText = true;//Just for incase
			this.LastBuildResult = null;

			if (!ChecksAlreadyDone.HasValue)
				ChecksAlreadyDone = OwnAppsInterop.RootVSprojectsDir != null;
			if (ChecksAlreadyDone == false)
			{
				onMessage("Cannot find RootVisualStudio path", FeedbackMessageTypes.Error);
				csprojectPaths = null;
				return false;
			}

			string projectFileName = this.SolutionFullpath;//@"...\ConsoleApplication3\ConsoleApplication3.sln";
			ProjectCollection pc = new ProjectCollection();
			Dictionary<string, string> buildGlobalProperties = new Dictionary<string, string>();
			foreach (var key in GlobalBuildProperties.Keys)
				buildGlobalProperties.Add(key, GlobalBuildProperties[key]);
			if (GlobalBuildProperties.ContainsKey("Platform") && GlobalBuildProperties["Platform"].Equals("x86", StringComparison.InvariantCultureIgnoreCase))
				onMessage("Publishing in 32bit (x86) mode only", FeedbackMessageTypes.Warning);
			//NB, what if we need to publish in Any CPU mode??

			BuildRequestData BuidlRequest = new BuildRequestData(projectFileName, buildGlobalProperties, null, new string[] { "Build" }, null);

			List<string> csprojectPathsCaughtMatchingSolutionName = new List<string>();
			List<string> buildErrorsCaught = new List<string>();
			//var buildManager = new BuildManager();
			//BuildResult buildResult = BuildManager.DefaultBuildManager.Build(
			BuildManager.DefaultBuildManager.BeginBuild(
				//BuildResult buildResult = buildManager.Build(
				new BuildParameters(pc)
				{
					DetailedSummary = true,
					Loggers = new ILogger[]
					{
						new MyLogger(
							(builderr) =>
							{
								string errMsg = string.Format("Could not build '{0}',{1}Error in {2}: line {3},{1}Error message: '{4}'",
									builderr.ProjectFile, /*Environment.NewLine*/"  ", builderr.File, builderr.LineNumber, builderr.Message);
								onMessage(errMsg, FeedbackMessageTypes.Error);
								buildErrorsCaught.Add(errMsg);
							},
							(projstarted) =>
							{
								csprojectPathsCaughtMatchingSolutionName.AddRange(projstarted.Items.Cast<DictionaryEntry>()
								.Where(de => de.Key is string && (de.Key ?? "").ToString().Equals("ProjectReference", StringComparison.InvariantCultureIgnoreCase))
								.Select(de => de.Value.ToString())
								.Where(csprojpath => Path.GetFileNameWithoutExtension(csprojpath).Equals(Path.GetFileNameWithoutExtension(SolutionFullpath), StringComparison.InvariantCultureIgnoreCase)));
							})
					}
				}/*,
				BuidlRequest*/);

			//BuildRequestData request = new BuildRequestData(buildProject.CreateProjectInstance(), new string[0]);
			BuildSubmission submission = BuildManager.DefaultBuildManager.PendBuildRequest(BuidlRequest);//request);
			submission.ExecuteAsync(null, null);
			onMessage("Project started to build", FeedbackMessageTypes.Status);
			// Wait for the build to finish.
			submission.WaitHandle.WaitOne();

			BuildManager.DefaultBuildManager.EndBuild();

			bool successFullyBuilt = submission.BuildResult.OverallResult == BuildResultCode.Success;
			if (successFullyBuilt && csprojectPathsCaughtMatchingSolutionName.Count > 0)
			{
				this.HasFeedbackText = false;
				csprojectPaths = csprojectPathsCaughtMatchingSolutionName;
				return true;
			}
			else
			{
				this.HasFeedbackText = true;
				string nowString = DateTime.Now.ToString("HH:mm:ss.fff");
				if (csprojectPathsCaughtMatchingSolutionName.Count == 0 && successFullyBuilt)//Build successfully but could not obtain csProject filepaths
					this.LastBuildFeedback = string.Format("[{0}] Build successfully but could not obtain .csproj path(s) for solution: {1}", nowString, SolutionFullpath);
				else if (buildErrorsCaught.Count == 0)
					this.LastBuildFeedback = string.Format("[{0}] Unknown error to build " + this.ApplicationName, nowString);
				else
					this.LastBuildFeedback = string.Format("[{0}] Build failed for " + this.ApplicationName, nowString)
						+ Environment.NewLine + string.Join(Environment.NewLine, buildErrorsCaught);
				onMessage(this.LastBuildFeedback, FeedbackMessageTypes.Error);
				csprojectPaths = null;
				return false;
			}
			//}
			//finally
			//{
			//    IsBusyBuilding = false;
			//}
		}

		public bool PerformPublish(Action<string, FeedbackMessageTypes> actionOnMessage, Action<int> actionOnProgressPercentage,
			bool _64bit = false, bool autoUpdateRevision = true, bool installLocally = true,
			bool placeSetupInTempWebFolder = false, string customSetupFilename = null)
		{
			string outPublishedVersion;
			string resultSetupFilename;
			bool publishResult = PublishInterop.PerformPublish(
				this.ApplicationName,
				_64bit,//What if required
				false,//What about QuickAccess
				autoUpdateRevision,
				installLocally,
				false,//Never run on startup?
				false,
				out outPublishedVersion,
				out resultSetupFilename,
				actionOnMessage,
				actionOnProgressPercentage,
				placeSetupInTempWebFolder,
				customSetupFilename);

			if (publishResult)
				this.PublishedSetupPath = resultSetupFilename;
			else
				this.PublishedSetupPath = null;

			return publishResult;
		}

		public bool PerformPublishOnline(Action<string, FeedbackMessageTypes> actionOnMessage, Action<int> actionOnProgressPercentage)
		{
			string outPublishedVersion;
			string resultSetupFilename;
			bool publishResult = PublishInterop.PerformPublishOnline(
				this.ApplicationName,
				false,//What if required
				false,//What about QuickAccess
				true,
				true,
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