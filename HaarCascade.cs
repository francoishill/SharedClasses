using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;

namespace SharedClasses
{
	internal class HaarCascade
	{

		//--------------------------------------------------------------------------
		// HaarCascadeClassifier > HaarCascade.vb
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


		// Feature rectangle
		public struct FeatureRect
		{
			public Rectangle Rectangle;
			public float Weight;
		}

		// Binary tree nodes
		public struct Node
		{
			// Feature rectangles
			public List<FeatureRect> FeatureRects;
			// Threshold for determining what to select (left value/right value) or where to go on binary tree (left or right)
			public float Threshold;
			// Left value
			public float LeftVal;
			// Right value
			public float RightVal;
			// Does this node have a left node? (Checking a boolean takes less time then to control if left node is null or not.)
			public bool HasLNode;
			// Left node. If current node doesn't have a left node, this will be null.
			public int LeftNode;
			// Does this node have a right node?
			public bool HasRNode;
			// Right node. If current node doesn't have a right node, this will be null.
			public int RightNode;
		}

		// Will be used as a binary tree
		public struct Tree
		{
			// Each tree can have max 3 nodes. First one is the current and others are nodes of the current.
			public List<Node> Nodes;
		}

		// Stages
		public struct Stage
		{
			// Trees in the stage.
			public List<Tree> Trees;
			// Threshold of the stage.
			public float Threshold;
		}

		// Stages of the cascade
		public List<Stage> Stages;
		// Original (unscaled) size of searching window
		public Size WindowSize;

		// Loads cascade from xml file at given path and creates a HaarCascade object using its content
		public HaarCascade(string OpenCVXmlStorageFile)
		{
			XmlDocument XMLDoc = new XmlDocument();
			XMLDoc.Load(OpenCVXmlStorageFile);
			Load(XMLDoc);
		}

		// If you embed the xml file, you can create an XmlDocument using embedded file and then use this constructor to create new HaarCascade.
		public HaarCascade(XmlDocument XmlDoc)
		{
			Load(XmlDoc);
		}

		// Parses given xml document and loads parsed data
		private void Load(XmlDocument XmlDoc)
		{
			foreach (XmlNode RootNode in XmlDoc.ChildNodes)
			{
				if (RootNode.NodeType == XmlNodeType.Comment)
					continue;

				foreach (XmlNode CascadeNode in RootNode)
				{
					// All haar cascades start with this expression: <haarcascade_frontalface_alt type_id="opencv-haar-classifier">
					if (CascadeNode.NodeType == XmlNodeType.Comment || CascadeNode.Attributes["type_id"] == null || !CascadeNode.Attributes["type_id"].Value.Equals("opencv-haar-classifier"))
						continue;

					Stages = new List<Stage>();

					foreach (XmlNode CascadeChild in CascadeNode)
					{
						if (CascadeChild.NodeType == XmlNodeType.Comment)
							continue;

						if (CascadeChild.Name.Equals("size"))
						{
							WindowSize = HaarCascadeParser.ParseSize(CascadeChild.InnerText);
						}
						else if (CascadeChild.Name.Equals("stages"))
						{
							foreach (XmlNode StageNode in CascadeChild)
							{
								if (StageNode.NodeType == XmlNodeType.Comment)
									continue;

								Stage NewStage = new Stage();
								NewStage.Trees = new List<Tree>();
								foreach (XmlNode StageChild in StageNode)
								{
									if (StageChild.NodeType == XmlNodeType.Comment)
										continue;

									if (StageChild.Name.Equals("stage_threshold"))
									{
										NewStage.Threshold = HaarCascadeParser.ParseSingle(StageChild.InnerText);
									}
									else if (StageChild.Name.Equals("trees"))
									{
										foreach (XmlNode Tree in StageChild)
										{
											if (Tree.NodeType == XmlNodeType.Comment)
												continue;

											Tree NewTree = new Tree();
											NewTree.Nodes = new List<Node>();

											foreach (XmlNode TreeNode in Tree)
											{
												if (TreeNode.NodeType == XmlNodeType.Comment)
													continue;

												Node NewNode = new Node();
												NewNode.FeatureRects = new List<FeatureRect>();

												foreach (XmlNode TreeNodeChild in TreeNode)
												{
													if (TreeNodeChild.NodeType == XmlNodeType.Comment)
														continue;

													if (TreeNodeChild.Name.Equals("feature"))
													{
														foreach (XmlNode TNCChild in TreeNodeChild)
														{
															if (TNCChild.NodeType == XmlNodeType.Comment)
																continue;

															if (TNCChild.Name.Equals("rects"))
															{
																foreach (XmlNode Rect in TNCChild)
																{
																	if (Rect.NodeType == XmlNodeType.Comment)
																		continue;

																	NewNode.FeatureRects.Add(HaarCascadeParser.ParseFeatureRect(Rect.InnerText));
																}
															}
															else if (TNCChild.Name.Equals("tilted"))
															{
																if (HaarCascadeParser.ParseInt(TNCChild.InnerText) == 1)
																{
																	// Not supported for now. Will be implemented in future releases.
																	throw new Exception("Tilted features are not supported yet!");
																	return;
																}
															}
														}
													}
													else if (TreeNodeChild.Name.Equals("threshold"))
													{
														NewNode.Threshold = HaarCascadeParser.ParseSingle(TreeNodeChild.InnerText);
													}
													else if (TreeNodeChild.Name.Equals("left_val"))
													{
														NewNode.LeftVal = HaarCascadeParser.ParseSingle(TreeNodeChild.InnerText);
														NewNode.HasLNode = false;
													}
													else if (TreeNodeChild.Name.Equals("right_val"))
													{
														NewNode.RightVal = HaarCascadeParser.ParseSingle(TreeNodeChild.InnerText);
														NewNode.HasRNode = false;
													}
													else if (TreeNodeChild.Name.Equals("left_node"))
													{
														NewNode.LeftNode = HaarCascadeParser.ParseInt(TreeNodeChild.InnerText);
														NewNode.HasLNode = true;
													}
													else if (TreeNodeChild.Name.Equals("right_node"))
													{
														NewNode.RightNode = HaarCascadeParser.ParseInt(TreeNodeChild.InnerText);
														NewNode.HasRNode = true;
													}
												}
												NewTree.Nodes.Add(NewNode);
											}

											NewStage.Trees.Add(NewTree);
										}
									}
								}
								Stages.Add(NewStage);
							}
						}
					}

					return;
				}
			}

			throw new Exception("Given XML document does not contain a haar cascade in supported format.");
		}

	}

	//=======================================================
	//Service provided by Telerik (www.telerik.com)
	//Conversion powered by NRefactory.
	//Twitter: @telerik, @toddanglin
	//Facebook: facebook.com/telerik
	//=======================================================
}