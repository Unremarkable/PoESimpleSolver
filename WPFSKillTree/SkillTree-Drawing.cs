using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace POESKillTree
{
    public partial class SkillTree
    {
        #region Members
        public DrawingVisual picSkillIconLayer;
        public DrawingVisual picSkillSurround;
        public DrawingVisual picLinks = new DrawingVisual();
        public DrawingVisual picActiveLinks;
        public DrawingVisual picPathOverlay;
        public DrawingVisual picBackground;
        public DrawingVisual picFaces;
		public DrawingVisual picHighlights;
		public DrawingVisual picSolveHalos;
		public DrawingVisual picSolvePaths;
		public DrawingVisual picSimplePaths;
        public DrawingVisual picSkillBaseSurround;
        public DrawingVisual SkillTreeVisual;

        public Dictionary<bool, KeyValuePair<Rect, ImageBrush>> StartBackgrounds = new Dictionary<bool, KeyValuePair<Rect, ImageBrush>>();
        public List<KeyValuePair<Size, ImageBrush>> NodeSurroundBrush = new List<KeyValuePair<Size, ImageBrush>>();
        public List<KeyValuePair<Rect, ImageBrush>> FacesBrush = new List<KeyValuePair<Rect, ImageBrush>>();

        public void CreateCombineVisual()
        {
            SkillTreeVisual = new DrawingVisual();
            SkillTreeVisual.Children.Add(picBackground);
            SkillTreeVisual.Children.Add(picLinks);
            SkillTreeVisual.Children.Add(picActiveLinks);
            SkillTreeVisual.Children.Add(picPathOverlay);
            SkillTreeVisual.Children.Add(picSkillIconLayer);
            SkillTreeVisual.Children.Add(picSkillBaseSurround);
            SkillTreeVisual.Children.Add(picSkillSurround);
            SkillTreeVisual.Children.Add(picFaces);
			SkillTreeVisual.Children.Add(picHighlights);
			SkillTreeVisual.Children.Add(picSolveHalos);
			SkillTreeVisual.Children.Add(picSolvePaths);
			SkillTreeVisual.Children.Add(picSimplePaths);
        }
        #endregion
        private void InitOtherDynamicLayers()
        {
            picActiveLinks = new DrawingVisual();
            picPathOverlay = new DrawingVisual();
			picHighlights = new DrawingVisual();
			picSolveHalos = new DrawingVisual();
			picSolvePaths = new DrawingVisual();
			picSimplePaths = new DrawingVisual();
        }

		public void DrawSolveHalo(HashSet<SkillNode> nodes)
		{
			Pen hpen = new Pen(Brushes.Crimson, 20);
			using (DrawingContext dc = picSolveHalos.RenderOpen()) {
				foreach (SkillNode node in nodes) {
					dc.DrawEllipse(null, hpen, node.Position, 80, 80);
				}
			}
		}

		static Color[] Colors = new Color[] {
			Color.FromRgb(0x00, 0xFF, 0x00), Color.FromRgb(0x00, 0x00, 0xFF), Color.FromRgb(0xFF, 0xFF, 0x00), Color.FromRgb(0xFF, 0x00, 0xFF), Color.FromRgb(0x00, 0xFF, 0xFF), Color.FromRgb(0x00, 0x00, 0x00), 
			Color.FromRgb(0x00, 0x80, 0x00), Color.FromRgb(0x00, 0x00, 0x80), Color.FromRgb(0x80, 0x80, 0x00), Color.FromRgb(0x80, 0x00, 0x80), Color.FromRgb(0x00, 0x80, 0x80), Color.FromRgb(0x80, 0x80, 0x80), 
			Color.FromRgb(0x00, 0xC0, 0x00), Color.FromRgb(0x00, 0x00, 0xC0), Color.FromRgb(0xC0, 0xC0, 0x00), Color.FromRgb(0xC0, 0x00, 0xC0), Color.FromRgb(0x00, 0xC0, 0xC0), Color.FromRgb(0xC0, 0xC0, 0xC0), 
			Color.FromRgb(0x00, 0x40, 0x00), Color.FromRgb(0x00, 0x00, 0x40), Color.FromRgb(0x40, 0x40, 0x00), Color.FromRgb(0x40, 0x00, 0x40), Color.FromRgb(0x00, 0x40, 0x40), Color.FromRgb(0x40, 0x40, 0x40), 
			Color.FromRgb(0x00, 0x20, 0x00), Color.FromRgb(0x00, 0x00, 0x20), Color.FromRgb(0x20, 0x20, 0x00), Color.FromRgb(0x20, 0x00, 0x20), Color.FromRgb(0x00, 0x20, 0x20), Color.FromRgb(0x20, 0x20, 0x20), 
			Color.FromRgb(0x00, 0x60, 0x00), Color.FromRgb(0x00, 0x00, 0x60), Color.FromRgb(0x60, 0x60, 0x00), Color.FromRgb(0x60, 0x00, 0x60), Color.FromRgb(0x00, 0x60, 0x60), Color.FromRgb(0x60, 0x60, 0x60), 
			Color.FromRgb(0x00, 0xA0, 0x00), Color.FromRgb(0x00, 0x00, 0xA0), Color.FromRgb(0xA0, 0xA0, 0x00), Color.FromRgb(0xA0, 0x00, 0xA0), Color.FromRgb(0x00, 0xA0, 0xA0), Color.FromRgb(0xA0, 0xA0, 0xA0), 
			Color.FromRgb(0x00, 0xE0, 0x00), Color.FromRgb(0x00, 0x00, 0xE0), Color.FromRgb(0xE0, 0xE0, 0x00), Color.FromRgb(0xE0, 0x00, 0xE0), Color.FromRgb(0x00, 0xE0, 0xE0), Color.FromRgb(0xE0, 0xE0, 0xE0), 
		};

		public void DrawSolvePath(List<List<Tuple<ushort, ushort>>> edgesList)
		{
			Dictionary<Tuple<ushort, ushort>, List<int>> edgeMap = new Dictionary<Tuple<ushort, ushort>, List<int>>(new EdgeComparer());

			Brush[] brushes = new Brush[edgesList.Count];

			for (int i = 0; i < edgesList.Count; ++i) {
				brushes[i] = new SolidColorBrush(Colors[i]);
			}

			using (DrawingContext dc = picSolvePaths.RenderOpen()) {
				for (int i = 0; i < edgesList.Count; ++i) {
					foreach (var edge in edgesList[i]) {
						if (!edgeMap.ContainsKey(edge))
							edgeMap[edge] = new List<int>();
						edgeMap[edge].Add(i);
					}
				}

				foreach (var edge in edgeMap) {
					for (int j = 0; j < edge.Value.Count; ++j) {
						int i = edge.Value[j];
						Pen pen = new Pen(brushes[i], 15f);
						pen.DashStyle = new DashStyle(new DoubleCollection() { 1, edge.Value.Count - 1 }, j);
						pen.DashCap = PenLineCap.Flat;
					//	dc.DrawLine(pen, Skillnodes[edge.Key.Item1].Position, Skillnodes[edge.Key.Item2].Position);

						SkillNode goal = Skillnodes[edge.Key.Item2];
						HashSet<SkillNode> from = new HashSet<SkillNode>();
						from.Add(Skillnodes[edge.Key.Item1]);
						while (from.Count > 0) {
							HashSet<SkillNode> newOrigin = new HashSet<SkillNode>();
							foreach (SkillNode next in from) {
								foreach (SkillNode to in ShortestPathTable[next][goal].Item1) {
									DrawConnection(dc, pen, next, to);
									if (to != goal)
										newOrigin.Add(to);
								}
							}
							from = newOrigin;
						}
					}
				}
			/*	for (int i = 0; i < edgesList.Count; ++i) {
					System.Windows.Media.Brush brush = new SolidColorBrush(Color.FromArgb(255, 0, 255, (byte)((255 * (i + 1)) / edgesList.Count)));
					System.Windows.Media.Pen hpen = new System.Windows.Media.Pen(brush, 15f);
					hpen.DashCap = PenLineCap.Flat;
					hpen.DashStyle = new DashStyle(new DoubleCollection() { 1, edgesList.Count - 1 }, hpen.Thickness * (i) / edgesList.Count);
					foreach (var tuple in edgesList[i]) {
						dc.DrawLine(hpen, Skillnodes[tuple.Item1].Position, Skillnodes[tuple.Item2].Position);
					}
				}*/
			}
		}

		public void DrawSimpleGraph(Dictionary<SkillNode, Dictionary<SkillNode, int>> paths)
		{
			System.Windows.Media.Brush brush = new SolidColorBrush(Color.FromArgb(32, 255, 0, 0));
			System.Windows.Media.Pen hpen = new System.Windows.Media.Pen(brush, 15f);


			using (DrawingContext dc = picSimplePaths.RenderOpen()) {
				foreach (KeyValuePair<SkillNode, Dictionary<SkillNode, int>> pair in paths) {
					foreach (var ep in pair.Value) {
						dc.DrawLine(hpen, pair.Key.Position, ep.Key.Position);
						dc.DrawText(new FormattedText(ep.Value.ToString(), CultureInfo.GetCultureInfo("en-us"),
		  FlowDirection.LeftToRight,
		  new Typeface("Verdana"),
		  64, System.Windows.Media.Brushes.Red), (pair.Key.Position + ep.Key.Position) / 2);
					}
				}
			}
		}

        private void DrawBackgroundLayer()
        {
            picBackground = new DrawingVisual();
            using (DrawingContext dc = picBackground.RenderOpen())
            {
                BitmapImage[] iscr = new BitmapImage[]
                                         {
                                             assets["PSGroupBackground1"].PImage, assets["PSGroupBackground2"].PImage,
                                             assets["PSGroupBackground3"].PImage
                                         };
                Brush[] OrbitBrush = new Brush[3];
                OrbitBrush[0] = new ImageBrush(assets["PSGroupBackground1"].PImage);
                OrbitBrush[1] = new ImageBrush(assets["PSGroupBackground2"].PImage);
                OrbitBrush[2] = new ImageBrush(assets["PSGroupBackground3"].PImage);
                (OrbitBrush[2] as ImageBrush).TileMode = TileMode.FlipXY;
                (OrbitBrush[2] as ImageBrush).Viewport = new Rect(0, 0, 1, .5f);

                ImageBrush BackgroundBrush = new ImageBrush(assets["Background1"].PImage);
                BackgroundBrush.TileMode = TileMode.FlipXY;
                dc.DrawRectangle(BackgroundBrush, null, TRect);
                foreach (var ngp in NodeGroups)
                {
                    if (ngp.OcpOrb == null)
                        ngp.OcpOrb = new Dictionary<int, bool>();
                    var cgrp = ngp.OcpOrb.Keys.Where(ng => ng <= 3);
                    if (cgrp.Count()==0)continue;
                    int maxr = cgrp.Max( ng => ng );
                    if (maxr == 0) continue;
                    maxr = maxr > 3 ? 2 : maxr - 1;
                    int maxfac = maxr == 2 ? 2 : 1;
                    dc.DrawRectangle(OrbitBrush[maxr], null,
                                     new Rect(
                                         ngp.Position - new Vector2D(iscr[maxr].PixelWidth*1.5, iscr[maxr].PixelHeight*1.5 * maxfac),
                                         new Size(iscr[maxr].PixelWidth * 3, iscr[maxr].PixelHeight * 3 * maxfac)));
                  
                }
            }
        }
        private void InitFaceBrushesAndLayer()
        {
            foreach (string faceName in FaceNames)
            {
                var bi = new BitmapImage(new Uri("Data\\Assets\\" + faceName + ".png", UriKind.Relative));
                FacesBrush.Add(new KeyValuePair<Rect, ImageBrush>(new Rect(0, 0, bi.PixelWidth, bi.PixelHeight),
                                                                  new ImageBrush(bi)));
            }

            var bi2 = new BitmapImage(new Uri("Data\\Assets\\PSStartNodeBackgroundInactive.png", UriKind.Relative));
            StartBackgrounds.Add(false,
                                 (new KeyValuePair<Rect, ImageBrush>(new Rect(0, 0, bi2.PixelWidth, bi2.PixelHeight),
                                                                     new ImageBrush(bi2))));
            picFaces = new DrawingVisual();

        }

        private void DrawLinkBackgroundLayer()
        {
            Pen pen2 = new Pen(Brushes.DarkSlateGray, 32);
            HashSet<ushort> visited = new HashSet<ushort>(SkilledNodes);
            using (DrawingContext dc = picLinks.RenderOpen())
            {
                foreach (var skillNode in Skillnodes)
                {
                    if (!visited.Contains(skillNode.Key) && !skillNode.Value.isExternal)
                    {
                        foreach (var neighbor in skillNode.Value.Neighbor)
                        {
                            if (!neighbor.isExternal)
                            {
                                DrawConnection(dc, pen2, skillNode.Value, neighbor);
                            }
                        }
                        visited.Add(skillNode.Key);
                    }
                }
            }
        }

        private void DrawSkillIconLayer()
        {
            Pen pen = new Pen(Brushes.Black, 5);
            Pen pen3 = new Pen(Brushes.Green, 10);
            picSkillIconLayer = new DrawingVisual();

            Geometry g = new RectangleGeometry(TRect);
            using (DrawingContext dc = picSkillIconLayer.RenderOpen())
            {
                dc.DrawGeometry(null, pen, g);
                foreach (var skillNode in Skillnodes)
                {
                    Size isize;
                    ImageBrush br = new ImageBrush();
                    int icontype = skillNode.Value.not ? 1 : skillNode.Value.ks ? 2 : 0;
                    Rect r = iconActiveSkills.SkillPositions[skillNode.Value.icon].Key;
                    BitmapImage bi = iconActiveSkills.Images[iconActiveSkills.SkillPositions[skillNode.Value.icon].Value];
                    br.Stretch = Stretch.Uniform;
                    br.ImageSource = bi;

                    br.ViewboxUnits = BrushMappingMode.RelativeToBoundingBox;
                    br.Viewbox = new Rect(r.X / bi.PixelWidth, r.Y / bi.PixelHeight, r.Width / bi.PixelWidth, r.Height / bi.PixelHeight);
                    Vector2D pos = (skillNode.Value.Position);
                    dc.DrawEllipse(br, null, pos, r.Width, r.Height);
                }
            }

        }
        private void InitNodeSurround()
        {
            picSkillSurround = new DrawingVisual();
            picSkillBaseSurround = new DrawingVisual();
            Size sizeNot;
            ImageBrush brNot = new ImageBrush();
            brNot.Stretch = Stretch.Uniform;
            BitmapImage PImageNot = assets[nodeBackgrounds["notable"]].PImage;
            brNot.ImageSource = PImageNot;
            sizeNot = new Size(PImageNot.PixelWidth, PImageNot.PixelHeight);


            Size sizeKs;
            ImageBrush brKS = new ImageBrush();
            brKS.Stretch = Stretch.Uniform;
            BitmapImage PImageKr = assets[nodeBackgrounds["keystone"]].PImage;
            brKS.ImageSource = PImageKr;
            sizeKs = new Size(PImageKr.PixelWidth, PImageKr.PixelHeight);

            Size sizeNotH;
            ImageBrush brNotH = new ImageBrush();
            brNotH.Stretch = Stretch.Uniform;
            BitmapImage PImageNotH = assets[nodeBackgroundsActive["notable"]].PImage;
            brNotH.ImageSource = PImageNotH;
            sizeNotH = new Size(PImageNotH.PixelWidth, PImageNotH.PixelHeight);


            Size sizeKsH;
            ImageBrush brKSH = new ImageBrush();
            brKSH.Stretch = Stretch.Uniform;
            BitmapImage PImageKrH = assets[nodeBackgroundsActive["keystone"]].PImage;
            brKSH.ImageSource = PImageKrH;
            sizeKsH = new Size(PImageKrH.PixelWidth, PImageKrH.PixelHeight);

            Size isizeNorm;
            ImageBrush brNorm = new ImageBrush();
            brNorm.Stretch = Stretch.Uniform;
            BitmapImage PImageNorm = assets[nodeBackgrounds["normal"]].PImage;
            brNorm.ImageSource = PImageNorm;
            isizeNorm = new Size(PImageNorm.PixelWidth, PImageNorm.PixelHeight);

            Size isizeNormA;
            ImageBrush brNormA = new ImageBrush();
            brNormA.Stretch = Stretch.Uniform;
            BitmapImage PImageNormA = assets[nodeBackgroundsActive["normal"]].PImage;
            brNormA.ImageSource = PImageNormA;
            isizeNormA = new Size(PImageNormA.PixelWidth, PImageNormA.PixelHeight);

            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(isizeNorm, brNorm));
            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(isizeNormA, brNormA));
            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(sizeKs, brKS));
            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(sizeNot, brNot));
            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(sizeKsH, brKSH));
            NodeSurroundBrush.Add(new KeyValuePair<Size, ImageBrush>(sizeNotH, brNotH));
        }
        private void DrawNodeBaseSurround()
        {
            using (DrawingContext dc = picSkillBaseSurround.RenderOpen())
            {

                foreach (var skillNode in Skillnodes.Keys)
                {
                    Vector2D pos = (Skillnodes[skillNode].Position);

                    if (Skillnodes[skillNode].not)
                    {
                        dc.DrawRectangle(NodeSurroundBrush[3].Value, null,
                                         new Rect((int)pos.X - NodeSurroundBrush[3].Key.Width,
                                                  (int)pos.Y - NodeSurroundBrush[3].Key.Height,
                                                  NodeSurroundBrush[3].Key.Width * 2,
                                                  NodeSurroundBrush[3].Key.Height * 2));
                    }
                    else if (Skillnodes[skillNode].ks)
                    {
                        dc.DrawRectangle(NodeSurroundBrush[2].Value, null,
                                         new Rect((int)pos.X - NodeSurroundBrush[2].Key.Width,
                                                  (int)pos.Y - NodeSurroundBrush[2].Key.Height,
                                                  NodeSurroundBrush[2].Key.Width * 2,
                                                  NodeSurroundBrush[2].Key.Height * 2));
                    }
                    else
                        dc.DrawRectangle(NodeSurroundBrush[0].Value, null,
                                        new Rect((int)pos.X - NodeSurroundBrush[0].Key.Width,
                                                 (int)pos.Y - NodeSurroundBrush[0].Key.Height,
                                                 NodeSurroundBrush[0].Key.Width * 2,
                                                 NodeSurroundBrush[0].Key.Height * 2));
                }
            }
        }
        private void DrawNodeSurround()
        {
            using (DrawingContext dc = picSkillSurround.RenderOpen())
            {

                foreach (var skillNode in SkilledNodes)
                {
                    Vector2D pos = (Skillnodes[skillNode].Position);

                    if (Skillnodes[skillNode].not)
                    {
                        dc.DrawRectangle(NodeSurroundBrush[5].Value, null,
                                         new Rect((int)pos.X - NodeSurroundBrush[5].Key.Width,
                                                  (int)pos.Y - NodeSurroundBrush[5].Key.Height,
                                                  NodeSurroundBrush[5].Key.Width * 2,
                                                  NodeSurroundBrush[5].Key.Height * 2));
                    }
                    else if (Skillnodes[skillNode].ks)
                    {
                        dc.DrawRectangle(NodeSurroundBrush[4].Value, null,
                                         new Rect((int)pos.X - NodeSurroundBrush[4].Key.Width,
                                                  (int)pos.Y - NodeSurroundBrush[4].Key.Height,
                                                  NodeSurroundBrush[4].Key.Width * 2,
                                                  NodeSurroundBrush[4].Key.Height * 2));
                    }
                    else
                        dc.DrawRectangle(NodeSurroundBrush[1].Value, null,
                                        new Rect((int)pos.X - NodeSurroundBrush[1].Key.Width,
                                                 (int)pos.Y - NodeSurroundBrush[1].Key.Height,
                                                 NodeSurroundBrush[1].Key.Width * 2,
                                                 NodeSurroundBrush[1].Key.Height * 2));

                }
            }
        }
        public void DrawHighlights(List<SkillNode> nodes)
        {
            Pen hpen = new Pen(Brushes.Aqua, 20);
            using (DrawingContext dc = picHighlights.RenderOpen())
            {
                foreach (SkillNode node in nodes)
                {
                    dc.DrawEllipse(null, hpen, node.Position, 80, 80);
                }
            }
        }
        private void DrawConnection(DrawingContext dc, Pen pen2, SkillNode n1, SkillNode n2)
        {
            if (n1.NodeGroup == n2.NodeGroup && n1.orbit == n2.orbit)
            {
                if (n1.Arc - n2.Arc > 0 && n1.Arc - n2.Arc <= Math.PI ||
                    n1.Arc - n2.Arc < -Math.PI)
                {
                    dc.DrawArc(null, pen2, n1.Position, n2.Position,
                               new Size(SkillTree.SkillNode.orbitRadii[n1.orbit],
                                        SkillTree.SkillNode.orbitRadii[n1.orbit]));
                }
                else
                {
                    dc.DrawArc(null, pen2, n2.Position, n1.Position,
                               new Size(SkillTree.SkillNode.orbitRadii[n1.orbit],
                                        SkillTree.SkillNode.orbitRadii[n1.orbit]));
                }
            }
            else
            {
                dc.DrawLine(pen2, n1.Position, n2.Position);
            }
        }
        public void DrawFaces()
        {
            using (DrawingContext dc = picFaces.RenderOpen())
            {
                for (int i = 0; i < CharName.Count; i++)
                {
                    var s = CharName[i];
                    var pos = Skillnodes.First(nd => nd.Value.name.ToUpper() == s.ToUpper()).Value.Position;
                    dc.DrawRectangle(StartBackgrounds[false].Value, null, new Rect(pos - new Vector2D(StartBackgrounds[false].Key.Width, StartBackgrounds[false].Key.Height), pos + new Vector2D(StartBackgrounds[false].Key.Width, StartBackgrounds[false].Key.Height)));
                    if (chartype==i)
                    {
                        dc.DrawRectangle(FacesBrush[i].Value, null, new Rect(pos - new Vector2D(FacesBrush[i].Key.Width, FacesBrush[i].Key.Height), pos + new Vector2D(FacesBrush[i].Key.Width, FacesBrush[i].Key.Height)));
                        
                    }
                }
            }
        }
        public void DrawPath(List<ushort> path)
        {
            Pen pen2 = new Pen(Brushes.LawnGreen, 15f);
            pen2.DashStyle = new DashStyle(new DoubleCollection() { 2 }, 2);

            using (DrawingContext dc = picPathOverlay.RenderOpen())
            {
                for (int i = -1; i < path.Count - 1; i++)
                {
                    SkillNode n1 = i == -1 ? Skillnodes[path[i + 1]].Neighbor.First(sn => SkilledNodes.Contains(sn.id)) : Skillnodes[path[i]];
                    SkillNode n2 = Skillnodes[path[i + 1]];

                    DrawConnection(dc, pen2, n1, n2);
                }
            }
        }
        public void DrawRefundPreview(HashSet<ushort> nodes)
        {
            Pen pen2 = new Pen(Brushes.Red, 15f);
            pen2.DashStyle = new DashStyle(new DoubleCollection() { 2 }, 2);

            using (DrawingContext dc = picPathOverlay.RenderOpen())
            {
                foreach (ushort node in nodes)
                {
                    foreach (SkillNode n2 in Skillnodes[node].Neighbor)
                    {
                        if (SkilledNodes.Contains(n2.id) && (node < n2.id || !(nodes.Contains(n2.id))))
                            DrawConnection(dc, pen2, Skillnodes[node], n2);
                    }
                }
            }

        }
        public void ClearPath()
        {
            picPathOverlay.RenderOpen().Close();
        }
    }
}
