using System.Xml;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

namespace SharedClasses
{
	public class HaarDetector
	{

		//--------------------------------------------------------------------------
		// HaarCascadeClassifier > HaarDetector.vb
		//--------------------------------------------------------------------------
		// VB.Net implementation of Viola-Jones Object Detection algorithm
		// Huseyin Atasoy
		// huseyin@atasoyweb.net
		// www.atasoyweb.net
		// July 2012
		//--------------------------------------------------------------------------
		// Copyright 2012 Huseyin Atasoy
		//
		// Licensed under the Apache License, Version 2.0 (the "License");
		// you may not use this file except in compliance with the License.
		// You may obtain a copy of the License at
		//
		//     http://www.apache.org/licenses/LICENSE-2.0
		//
		// Unless required by applicable law or agreed to in writing, software
		// distributed under the License is distributed on an "AS IS" BASIS,
		// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
		// See the License for the specific language governing permissions and
		// limitations under the License.
		//--------------------------------------------------------------------------

		public struct DResults
		{
			public DResults(int SearchedSubRegionCount, int NOfObjects, Rectangle[] DetectedOLocs)
			{
				this.SearchedSubRegionCount = SearchedSubRegionCount;
				// Total number of searched subregions
				this.NOfObjects = NOfObjects;
				// Number of objects
				this.DetectedOLocs = DetectedOLocs ?? new Rectangle[0];
				// Detected objects' locations
			}
			public int SearchedSubRegionCount;
			public int NOfObjects;
			public Rectangle[] DetectedOLocs;
		}

		public struct DetectionParams
		{
			// Detector stops searching when number of detected objects reachs this value.
			public int MaxDetCount;
			// Minimum neighbour areas must be passed the detector to verify existence of searched object.
			public int MinNRectCount;
			// First scaler of searching window.
			public float FirstScale;
			// Maximum scaler of searching window.
			public float MaxScale;
			// ScaleMult (Scale Multiplier) and current scaler are multiplied to make an increment on current scaler.
			public float ScaleMult;
			// SizeMultForNesRectCon (Size Multiplier For Nested Object Control) and size of every rectangle are multiplied separately to obtain maximum acceptable horizontal and vertical distances between current rectangle and others. Maximum distances are used to check if rectangles are nested or not.
			public float SizeMultForNesRectCon;
			// The ratio of step size to scaled searching window width. (CurrentStepSize = ScaledWindowWidth * SlidingRatio)
			public float SlidingRatio;
			// A "Pen" object to draw rectangles on given bitmap. If it is given as null, nothing will be drawn.
			public Pen Pen;
			public DetectionParams(int MaxDetCount, int MinNRectCount, float FirstScale, float MaxScale, float ScaleMult, float SizeMultForNesRectCon, float SlidingRatio, Pen Pen)
			{
				this.MaxDetCount = MaxDetCount;
				this.MinNRectCount = MinNRectCount;
				this.FirstScale = FirstScale;
				this.MaxScale = MaxScale;
				this.ScaleMult = ScaleMult;
				this.SizeMultForNesRectCon = SizeMultForNesRectCon;
				this.SlidingRatio = SlidingRatio;
				this.Pen = Pen;
			}
		}


		private HaarCascade HCascade;
		// Creates a HaarDetector object, parsing opencv xml storage file of which full path is given.
		public HaarDetector(string OpenCVXmlStorage)
		{
			HCascade = new HaarCascade(OpenCVXmlStorage);
		}

		// Creates a HaarDetector object, parsing given xml document. This constructor can be used for loading embedded cascades.
		public HaarDetector(XmlDocument XmlDoc)
		{
			HCascade = new HaarCascade(XmlDoc);
		}

		// Calculate ratio of given size to unscaled searching window size. It can be used to calculate first and max scales of searching window.
		public float Size2Scale(int Size)
		{
			return Convert.ToSingle(Math.Min((double)Size / (double)HCascade.WindowSize.Width, (double)Size / (double)HCascade.WindowSize.Height));
		}

