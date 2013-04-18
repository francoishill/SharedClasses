using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualBasic.Devices;

namespace SharedClasses
{
	/// <summary>
	/// Interaction logic for ResourceUsageChart.xaml
	/// </summary>
	public partial class ResourceUsageChart : Window
	{
		private Func<IEnumerable<ResourceUsageTracker.GroupedResourceUsages>> _funcToObtainGroupedResourceUsages;
		Dictionary<DateTime, double> memoryUsagesForPastWeek = new Dictionary<DateTime, double>();
		Dictionary<DateTime, double> cpuLoadsForPastWeek = new Dictionary<DateTime, double>();

		public ResourceUsageChart(Func<IEnumerable<ResourceUsageTracker.GroupedResourceUsages>> funcToObtainGroupedResourceUsages)
		{
			InitializeComponent();

			_funcToObtainGroupedResourceUsages = funcToObtainGroupedResourceUsages;
		}

		private void Window_Loaded_1(object sender, RoutedEventArgs e)
		{
			if (_funcToObtainGroupedResourceUsages == null) return;

			this.UpdateLayout();

			comboboxCurrentGroup.ItemsSource = _funcToObtainGroupedResourceUsages();

			//xAxisDate.IntervalType = Visifire.Charts.IntervalTypes.Auto;//.Auto;//.Days;
			//xAxisDate.Interval = 1;

			yAxisMemoryUsage.AxisMinimum = 0;
			yAxisMemoryUsage.AxisMaximum = (int)(new ComputerInfo().TotalPhysicalMemory / (1024 * 1024 * 1024));
			yAxisMemoryUsage.Interval = 2;

			yAxisCpuLoad.AxisMinimum = 0;
			yAxisCpuLoad.AxisMaximum = 100;
			yAxisCpuLoad.Interval = 10;

			/*var minMemoryDate = memoryUsagesRecorded.Min(kv => kv.Key);
			var maxMemoryDate = memoryUsagesRecorded.Max(kv => kv.Key);
			var minCpuDate = cpuLoadsRecorded.Min(kv => kv.Key);
			var maxCpuDate = cpuLoadsRecorded.Max(kv => kv.Key);

			dateTimeAxis1.Minimum = DateTime.MinValue;//We need to otherwise we might set the minimum to larger then the maximum
			dateTimeAxis1.Maximum = DateTime.MaxValue;

			dateTimeAxis1.Minimum = minMemoryDate < minCpuDate ? minMemoryDate : minCpuDate;
			dateTimeAxis1.Maximum = maxMemoryDate > maxCpuDate ? maxMemoryDate : maxCpuDate;

			memorySeries.DataContext = memoryUsagesRecorded;
			cpuLoadSeries.DataContext = cpuLoadsRecorded;*/
		}

		private void comboboxCurrentGroup_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
		{
			var groupedResourceUsages = comboboxCurrentGroup.SelectedItem as ResourceUsageTracker.GroupedResourceUsages;
			if (groupedResourceUsages == null) return;

			//var minDate = groupedResourceUsages.ListOfResourceUsageSnapshots.Min(snapshot => snapshot.SnapshotTime);
			//var maxDate = groupedResourceUsages.ListOfResourceUsageSnapshots.Max(snapshot => snapshot.SnapshotTime);

			xAxisDate.AxisMinimum = DateTime.MinValue;
			xAxisDate.AxisMaximum = DateTime.Now.Add(TimeSpan.FromDays(10));

			//memorySeries.DataSource = groupedResourceUsages.ListOfResourceUsageSnapshots;
			memorySeries.DataContext = groupedResourceUsages.ListOfResourceUsageSnapshots;

			//cpuLoadSeries.DataSource = groupedResourceUsages.ListOfResourceUsageSnapshots;
			cpuLoadSeries.DataContext = groupedResourceUsages.ListOfResourceUsageSnapshots;

			logMessagesSeries.DataContext = groupedResourceUsages.ListOfResourceUsageSnapshots.Where(snap => !string.IsNullOrWhiteSpace(snap.LoggedStatus));

			xAxisDate.AxisMinimum = groupedResourceUsages.DateRange.Key;//minDate;
			xAxisDate.AxisMaximum = groupedResourceUsages.DateRange.Value;//maxDate;

			TimeSpan rangeDuration = groupedResourceUsages.DateRange.Value.Subtract(groupedResourceUsages.DateRange.Key);
			if (rangeDuration.TotalHours < 6)
			{
				xAxisDate.IntervalType = Visifire.Charts.IntervalTypes.Minutes;
				//xAxisDate.Interval = 10;
				xAxisDate.ValueFormatString = @"HH\hmm:ss";
			}
			else if (rangeDuration.TotalDays < 1)
			{
				xAxisDate.IntervalType = Visifire.Charts.IntervalTypes.Hours;
				//xAxisDate.Interval = 1;
				xAxisDate.ValueFormatString = @"HH\hmm:ss";
			}
			else if (rangeDuration.TotalDays < 2)
			{
				xAxisDate.IntervalType = Visifire.Charts.IntervalTypes.Hours;
				//xAxisDate.Interval = 2;
				xAxisDate.ValueFormatString = @"yyyy-MM-dd @ HH\hmm:ss";
			}
			else
			{
				xAxisDate.IntervalType = Visifire.Charts.IntervalTypes.Days;
				/*if (rangeDuration.TotalDays < 10)
					xAxisDate.Interval = 1;
				else if (rangeDuration.TotalDays < 21)
					xAxisDate.Interval = 2;
				else
					xAxisDate.Interval = 7;*/
				xAxisDate.ValueFormatString = @"yyyy-MM-dd @ HH\hmm:ss";
			}

			//trendLineMaximumMemory.Value = groupedResourceUsages.ListOfResourceUsageSnapshots.Max(snap => snap.MemoryUsedInGB);
			//trendLineMaximumCpuLoad.Value = groupedResourceUsages.ListOfResourceUsageSnapshots.Max(snap => snap.CpuLoadPercentage);

			//xAxisDate.Zoom(xAxisDate.AxisMinimum, xAxisDate.AxisMaximum);
		}
	}
}
