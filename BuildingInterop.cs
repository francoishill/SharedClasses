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
	public class VsBuildProject_NonAbstract : VsBuildProject
	{
		public VsBuildProject_NonAbstract(string applicationName, string csprojOrSolutionFullpath = null) : base(applicationName, csprojOrSolutionFullpath) { }
	}

	public abstract class VsBuildProject
	{
		/* Additional dependencies for this file:
			Class: SharedClassesSettings
			Assembly: Microsoft.Build
			Assembly: Microsoft.Build.Framework*/

		public enum StatusTypes { Normal, Queued, Busy, Success, Error, Warning };
		public static Action<VsBuildProject, string, FeedbackMessageTypes> ActionOnFeedbackMessageReceived = delegate { };
		public static Action<VsBuildProject, int?> ActionOnProgressPercentageChanged = delegate { };

		private readonly static Dictionary<string, string> GlobalBuildProperties = new Dictionary<string, string>()
		{
			{ "Configuration", "Release" },
			{ "Platform", "Any CPU" },
			//{ "Platform", "x86" },
		};

		private static bool? _checksAlreadyDone = null;

		public virtual string ApplicationName { get; set; }
		public virtual string CurrentStatusText { get; set; }
		public virtual StatusTypes CurrentStatus { get; set; }
		public virtual int? CurrentProgressPercentage { get; set; }
		//public virtual bool? LastBuildResult { get; set; }
		public virtual bool HasFeedbackText { get { return !string.IsNullOrWhiteSpace(CurrentStatusText); } /*set; */}

		public string PublishedSetupPath { get; private set; }

		public string SolutionFullpath { get; protected set; }
		public string GetSolutionDirectory() { return Path.GetDirectoryName(SolutionFullpath); }

		public VsBuildProject(string ApplicationName, string CsprojOrSolutionFullpath = null)
		{
			//if (ActionOnFeedbackMessageReceived == null) ActionOnFeedbackMessageReceived = errms => UserMessages.ShowErrorMessage(errms);

			this.ApplicationName = Path.GetFileNameWithoutExtension(ApplicationName);
			this.CurrentStatusText = null;
			//this.LastBuildResult = null;

			string err = null;
			this.SolutionFullpath = CsprojOrSolutionFullpath ?? OwnAppsInterop.GetSolutionPathFromApplicationName(this.ApplicationName, out err);
			if (err != null)
				OnFeedbackMessage(err, FeedbackMessageTypes.Error);
		}

		public void ResetStatus(bool markAsQueued)
		{
			this.CurrentStatusText = null;
			/*if (setIndeterminateProgress)
			{
				this.CurrentProgressPercentage = null;
				this.CurrentStatus = StatusTypes.Busy;
			}
			else
			{*/
			this.CurrentProgressPercentage = 0;
			if (markAsQueued)
				this.CurrentStatus = StatusTypes.Queued;
			else
				this.CurrentStatus = StatusTypes.Normal;
			//}
		}

		public void MarkAsBusy()
		{
			this.CurrentProgressPercentage = null;
			this.CurrentStatus = StatusTypes.Busy;
		}

		public void MarkAsComplete()
		{
			this.CurrentProgressPercentage = 0;
			if (this.CurrentStatus == StatusTypes.Busy)
				this.CurrentStatus = StatusTypes.Normal;
		}

		public void AppendCurrentStatusText(string textToAppend)
		{
			if (!string.IsNullOrWhiteSpace(this.CurrentStatusText))
				this.CurrentStatusText += Environment.NewLine;
			this.CurrentStatusText += textToAppend;
		}

		#region OnFeedback events
		#region Text messages
		public static void OnFeedbackMessage(VsBuildProject buildapp, string message, FeedbackMessageTypes messageType)
		{
			switch (messageType)
			{
				case FeedbackMessageTypes.Success:
					if (buildapp.CurrentStatus != StatusTypes.Error
						&& buildapp.CurrentStatus != StatusTypes.Warning)//Only set success if its not Error/Warning
						buildapp.CurrentStatus = StatusTypes.Success;
					break;
				case FeedbackMessageTypes.Error:
					buildapp.CurrentStatus = StatusTypes.Error;
					break;
				case FeedbackMessageTypes.Warning:
					if (buildapp.CurrentStatus != StatusTypes.Error)//Only set warning if its not Error
						buildapp.CurrentStatus = StatusTypes.Warning;
					break;
				case FeedbackMessageTypes.Status:
					break;
				default:
					UserMessages.ShowWarningMessage("Cannot use messagetype = " + messageType.ToString());
					break;
			}
			buildapp.AppendCurrentStatusText(message);
			ActionOnFeedbackMessageReceived(buildapp, message, messageType);
		}
		public static void OnErrorMessage(VsBuildProject buildapp, string errMessage)
		{
			OnFeedbackMessage(buildapp, errMessage, FeedbackMessageTypes.Error);
		}

		public void OnFeedbackMessage(string message, FeedbackMessageTypes messageType)
		{
			OnFeedbackMessage(this, message, messageType);
		}
		public void OnErrorMessage(string errMessage)
		{
			OnFeedbackMessage(errMessage, FeedbackMessageTypes.Error);
		}
		#endregion Text messages

		#region Progress changes
		public static void OnProgressPercentageChanged(VsBuildProject buildapp, int? newProgressValue)
		{
			buildapp.CurrentProgressPercentage = newProgressValue;
			ActionOnProgressPercentageChanged(buildapp, newProgressValue);
		}
		public void OnProgressPercentageChanged(int? newProgressValue)
		{
			OnProgressPercentageChanged(this, newProgressValue);
		}
		#endregion Progress changes
		#endregion OnFeedback events

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

		public static Dictionary<VsBuildProject, bool> PerformMultipleBuild(IEnumerable<VsBuildProject> projects, out Dictionary<VsBuildProject, string> errorsIfFail, Action<VsBuildProject> onBuildStart, Action<VsBuildProject, bool> onBuildComplete)
		{
			if (onBuildStart == null) onBuildStart = delegate { };
			if (onBuildComplete == null) onBuildComplete = delegate { };

			errorsIfFail = new Dictionary<VsBuildProject, string>();
			foreach (var app in projects)
				errorsIfFail.Add(app, string.Empty);

			Dictionary<int, VsBuildProject> submissionIDs = new Dictionary<int, VsBuildProject>();
			Dictionary<VsBuildProject, List<string>> buildErrorsCaught = new Dictionary<VsBuildProject, List<string>>();
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
			Dictionary<VsBuildProject, BuildSubmission> submissions = new Dictionary<VsBuildProject, BuildSubmission>();

			//string parallelErrIfFail = string.Empty;
			//var projArray = projects.ToArray();
			foreach (var proj in projects)
			//Parallel.For(0, projArray.Length - 1, (i) =>
			{
				onBuildStart(proj);

				//var proj = projArray[i];
				if (!_checksAlreadyDone.HasValue)
					_checksAlreadyDone = OwnAppsInterop.RootVSprojectsDir != null;
				if (_checksAlreadyDone == false)
				{
					errorsIfFail[proj] += "Cannot find RootVisualStudio path" + Environment.NewLine;
					continue;
				}

				if (proj.SolutionFullpath == null)
				{
					errorsIfFail[proj] += "SolutionFullPath is null for application " + proj.ApplicationName + ", cannot build project" + Environment.NewLine;
					continue;
				}

				proj.ResetStatus(true);

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
					errorIfFail = null;
					csprojectPaths = csprojectPathsCaughtMatchingSolutionName;
					return true;
				}
				else
				{
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

			var tmpResult = new Dictionary<VsBuildProject, bool>();
			var submissionKeys = submissions.Keys.ToList();
			while (submissionKeys.Count > 0)// && !submissions[0].IsCompleted)
			{
				for (int i = submissionKeys.Count - 1; i >= 0; i--)
					if (submissions[submissionKeys[i]].IsCompleted)
					{
						var proj = submissionIDs[submissions[submissionKeys[i]].SubmissionId];

						if (submissions[proj].BuildResult.OverallResult == BuildResultCode.Success)// && csprojectPathsCaughtMatchingSolutionName.Count > 0)
						{
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

							string nowString = DateTime.Now.ToString("HH:mm:ss.fff");
							/*if (csprojectPathsCaughtMatchingSolutionName.Count == 0 && buildResult.OverallResult == BuildResultCode.Success)//Build successfully but could not obtain csProject filepaths
								this.LastBuildFeedback = string.Format("[{0}] Build successfully but could not obtain .csproj path(s) for solution: {1}", nowString, SolutionFullpath);
							else */
							if (buildErrorsCaught[proj].Count == 0)
								proj.CurrentStatusText = string.Format("[{0}] Unknown error to build " + proj.ApplicationName, nowString);
							else
								proj.CurrentStatusText = string.Format("[{0}] Build failed for " + proj.ApplicationName, nowString)
							 + Environment.NewLine + string.Join(Environment.NewLine, buildErrorsCaught[proj]);
							errorsIfFail[proj] += proj.CurrentStatusText + Environment.NewLine;
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
			//{
			var tmpErrKeys = errorsIfFail.Keys.ToList();
			foreach (var k in tmpErrKeys)
				errorsIfFail[k] = errorsIfFail[k].TrimEnd('\n', '\r');
			//}

			return tmpResult;
		}

		//private static bool IsBusyBuilding = false;//Can only run one build at time (microsoft limitation)
		/// <summary>
		/// Builds and returns error
		/// </summary>
		/// <param name="errorIfFail">Returns an error string if the result was FALSE</param>
		/// <returns>Returns null if succeeded, otherwise error</returns>
		public bool PerformBuild(out List<string> csprojectPaths)
		{
			/*if (IsBusyBuilding)
			{
				errorIfFail = "Cannot build " + this.ApplicationName + ", another build is already in progress.";
				csprojectPaths = null;
				return false;
			}*/

			if (SolutionFullpath == null)
			{
				OnFeedbackMessage("SolutionFullPath is null for application " + this.ApplicationName + ", cannot build project", FeedbackMessageTypes.Error);
				csprojectPaths = null;
				return false;
			}

			//IsBusyBuilding = true;

			//try
			//{
			this.ResetStatus(true);

			if (!_checksAlreadyDone.HasValue)
				_checksAlreadyDone = OwnAppsInterop.RootVSprojectsDir != null;
			if (_checksAlreadyDone == false)
			{
				OnFeedbackMessage("Cannot find RootVisualStudio path", FeedbackMessageTypes.Error);
				csprojectPaths = null;
				return false;
			}

			string projectFileName = this.SolutionFullpath;//@"...\ConsoleApplication3\ConsoleApplication3.sln";
			ProjectCollection pc = new ProjectCollection();
			Dictionary<string, string> buildGlobalProperties = new Dictionary<string, string>();
			foreach (var key in GlobalBuildProperties.Keys)
				buildGlobalProperties.Add(key, GlobalBuildProperties[key]);
			if (GlobalBuildProperties.ContainsKey("Platform") && GlobalBuildProperties["Platform"].Equals("x86", StringComparison.InvariantCultureIgnoreCase))
				OnFeedbackMessage("Publishing in 32bit (x86) mode only", FeedbackMessageTypes.Warning);
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
								OnFeedbackMessage(errMsg, FeedbackMessageTypes.Error);
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
			OnFeedbackMessage("Project started to build", FeedbackMessageTypes.Status);
			// Wait for the build to finish.
			submission.WaitHandle.WaitOne();

			BuildManager.DefaultBuildManager.EndBuild();

			bool successFullyBuilt = submission.BuildResult.OverallResult == BuildResultCode.Success;
			if (successFullyBuilt && csprojectPathsCaughtMatchingSolutionName.Count > 0)
			{
				csprojectPaths = csprojectPathsCaughtMatchingSolutionName;
				return true;
			}
			else
			{
				string nowString = DateTime.Now.ToString("HH:mm:ss.fff");
				if (csprojectPathsCaughtMatchingSolutionName.Count == 0 && successFullyBuilt)//Build successfully but could not obtain csProject filepaths
					this.CurrentStatusText = string.Format("[{0}] Build successfully but could not obtain .csproj path(s) for solution: {1}", nowString, SolutionFullpath);
				else if (buildErrorsCaught.Count == 0)
					this.CurrentStatusText = string.Format("[{0}] Unknown error to build " + this.ApplicationName, nowString);
				else
					this.CurrentStatusText = string.Format("[{0}] Build failed for " + this.ApplicationName, nowString)
						+ Environment.NewLine + string.Join(Environment.NewLine, buildErrorsCaught);
				OnFeedbackMessage(this.CurrentStatusText, FeedbackMessageTypes.Error);
				csprojectPaths = null;
				return false;
			}
			//}
			//finally
			//{
			//    IsBusyBuilding = false;
			//}
		}

		public bool PerformPublish(bool _64bit = false, bool autoUpdateRevision = true, bool installLocally = true,
			bool placeSetupInTempWebFolder = false, string customSetupFilename = null)
		{
			string outPublishedVersion;
			string resultSetupFilename;
			DateTime outPublishDate;
			bool publishResult = PublishInterop.PerformPublish(
				this.ApplicationName,
				/*_64bit,//What if required*/
				false,//What about QuickAccess
				autoUpdateRevision,
				installLocally,
				false,//Never run on startup?
				false,
				out outPublishedVersion,
				out resultSetupFilename,
				out outPublishDate,
				OnFeedbackMessage,
				(progperc) => OnProgressPercentageChanged(progperc),
				placeSetupInTempWebFolder,
				customSetupFilename);

			if (publishResult)
				this.PublishedSetupPath = resultSetupFilename;
			else
				this.PublishedSetupPath = null;

			return publishResult;
		}

		public bool PerformPublishOnline()
		{
			string outPublishedVersion;
			string resultSetupFilename;
			DateTime outPublishDate;
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
				out outPublishDate,
				OnFeedbackMessage,
				(progperc) => OnProgressPercentageChanged(progperc));
			return publishResult;
		}
	}
}