		// For 8 bits per pixel images
		private void CalculateCumSums8bpp(ref int[,] CumSum, ref long[,] CumSum2, ref BitmapData BitmapData, ref int Width, ref int Height)
		{
			int AbsOfStride = FastAbs(BitmapData.Stride);
			int ExtraBPerLine = AbsOfStride - Width;
			int ScanWidthWP = AbsOfStride - ExtraBPerLine;
			// Scan width without padding
			IntPtr BmpDataPtr = BitmapData.Scan0;

			int ByteCount = Convert.ToInt32((float)AbsOfStride * (float)Height);
			byte[] Colors = new byte[ByteCount];
			Marshal.Copy(BmpDataPtr, Colors, 0, ByteCount);

			int CurRowSum = 0;
			long CurRowSum2 = 0;
			int k = Width;
			// image2D(0,1) = image1D(width)   (Skip first row)
			int i = Width + ExtraBPerLine;
			int PosControl = 0;
			// We will start right after first extra bytes
			while (i < ByteCount)
			{
				int GrayVal = Colors[i];
				i = i + 1;

				PosControl = PosControl + 1;
				// If current position is equal to ScanWidthWP now, skip bytes inserted for padding the scan line and zero PosControl for future controls.
				if (PosControl == ScanWidthWP)
				{
					PosControl = 0;
					i = i + ExtraBPerLine;
				}

				int CurRow = Convert.ToInt32(Math.Floor((double)k / (double)Width));
				int CurCol = k % Width;
				if (CurCol == 0)
				{
					CurRowSum = 0;
					CurRowSum2 = 0;
				}
				CurRowSum = CurRowSum + GrayVal;
				CurRowSum2 = CurRowSum2 + Convert.ToInt32((float)GrayVal * (float)GrayVal);
				CumSum[CurCol, CurRow] = CumSum[CurCol, CurRow - 1] + CurRowSum;
				CumSum2[CurCol, CurRow] = CumSum2[CurCol, CurRow - 1] + CurRowSum2;

				k = k + 1;
			}
		}

		// For 24 bits per pixel images
		private void CalculateCumSums24bpp(ref int[,] CumSum, ref long[,] CumSum2, ref BitmapData BitmapData, ref int Width, ref int Height)
		{
			int AbsOfStride = FastAbs(BitmapData.Stride);
			int ExtraBPerLine = AbsOfStride - Convert.ToInt32((double)Width * (double)3);
			int ScanWidthWP = AbsOfStride - ExtraBPerLine;
			// Scan width without padding
			IntPtr BmpDataPtr = BitmapData.Scan0;

			int ByteCount = Convert.ToInt32((double)AbsOfStride * (double)Height);
			byte[] Colors = new byte[ByteCount];
			Marshal.Copy(BmpDataPtr, Colors, 0, ByteCount);

			int CurRowSum = 0;
			long CurRowSum2 = 0;
			int k = Width;
			// image2D(0,1) = image1D(width)   (Skip first row)
			int i = Convert.ToInt32((double)3 * (double)Width) + ExtraBPerLine;
			//8bppimage2D(0,1) = 8bppimage1D(3 * Width)
			int PosControl = 0;
			// We will start right after first extra bytes
			while (i < ByteCount)
			{
				// For conversation from rgb to gray.
				float GrayVal = Colors[i];
				// Blue
				GrayVal = (float)0.114f * (float)GrayVal;
				i = i + 1;

				GrayVal = GrayVal + (float)0.587f * (float)Colors[i];
				// Green
				i = i + 1;

				GrayVal = GrayVal + (float)0.299f * (float)Colors[i];
				// Red
				i = i + 1;

				PosControl = PosControl + 3;
				// If current position is equal to ScanWidthWP now, skip bytes inserted for padding the scan line and zero PosControl for future controls.
				if (PosControl == ScanWidthWP)
				{
					PosControl = 0;
					i = i + ExtraBPerLine;
				}

				int CurRow = Convert.ToInt32(Math.Floor((double)k / (double)Width));
				int CurCol = k % Width;
				if (CurCol == 0)
				{
					CurRowSum = 0;
					CurRowSum2 = 0;
				}
				CurRowSum = Convert.ToInt32(CurRowSum + GrayVal);
				CurRowSum2 = Convert.ToInt32(CurRowSum2 + GrayVal * GrayVal);
				CumSum[CurCol, CurRow] = CumSum[CurCol, CurRow - 1] + CurRowSum;
				CumSum2[CurCol, CurRow] = CumSum2[CurCol, CurRow - 1] + CurRowSum2;

				k = k + 1;
			}
		}

