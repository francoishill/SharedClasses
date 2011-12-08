using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace SharedClasses
{
	public class ChartInterop_MeterReadings
	{
		//System.Windows.Forms.DataVisualization.Charting.Chart chart = new System.Windows.Forms.DataVisualization.Charting.Chart();

		/// <summary>
		/// Temp chart.
		/// </summary>
		/// <returns>Returns the temp chart.</returns>
		public static Chart TestChart()
		{
			Chart chart = new Chart();
			chart.ChartAreas.Add("Area1");
			chart.Series.Add("Series1");
			chart.Series[0].ChartType = SeriesChartType.Column;

			chart.Series[0].Points.Add(new DataPoint
					(0, 3));
			chart.Series[0].Points.Add(new DataPoint
					(5, 12));

			return chart;
		}

		/// <summary>
		/// A predefined chart type for meter readings with their dates (showing the readings as dots and the dates as columns).
		/// </summary>
		/// <param name="ReadingsList"></param>
		/// <returns></returns>
		public static Chart MeterReadingChart(List<ReadingAndDate> ReadingsList)
		{
			Chart result = new Chart();
			String ChartAreaName = "ReadingsArea";
			result.ChartAreas.Add(ChartAreaName);
			result.ChartAreas[0].AxisX.LabelStyle.Angle = -90;
			result.ChartAreas[0].AxisX.LabelStyle.Format = "yyyy / MM / dd   ";
			result.ChartAreas[0].AxisY2.MajorGrid.Enabled = false;

			result.Series.Add("Readings");
			result.Series[0].XValueType = ChartValueType.DateTime;
			result.Series[0].ChartArea = ChartAreaName;
			result.Series[0].ChartType = SeriesChartType.Point;
			result.Series[0].YAxisType = AxisType.Secondary;
			result.Series[0].Color = System.Drawing.Color.LightGreen;
			result.Series[0].MarkerStyle = MarkerStyle.Circle;
			result.Series[0].MarkerSize = 5;
			result.Series[0].BorderColor = System.Drawing.Color.Black;

			result.Series.Add("Consumptions");
			result.Series[1].XValueType = ChartValueType.DateTime;
			result.Series[1].ChartArea = ChartAreaName;
			result.Series[1].ChartType = SeriesChartType.Column;
			result.Series[1].Color = System.Drawing.Color.Blue;
			result.Series[1].BorderColor = System.Drawing.Color.Black;
			result.Series[1].CustomProperties = "PointWidth=0.2";

			foreach (ReadingAndDate readingitem in ReadingsList)
			{
				DataPoint readingpoint = new DataPoint();
				readingpoint.XValue = readingitem.Date.ToOADate();
				readingpoint.YValues = new double[] { readingitem.Reading };
				result.Series[0].Points.Add(readingpoint);
			}

			if (result.Series[0].Points.Count > 1)
			{
				for (int i = 1; i < result.Series[0].Points.Count; i++)
				{
					DataPoint tmpPoint = result.Series[0].Points[i];
					DataPoint tmpPrevPoint = result.Series[0].Points[i - 1];
					DataPoint tmpConsPoint = new DataPoint((tmpPoint.XValue + tmpPrevPoint.XValue) / 2, tmpPoint.YValues[0] - tmpPrevPoint.YValues[0]);
					result.Series[1].Points.Add(tmpConsPoint);
				}
			}

			return result;
		}

		/// <summary>
		/// A class just for the reading and date.
		/// </summary>
		public class ReadingAndDate
		{
			/// <summary>
			/// The reading (of the meter).
			/// </summary>
			public double Reading;
			/// <summary>
			/// The date of the reading.
			/// </summary>
			public DateTime Date;

			/// <summary>
			/// The constructor, the only constructor required a reading and date.
			/// </summary>
			/// <param name="reading">The reading of the item.</param>
			/// <param name="date">The date of the reading.</param>
			public ReadingAndDate(double reading, DateTime date)
			{
				Reading = reading;
				Date = date;
			}
		}
	}
}
