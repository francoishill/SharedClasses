using System;
using System.Drawing;

namespace SharedClasses
{
	internal class HaarCascadeParser
	{

		//--------------------------------------------------------------------------
		// HaarCascadeClassifier > Parser.vb
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


		// Sample: "20 20"
		public static Size ParseSize(string StringVal)
		{
			string[] Slices = StringVal.Trim().Split(' ');
			return new Size(Convert.ToInt32(Slices[0].Trim()), Convert.ToInt32(Slices[1].Trim()));
		}

		// Sample: "0.0337941907346249"
		public static float ParseSingle(string StringVal)
		{
			return (float.Parse(StringVal.Trim()));//.Replace(".", ",")));
		}

		// Sample: "1"
		public static int ParseInt(string StringVal)
		{
			return int.Parse(StringVal.Trim());
		}

		// Sample: "3 7 14 4 -1."
		public static HaarCascade.FeatureRect ParseFeatureRect(string StringVal)
		{
			string[] Slices = StringVal.Trim().Split(' ');
			HaarCascade.FeatureRect FR = new HaarCascade.FeatureRect();
			FR.Rectangle = new Rectangle(Convert.ToInt32(Slices[0].Trim()), Convert.ToInt32(Slices[1].Trim()), Convert.ToInt32(Slices[2].Trim()), Convert.ToInt32(Slices[3].Trim()));

			string Weight = Slices[4];
			if (Weight.EndsWith("."))
			{
				Weight = Weight.Replace(".", "");
			}
			else
			{
				Weight = Weight.Replace(".", ",");
			}
			FR.Weight = Convert.ToInt32(Weight.Trim());
			return FR;
		}
	}

	//=======================================================
	//Service provided by Telerik (www.telerik.com)
	//Conversion powered by NRefactory.
	//Twitter: @telerik, @toddanglin
	//Facebook: facebook.com/telerik
	//=======================================================
}