		// For 32 bits per pixel images (for both premultiplied and not premultiplied by alpha values.) Alpha channel is ignored.
		private void CalculateCumSums32bpp(ref int[,] CumSum, ref long[,] CumSum2, ref BitmapData BitmapData, ref int Width, ref int Height)
		{
			int ScanWidthWP = Width * 4;
			// 32bpp formatted bitmaps never contains padding bytes.
			IntPtr BmpDataPtr = BitmapData.Scan0;

			int ByteCount = ScanWidthWP * Height;
			byte[] Colors = new byte[ByteCount];
			Marshal.Copy(BmpDataPtr, Colors, 0, ByteCount);

			int CurRowSum = 0;
			long CurRowSum2 = 0;
			int k = Width;
			// image2D(0,1) = image1D(width)   (Skip first row)
			int i = ScanWidthWP;
			//8bppimage2D(0,1) = 8bppimage1D(3 * Width)
			while (i < ByteCount)
			{
				// For conversation from rgb to gray.
				float GrayVal = Colors[i];
				// Blue
				GrayVal = 0.114f * GrayVal;
				i = i + 1;

				GrayVal = GrayVal + 0.587f * (float)Colors[i];
				// Green
				i = i + 1;

				GrayVal = GrayVal + 0.299f * (float)Colors[i];
				// Red
				i = i + 2;
				// Skip alpha channel

				int CurRow = Convert.ToInt32(Math.Floor((double)k / (double)Width));
				int CurCol = k % Width;
				if (CurCol == 0)
				{
					CurRowSum = 0;
					CurRowSum2 = 0;
				}
				CurRowSum = Convert.ToInt32(CurRowSum + GrayVal);
				CurRowSum2 = Convert.ToInt32(CurRowSum2 + GrayVal * GrayVal);
				CumSum[CurCol, CurRow] = CumSum[CurCol, CurRow - 1] + CurRowSum;
				CumSum2[CurCol, CurRow] = CumSum2[CurCol, CurRow - 1] + CurRowSum2;

				k = k + 1;
			}
		}

		// Detects objects on given Bitmap. Only 32bppPArgb, 32bppArgb, 24bppRgb and 8bppIndexed formats are supported for now.
		public DResults? Detect(ref Bitmap Bitmap, DetectionParams Parameters)
		{
			int Width = Bitmap.Width;
			int Height = Bitmap.Height;

			int[,] CumSum = new int[Width, Height];
			// Cumulative sums of every pixel.
			long[,] CumSum2 = new long[Width, Height];
			// Squares of sums of every pixel. These will be used for standart deviation calculations.

			BitmapData BitmapData = Bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, Bitmap.PixelFormat);
			if (Bitmap.PixelFormat == PixelFormat.Format24bppRgb)
			{
				CalculateCumSums24bpp(ref CumSum, ref CumSum2, ref BitmapData, ref Width, ref Height);
			}
			else if (Bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
			{
				CalculateCumSums8bpp(ref CumSum, ref CumSum2, ref BitmapData, ref Width, ref Height);
				Parameters.Pen = null;
				// Can't draw anything on an 8 bit indexed image.
			}
			else if (Bitmap.PixelFormat == PixelFormat.Format32bppPArgb || Bitmap.PixelFormat == PixelFormat.Format32bppArgb)
			{
				CalculateCumSums32bpp(ref CumSum, ref CumSum2, ref BitmapData, ref Width, ref Height);
			}
			else
			{
				throw new Exception(Bitmap.PixelFormat.ToString() + " is not supported.");
				Bitmap.UnlockBits(BitmapData);
				return null;
			}
			Bitmap.UnlockBits(BitmapData);

			List<Rectangle> DetectedOLocs = new List<Rectangle>();
			// Passed regions will be stored here.
			int NOfObjects = 0;
			// Number of detected objects
			int SearchedSubRegionCount = 0;
			// Searched subregion count

			float Scaler = Parameters.FirstScale;
			// For all scales between first scale and max scale.
			while (Scaler < Parameters.MaxScale)
			{
				int WinWidth = Convert.ToInt32((float)HCascade.WindowSize.Width * Scaler);
				// Scaled searching window width
				int WinHeight = Convert.ToInt32((float)HCascade.WindowSize.Height * Scaler);
				// Scaled searching window height
				float InvArea = Convert.ToSingle((float)1 / ((float)WinWidth * (float)WinHeight));
				// Inverse of the area

				int StepSize = Convert.ToInt32((float)WinWidth * Parameters.SlidingRatio);
				// Current step size
				for (int i = 0; i <= Width - WinWidth - 1; i += StepSize)
				{
					for (int j = 0; j <= Height - WinHeight - 1; j += StepSize)
					{
						SearchedSubRegionCount = SearchedSubRegionCount + 1;

						// Integral image of current region:
						int IImg = CumSum[i + WinWidth, j + WinHeight] - CumSum[i, j + WinHeight] - CumSum[i + WinWidth, j] + CumSum[i, j];
						long IImg2 = CumSum2[i + WinWidth, j + WinHeight] - CumSum2[i, j + WinHeight] - CumSum2[i + WinWidth, j] + CumSum2[i, j];
						float Mean = (float)IImg * InvArea;
						float Variance = (float)IImg2 * InvArea - Mean * Mean;
						float Normalizer = 0;
						// Will normalize thresholds.
						if (Variance > 1)
						{
							Normalizer = Convert.ToSingle(Math.Sqrt(Variance));
							// Standart deviation
						}
						else
						{
							Normalizer = 1;
						}

						bool Passed = true;
						foreach (HaarCascade.Stage Stage in HCascade.Stages)
						{
							float StageVal = 0;
							foreach (HaarCascade.Tree Tree in Stage.Trees)
							{
								HaarCascade.Node CurNode = Tree.Nodes[0];
								while (true)
								{
									int RectSum = 0;
									foreach (HaarCascade.FeatureRect FeatureRect in CurNode.FeatureRects)
									{
										// Resize current feature rectangle to fit it in scaled searching window:
										int Rx1 = Convert.ToInt32(i + Math.Floor((float)FeatureRect.Rectangle.X * Scaler));
										int Ry1 = Convert.ToInt32(j + Math.Floor((float)FeatureRect.Rectangle.Y * Scaler));
										int Rx2 = Convert.ToInt32(Rx1 + Math.Floor((float)FeatureRect.Rectangle.Width * Scaler));
										int Ry2 = Convert.ToInt32(Ry1 + Math.Floor((float)FeatureRect.Rectangle.Height * Scaler));
										// Integral image of the region bordered by the current feature ractangle (sum of all pixels in it):
										RectSum = Convert.ToInt32(RectSum + (CumSum[Rx2, Ry2] - CumSum[Rx1, Ry2] - CumSum[Rx2, Ry1] + CumSum[Rx1, Ry1]) * FeatureRect.Weight);
									}

									float AvgRectSum = (float)RectSum * InvArea;
									if (AvgRectSum < CurNode.Threshold * Normalizer)
									{
										if (CurNode.HasLNode)
										{
											CurNode = Tree.Nodes[CurNode.LeftNode];
											// Go to the left node
											continue;
										}
										else
										{
											StageVal = StageVal + CurNode.LeftVal;
											break; // TODO: might not be correct. Was : Exit While
											// It is a leaf, exit.
										}
									}
									else
									{
										if (CurNode.HasRNode)
										{
											CurNode = Tree.Nodes[CurNode.RightNode];
											// Go to the right node
											continue;
										}
										else
										{
											StageVal = StageVal + CurNode.RightVal;
											break; // TODO: might not be correct. Was : Exit While
											// It is a leaf, exit.
										}
									}
								}
							}
							if (StageVal < Stage.Threshold)
							{
								Passed = false;
								break; // TODO: might not be correct. Was : Exit For
								// Don't waste time with trying to pass it from other stages.
							}
						}
						// If current region was passed from all stages
						if (Passed)
						{
							DetectedOLocs.Add(new Rectangle(i, j, WinWidth, WinHeight));
							NOfObjects += 1;
							// Are they enough? (note that, nested rectangles are not eliminated yet)
							if (NOfObjects == Parameters.MaxDetCount)
							{
								break; // TODO: might not be correct. Was : Exit While
							}
						}
					}
				}
				Scaler *= Parameters.ScaleMult;
			}

			DResults Results = default(DResults);
			if (DetectedOLocs.Count > 0)
			{
				Results = EliminateNestedRects(DetectedOLocs.ToArray(), NOfObjects, Parameters.MinNRectCount + 1, ref Parameters.SizeMultForNesRectCon);
				// If a pen was given, mark objects using given pen
				if (Parameters.Pen != null)
				{
					Graphics G = Graphics.FromImage(Bitmap);
					G.DrawRectangles(Parameters.Pen, Results.DetectedOLocs);
					G.Dispose();
				}
			}
			else
			{
				Results = new DResults(0, 0, null);
			}

			Results.SearchedSubRegionCount = SearchedSubRegionCount;
			return Results;
		}

		// Every detected object must be marked only with one rectangle. Others must be eliminated:
		private DResults EliminateNestedRects(Rectangle[] DetectedOLocs, int NOfObjects, int MinNRectCount, ref float SizeMultForNesRectCon)
		{
			int[] NestedRectsCount = new int[NOfObjects];
			Rectangle[] AvgRects = new Rectangle[NOfObjects];
			for (int i = 0; i <= NOfObjects - 1; i++)
			{
				Rectangle Current = DetectedOLocs[i];
				AvgRects[i] = Current;
				for (int j = 0; j <= NOfObjects - 1; j++)
				{
					// Check if these 2 rectangles are nested
					if (i != j && DetectedOLocs[j].Width > 0 && AreTheyNested(ref Current, ref DetectedOLocs[j], ref SizeMultForNesRectCon))
					{
						NestedRectsCount[i] += 1;
						AvgRects[i].X += DetectedOLocs[j].X;
						AvgRects[i].Y += DetectedOLocs[j].Y;
						AvgRects[i].Width += DetectedOLocs[j].Width;
						AvgRects[i].Height += DetectedOLocs[j].Height;
						DetectedOLocs[j].Width = 0;
						// Zero it to eliminate.
					}
				}
			}

			int k = 0;
			Rectangle[] NewRects = new Rectangle[NOfObjects];
			for (int i = 0; i <= NOfObjects - 1; i++)
			{
				// Rectangles that are not eliminated
				if (DetectedOLocs[i].Width > 0)
				{
					int NOfNRects = NestedRectsCount[i] + 1;
					//+1 is itself. It is required, becuse we will calculate average of them.
					if (NOfNRects >= MinNRectCount)
					{
						// Average rectangle:
						NewRects[k] = new Rectangle(Convert.ToInt32((double)AvgRects[i].X / (double)NOfNRects), Convert.ToInt32((double)AvgRects[i].Y / (double)NOfNRects), Convert.ToInt32((double)AvgRects[i].Width / (double)NOfNRects), Convert.ToInt32((double)AvgRects[i].Height / (double)NOfNRects));
					}
					k = k + 1;
				}
			}

			DResults Results = new DResults() { DetectedOLocs = new Rectangle[NewRects.Length] };
			// ERROR: Not supported in C#: ReDimStatement

			Array.Copy(NewRects, Results.DetectedOLocs, k);
			Results.NOfObjects = k;

			return Results;
		}

		private bool AreTheyNested(ref Rectangle Rectangle1, ref Rectangle Rectangle2, ref float SizeMultForNesRectCon)
		{
			// Maybe they are not fully nested, we must be tolerant:
			int MaxHorDist = Convert.ToInt32((double)SizeMultForNesRectCon * (double)Rectangle1.Width);
			int MaxVertDist = Convert.ToInt32((double)SizeMultForNesRectCon * (double)Rectangle1.Height);
			if ((FastAbs(Rectangle2.X - Rectangle1.X) < MaxHorDist && FastAbs(Rectangle2.Right - Rectangle1.Right) < MaxHorDist) && (FastAbs(Rectangle2.Y - Rectangle1.Y) < MaxVertDist && FastAbs(Rectangle2.Bottom - Rectangle1.Bottom) < MaxVertDist))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		// Math.Abs() makes type conversations before and after the operation. It is waste of time...
		private int FastAbs(int Int)
		{
			if (Int < 0)
			{
				return -Int;
			}
			else
			{
				return Int;
			}
		}
	}

	//=======================================================
	//Service provided by Telerik (www.telerik.com)
	//Conversion powered by NRefactory.
	//Twitter: @telerik, @toddanglin
	//Facebook: facebook.com/telerik
	//=======================================================
}