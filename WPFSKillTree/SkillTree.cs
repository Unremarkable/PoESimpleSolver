﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Collections;
using POESKillTree.Utility;

namespace POESKillTree
{
    public partial class SkillTree
    {
        public delegate void startLoadingWindow();
        public delegate void closeLoadingWindow();
        public delegate void UpdateLoadingWindow(double current, double max);


        string TreeAddress = "http://www.pathofexile.com/passive-skill-tree/";
        public List<NodeGroup> NodeGroups = new List<NodeGroup>();
        public Dictionary<UInt16, SkillNode> Skillnodes = new Dictionary<UInt16, SkillNode>();
        public List<string> AttributeTypes = new List<string>();
        // public Bitmap iconActiveSkills;
        public SkillIcons iconInActiveSkills = new SkillIcons();
        public SkillIcons iconActiveSkills = new SkillIcons();
        public Dictionary<string, string> nodeBackgrounds = new Dictionary<string, string>() { { "normal", "PSSkillFrame" }, { "notable", "NotableFrameUnallocated" }, { "keystone", "KeystoneFrameUnallocated" } };
        public Dictionary<string, string> nodeBackgroundsActive = new Dictionary<string, string>() { { "normal", "PSSkillFrameActive" }, { "notable", "NotableFrameAllocated" }, { "keystone", "KeystoneFrameAllocated" } };
        public List<string> FaceNames = new List<string>() {"centerscion", "centermarauder", "centerranger", "centerwitch", "centerduelist", "centertemplar", "centershadow"  };
        public List<string> CharName = new List<string>() { "SEVEN","MARAUDER", "RANGER", "WITCH", "DUELIST", "TEMPLAR", "SIX" };
        public Dictionary<string, float>[] CharBaseAttributes = new Dictionary<string, float>[7];
        public Dictionary<string, float> BaseAttributes = new Dictionary<string, float>()
                                                              {
                                                                  {"+# to maximum Mana",36},
                                                                  {"+# to maximum Life",44},
                                                                  {"Evasion Rating: #",50},
                                                                  {"+# Maximum Endurance Charge",3},
                                                                  {"+# Maximum Frenzy Charge",3},
                                                                  {"+# Maximum Power Charge",3},
                                                                  {"#% Additional Elemental Resistance per Endurance Charge",4},
                                                                  {"#% Physical Damage Reduction per Endurance Charge",4},
                                                                  {"#% Attack Speed Increase per Frenzy Charge",5},
                                                                  {"#% Cast Speed Increase per Frenzy Charge",5},
                                                                  {"#% Critical Strike Chance Increase per Power Charge",50},
                                                              };
        public static float LifePerLevel = 8;
        public static float EvasPerLevel = 3;
        public static float ManaPerLevel = 4;
        public static float IntPerMana = 2;
        public static float IntPerES = 5; //%
        public static float StrPerLife = 2;
        public static float StrPerED = 5; //%
        public static float DexPerAcc = 0.5f;
        public static float DexPerEvas = 5; //%
        private List<SkillTree.SkillNode> highlightnodes;
        private int level = 1;
        private int chartype = 0;
        public HashSet<ushort> SkilledNodes = new HashSet<ushort>();
        public HashSet<ushort> AvailNodes = new HashSet<ushort>();
        Dictionary<string, Asset> assets = new Dictionary<string, Asset>();
        public Rect2D TRect = new Rect2D();
        public float scaleFactor = 1;
        public HashSet<int[]> Links = new HashSet<int[]>();
        public void Reset()
        {
            SkilledNodes.Clear();
            var node = Skillnodes.First(nd => nd.Value.name.ToUpper() == CharName[chartype]);
            SkilledNodes.Add(node.Value.id);
            UpdateAvailNodes();
        }
        static Action emptyDelegate = delegate
        {
        };

		HashSet<SkillNode> SolveSet = new HashSet<SkillNode>();
        /*
		public class EdgeComparer : IEqualityComparer<Edge>
		{
			public static EdgeComparer Instance = new EdgeComparer();

			public bool Equals(Edge x, Edge y)
			{
				return (x.Item1 == y.Item1 && x.Item2 == y.Item2)
					|| (x.Item1 == y.Item2 && x.Item2 == y.Item1);
			}

			public int GetHashCode(Edge obj)
			{
				if (obj.Item1 < obj.Item2)
					return (obj.Item1 << sizeof(ushort)) | obj.Item2;
				else
					return (obj.Item2 << sizeof(ushort)) | obj.Item1;
			}
		}*/

        public class Edge
        {
            public ushort left;
            public ushort right;
            public int id;

            public Edge(ushort left, ushort right)
            {
                this.left = left;
                this.right = right;
            }

            public override bool Equals(object obj)
            {
                return GetHashCode() == (obj as Edge).GetHashCode();
            }

            public override int GetHashCode()
            {
                return left << 16| right;
            }
        }

		public class TreePart
			: IEquatable<TreePart>
		{
			public TreePart(SkillNode start)
			{
				this.Nodes    = new HashSet<ushort>();
				this.Edges    = new List<Edge>();
				this.Size     = 1;

				this.Nodes.Add(start.id);
			}

			public TreePart(TreePart clone)
			{
				this.Nodes    = new HashSet<ushort>(clone.Nodes);
				this.Edges    = new List<Edge>();
                foreach (Edge edge in clone.Edges)
                {
                    addEdge(edge);
                }
				this.Size     = clone.Size;
			}

            public void merge(TreePart other)
            {
                Nodes.UnionWith(other.Nodes);
                foreach (Edge edge in other.Edges)
                {
                    addEdge(edge);
                }
                Size += other.Size - 1;
            }

            public void addEdge(Edge newEdge)
            {
              //  Console.WriteLine(" addEdge " + newEdge.id / sizeof(long));
                edgeBitField[newEdge.id / 64] |= 1 << (newEdge.id % 64);
                Edges.Add(newEdge);
            }

			public bool Equals(TreePart other)
			{
                if (this.Size != other.Size)
					return false;
                for (int i = 0; i < edgeBitField.Length; i++)
                {
                    if (this.edgeBitField[i] != other.edgeBitField[i])
                        return false;
                }
				return true;
			}

			public override bool Equals(object obj)
			{
				if (obj is TreePart)
					return Equals(obj as TreePart);
				return false;
			}

			public override int GetHashCode()
			{
                long hashCode = 0;
                for (int i = 0; i < edgeBitField.Length; i++)
                {
                    hashCode ^= edgeBitField[i];
                }
                return (int)hashCode ^ (int)(hashCode >> 32);
			}

			public int Size;
			public List<Edge> Edges;
			public HashSet<ushort> Nodes;
            public long[] edgeBitField = new long[10];
		}

		public class TreeGroup
			: IEquatable<TreeGroup>
		{
			public TreeGroup(TreePart a, TreePart[] b)
			{
				this.Parts = new TreePart[b.Length];
				this.Smallest = a;
                AddToEdgeBitField(a);

				this.Size = a.Size;
				int index = 0;
				for (int i = 0; i < b.Length; ++i) {
					this.Size += b[i].Size;

                    AddToEdgeBitField(b[i]);

					if (b[i].Size < Smallest.Size) {
						this.Parts[index++] = Smallest;
						this.Smallest = b[i];
					} else {
						this.Parts[index++] = b[i];
					}
				}
			}

			public TreeGroup(TreePart[] a)
			{
				this.Parts = new TreePart[a.Length - 1];
				this.Smallest = a[0];

				this.Size = a[0].Size;
				int index = 0;
				for (int i = 1; i < a.Length; ++i) {
					this.Size += a[i].Size;

                    AddToEdgeBitField(a[i]);

					if (a[i].Size < Smallest.Size) {
						this.Parts[index++] = Smallest;
						this.Smallest = a[i];
					} else {
						this.Parts[index++] = a[i];
					}
				}
			}

			public TreePart Containing(ushort node)
			{
				foreach (TreePart part in Parts)
					if (part.Nodes.Contains(node))
						return part;
				return null;
			}

			public override int GetHashCode()
			{
                long hashCode = 0;
                for (int i = 0; i < edgeBitField.Length; i++)
                {
                    hashCode ^= edgeBitField[i];
                }
                return (int)hashCode ^ (int)(hashCode >> 32);
			}

            private void AddToEdgeBitField(TreePart part)
            {
                for (int i = 0; i < edgeBitField.Length; i++)
                {
                    edgeBitField[i] |= part.edgeBitField[i];
                }
            }

			public bool Equals(TreeGroup other)
			{
                if (this.Size != other.Size)
                    return false;
                for (int i = 0; i < edgeBitField.Length; i++)
                {
                    if (this.edgeBitField[i] != other.edgeBitField[i])
                        return false;
                }

				return true;
			}

			public override bool Equals(object obj)
			{
				if (obj is TreeGroup)
					return Equals(obj as TreeGroup);
				return false;
			}

			public int Size;
			public TreePart[] Parts;
			public TreePart   Smallest;
            public long[] edgeBitField = new long[10];
		}

		public List<List<Edge>> SolveSimpleGraph(IEnumerable<SkillNode> solveSet, int maxSize)
		{
            int edgeIdCount = 0;
            Dictionary<Edge, int> allEdges = new Dictionary<Edge, int>();
			Dictionary<ushort, List<KeyValuePair<ushort, int>>> neighbors = new Dictionary<ushort, List<KeyValuePair<ushort, int>>>();
			HashSet<TreeGroup> treeHash = new HashSet<TreeGroup>(EqualityComparer<TreeGroup>.Default);

			// INITIALIZE
			foreach (var origin in SimpleGraph) {
				neighbors[origin.Key.id] = new List<KeyValuePair<ushort, int>>();
				foreach (var destination in origin.Value) {
					neighbors[origin.Key.id].Add(new KeyValuePair<ushort, int>(destination.Key.id, destination.Value));
				}
			}

            BucketQueue<TreeGroup> groups = new BucketQueue<TreeGroup>();

			List<TreePart> initialParts = new List<TreePart>();
			{
				foreach (SkillNode node in solveSet) {
					initialParts.Add(new TreePart(node));
				}
			}

			TreeGroup initialGroup = new TreeGroup(initialParts.ToArray());
			treeHash.Add(initialGroup);

			groups.Enqueue(initialGroup, initialGroup.Size );

			int generation = 0;

			List<List<Edge>> solutions = new List<List<Edge>>();

			// DO WORK.
			while (!groups.IsEmpty()) {
				generation++;
				TreeGroup group = groups.Dequeue();

				if (group.Size > maxSize)
					break;

				if (group.Parts.Length == 0) {
					maxSize = group.Size;
                    Console.Out.WriteLine(maxSize);
					solutions.Add(group.Smallest.Edges);
					continue;
				}

				TreePart smallest = group.Smallest;

				foreach (ushort node in smallest.Nodes) {
					foreach (var next in neighbors[node]) {
						if (smallest.Nodes.Contains(next.Key))
							continue;

						TreePart part = new TreePart(smallest);
						Edge edge = new Edge(node, next.Key);
                        if (allEdges.ContainsKey(edge))
                        {
                            edge.id = allEdges[edge];
                        }
                        else
                        {
                            edge.id = edgeIdCount++;
                            Console.WriteLine(edge.id);
                            allEdges[edge] = edge.id;
                        }
                        
						part.Nodes.Add(next.Key);
                        part.addEdge(edge);
						part.Size += next.Value;

						TreePart mergePart = group.Containing(next.Key);
						TreeGroup newGroup;
						if (mergePart != null) {
                            part.merge(mergePart);

							TreePart[] other = new TreePart[group.Parts.Length - 1];
							int index = 0;
							for (int i = 0; i < group.Parts.Length; ++i)
								if (group.Parts[i] != mergePart)
									other[index++] = group.Parts[i];
							newGroup = new TreeGroup(part, other);
						} else {
							newGroup = new TreeGroup(part, group.Parts);
						}

						if (newGroup.Size > maxSize)
							continue;

						if (!treeHash.Contains(newGroup)) {
							treeHash.Add(newGroup);
							groups.Enqueue(newGroup, newGroup.Size );
						}
					}
				}
			}
            
			return solutions;
		}

		/*
        private class SubTree : IComparable<SubTree>, IEquatable<SubTree>
        {
			private class Prioritizer : IPrioritizer<SubTree>
			{
				int IPrioritizer<SubTree>.PriorityOf(SubTree t)
				{
					return t.size;
				}
			}

			public static IPrioritizer<SubTree> Prioritizer = new Prioritizer();

      //    public HashSet<Edge> edges;

            HashSet<ushort> nodes;
            public int size;
            private int hashCode;

            public SubTree(HashSet<Edge> edges, int size)
            {
                this.edges = edges;
                this.size = size;
            }

			public bool Equals(SubTree other)
			{
				if (other.GetHashCode() != this.GetHashCode())
					return false;
				if (other.edges.Count != this.edges.Count)
					return false;
			//	foreach (Edge edge in edges) {
			//		if (!other.edges.Contains(edge))
			//			return false;
			//	}
				return true;
			}

            public override bool Equals(object other)
            {
				if (other is SubTree)
					return (this as IEquatable<SubTree>).Equals(other as SubTree);
				return false;
            }

            public int CompareTo(SubTree other)
			{
				if (this.Equals(other))
					return  0;
				if (this.size > other.size)
					return  1;
				if (this.size < other.size)
					return -1;
				if (this.GetHashCode() > other.GetHashCode())
					return  1;
				return -1;
            }

            public HashSet<ushort> getNodeIds() {
                if (nodes == null) {
                    nodes = new HashSet<ushort>(edges.Select(edge => edge.Item1));
                    nodes.UnionWith(edges.Select(edge => edge.Item2));
                }               
                return nodes;
            }

            public override int GetHashCode() {
                if (hashCode == 0)
                {
                    foreach (Edge edge in edges)
                    {
                        hashCode += edge.Item1;
                        hashCode += edge.Item2;
                    }
                }

                return hashCode;
            }
		}*/

		public void Solve()
		{
			DrawSolveHalo(SolveSet);

		//	if (SolveSet.Count > 1) {
				RecalculateExternal();
				Stopwatch simplifyTime = Stopwatch.StartNew();
				ConstructSimpleGraph();
				simplifyTime.Stop();
				Console.WriteLine("Simplify time: {0}", simplifyTime.Elapsed);

				DrawSimpleGraph(SimpleGraph);

				Stopwatch solutionTime = Stopwatch.StartNew();
				var solutions = SolveSimpleGraph(SolveSet, 120);
				solutionTime.Stop();
				Console.WriteLine("{0} solutions found!", solutions.Count);
				Console.WriteLine("Solve time: {0}", solutionTime.Elapsed);

				DrawSolvePath(solutions);
		//	} else {
		//		SimpleGraph.Clear();
		//		DrawSimpleGraph(SimpleGraph);
		//	}
			
		//	DrawLinkBackgroundLayer();
		}

		public void ToggleToSolve(SkillNode node)
		{
			if (SolveSet.Contains(node))
				SolveSet.Remove(node);
			else
				SolveSet.Add(node);

			Solve();
		}

		Dictionary<SkillNode, Dictionary<SkillNode, Tuple<HashSet<SkillNode>, int>>> ShortestPathTable = new Dictionary<SkillNode, Dictionary<SkillNode, Tuple<HashSet<SkillNode>, int>>>();

		public void CalculateAllShortestPaths()
		{
			foreach (var skillNode in Skillnodes.Values) {
				ShortestPathTable[skillNode] = new Dictionary<SkillNode, Tuple<HashSet<SkillNode>, int>>();
				ShortestPathTable[skillNode][skillNode] = new Tuple<HashSet<SkillNode>, int>(new HashSet<SkillNode>(), 0);
				foreach (var neighbor in skillNode.Neighbor) {
					ShortestPathTable[skillNode][neighbor] = new Tuple<HashSet<SkillNode>, int>(new HashSet<SkillNode>(), 1);
					ShortestPathTable[skillNode][neighbor].Item1.Add(neighbor);
				}
			}

			foreach (var originNode in Skillnodes.Values) {
				HashSet<SkillNode> visited = new HashSet<SkillNode>();
				HashSet<SkillNode> frontier = new HashSet<SkillNode>();
				visited.Add(originNode);
				frontier.UnionWith(originNode.Neighbor);
				while (frontier.Count > 0) {
					HashSet<SkillNode> newFrontier = new HashSet<SkillNode>();
					foreach (var frontierNode in frontier) {
						foreach (var neighbor in frontierNode.Neighbor) {
							if (!visited.Contains(neighbor) && !frontier.Contains(neighbor)) {
								newFrontier.Add(neighbor);
								if (!ShortestPathTable[originNode].ContainsKey(neighbor))
									ShortestPathTable[originNode][neighbor] = new Tuple<HashSet<SkillNode>, int>(new HashSet<SkillNode>(), ShortestPathTable[originNode][frontierNode].Item2 + 1);
								ShortestPathTable[originNode][neighbor].Item1.UnionWith(ShortestPathTable[originNode][frontierNode].Item1);
							}
						}
					}
					visited.UnionWith(frontier);
					frontier = newFrontier;
				}
			}
		}

		public void GetShortestPathNodes(SkillNode from, SkillNode to, HashSet<SkillNode> currentSet)
		{
			if (from == to) {
				currentSet.Add(to);
				return;
			}

			currentSet.Add(from);

			foreach (SkillNode pathStart in ShortestPathTable[from][to].Item1) {
				if (!currentSet.Contains(pathStart)) {
					GetShortestPathNodes(pathStart, to, currentSet);
				}
			}
		}

		public HashSet<SkillNode> GetShortestPathNodes(SkillNode from, SkillNode to)
		{
			HashSet<SkillNode> shortestPathNodes = new HashSet<SkillNode>();
			GetShortestPathNodes(from, to, shortestPathNodes);
			return shortestPathNodes;
		}

		public void MarkExternalRecursively(SkillNode node)
		{
			if (node.isInternal || node.isExternal) {
				return;
			}

			node.isExternal = true;
			foreach (SkillNode neighbor in node.Neighbor) {

				MarkExternalRecursively(neighbor);
			}
		}

		public void RecalculateExternal()
		{
			foreach (SkillNode node in Skillnodes.Values) {
				node.isExternal = false;
				node.isInternal = false;
			}
            int edgeId = 1;

            Dictionary<SkillNode, List<int>> edgeIds = new Dictionary<SkillNode, List<int>>();
            HashSet<SkillNode> remaining = new HashSet<SkillNode>(SolveSet);
			foreach (var skillA in SolveSet){
                remaining.Remove(skillA);
                foreach (var skillB in remaining) {
				    foreach (SkillNode node in GetShortestPathNodes(skillA, skillB)) {
					    node.isInternal = true;
                        if (!edgeIds.ContainsKey(node)) { edgeIds[node] = new List<int>(); }
                        edgeIds[node].Add(edgeId);
				    }
                    
                    edgeId++;
			    }
            }

			List<ushort> external = new List<ushort> { 40633, 34098, 9660, 12926, 31961, 44941, 59763, 18663, 22535, 31703, 22115, 24426, 14914, 54922 };
			foreach (ushort externalId in external) {
				MarkExternalRecursively(Skillnodes[externalId]);
			}
            /*
            List<SkillNode> undecided = new List<SkillNode>(Skillnodes.Values.Where(node => (!node.isExternal && !node.isInternal)));

            while (undecided.Count > 0) {
                SkillNode current = undecided.First();
                HashSet<SkillNode> visited = new HashSet<SkillNode>();
                HashSet<SkillNode> frontier = new HashSet<SkillNode>();
                frontier.Add(current);
                while (frontier.Count > 0) {
                    visited.UnionWith(frontier);
                    foreach (SkillNode node in Skillnodes) { 

                    }
                }
            }*/
		}

		private bool EVERY_MST_CONTAINS(SkillNode node, List<List<Edge>> msts)
		{
			for (var i = 0; i < msts.Count; ++i) {
				bool contains = false;
				for (var j = 0; j < msts[i].Count; ++j) {
					if (msts[i][j].left != node.id && msts[i][j].right != node.id)
						continue;
					contains = true;
					break;
				}
				if (!contains)
					return false;
			}
			return true;
		}

		private bool ANY_MST_OF_NEIGHBORS_INCLUDES(SkillNode node)
		{
			HashSet<SkillNode> neighbor_of_neighbor = new HashSet<SkillNode>();
			foreach (SkillNode neighbor in SimpleGraph[node].Keys)
				neighbor_of_neighbor.Union(SimpleGraph[neighbor].Keys);
			neighbor_of_neighbor.Remove(node);
			foreach (SkillNode neighbor in SimpleGraph[node].Keys)
				neighbor_of_neighbor.Remove(neighbor);

			List<List<SkillNode>> power = PowerSet(neighbor_of_neighbor);

			if (power.Count == neighbor_of_neighbor.Count)
				return true;

			foreach (List<SkillNode> set in power) {
				if (set.Count < 2)
					continue;
				int max = 0;
				for (int i = 0; i < set.Count; ++i)
					max += SimpleGraph[node][set[i]];
				List<List<Edge>> msts = SolveSimpleGraph(set, max);

				if (EVERY_MST_CONTAINS(node, msts))
					return true;
			}

			return false;
		}

		Dictionary<SkillNode, Dictionary<SkillNode, int>> SimpleGraph = new Dictionary<SkillNode, Dictionary<SkillNode, int>>();

		private List<List<T>> PowerSet<T>(IEnumerable<T> super)
		{
			List<List<T>> sets = new List<List<T>>();

			int i = 1;
			foreach (T x in super)
			foreach (T y in super) {
				int j = 0;
				List<T> s = new List<T>();
				foreach (T a in super) {
					if (((1 << j) & i) != 0)
						s.Add(a);
					j++;
				}
				sets.Add(s);
				i++;
			}

			return sets;
		}

		private bool Simplify()
		{
			foreach (SkillNode node in SimpleGraph.Keys) {
				if (SolveSet.Contains(node)/* || SkilledNodes.Contains(node.id)*/)
					continue;

				Dictionary<SkillNode, int> neighbors = SimpleGraph[node];

				if (neighbors.ContainsKey(node)) {
					neighbors.Remove(node);
					return true;
				}

				if (neighbors.Count == 0) {
					SimpleGraph.Remove(node);
					return true;
				}

				if (neighbors.Count == 1) {
					SimpleGraph[neighbors.Keys.First()].Remove(node);
					SimpleGraph.Remove(node);
					return true;
				}

				if (SimpleGraph[node].Count == 2) {
					SkillNode neighborA = neighbors.Keys.First();
					SkillNode neighborB = neighbors.Keys.Last();

					int cost = SimpleGraph[node][neighborA]
								+ SimpleGraph[node][neighborB];

					if (!SimpleGraph[neighborA].ContainsKey(neighborB) || SimpleGraph[neighborA][neighborB] > cost) {
						SimpleGraph[neighborA][neighborB] = cost;
						SimpleGraph[neighborB][neighborA] = cost;
					}

					SimpleGraph[neighborA].Remove(node);
					SimpleGraph[neighborB].Remove(node);

					SimpleGraph.Remove(node);
					return true;
				}
			}

			// MST test
			foreach (SkillNode node in SimpleGraph.Keys) {
				if (SolveSet.Contains(node))
					continue;

				if (SimpleGraph[node].Count != 3)
					continue;

				if (!ANY_MST_OF_NEIGHBORS_INCLUDES(node)) {
					Console.WriteLine("I'm helping!");
					foreach (SkillNode next in SimpleGraph[node].Keys)
						SimpleGraph[next].Remove(node);
					SimpleGraph.Remove(node);
					return true;
				}
			}

			// Prune edges to neighbor if a shorter non-direct path exists.
			// May be able to swap this with solve simple graph.
			foreach (SkillNode origin in SimpleGraph.Keys) {
				foreach (SkillNode destination in SimpleGraph[origin].Keys) {
					int distance = SimpleGraph[origin][destination];
					if (ShortestOtherPath(origin, destination) <= distance) {
						SimpleGraph[origin].Remove(destination);
						SimpleGraph[destination].Remove(origin);
						return true;
					}
				}
			}

			// Removes edges that move away from everything.
			foreach (SkillNode node in SimpleGraph.Keys) {
				foreach (var neighbor in SimpleGraph[node].Keys) {
					int best = 0x7ffffff;
					foreach (SkillNode ep in SolveSet) {
						int myDist = ShortestPathTable[node][ep].Item2;
						int itDist = ShortestPathTable[neighbor][ep].Item2;
						int cost = SimpleGraph[node][neighbor];
						int diff = (itDist + cost) - myDist;
						if (diff < best)
							best = diff;
					}

					if (best > 0) {
						SimpleGraph[node].Remove(neighbor);
						SimpleGraph[neighbor].Remove(node);
						return true;
					}
				}
			}

			return false;
		}

		private int ShortestPath(SkillNode origin, SkillNode destination)
		{
			HashSet<Tuple<SkillNode, int>> queue = new HashSet<Tuple<SkillNode, int>>();
			HashSet<SkillNode> visited = new HashSet<SkillNode>();

			queue.Add(new Tuple<SkillNode, int>(origin, 0));

			while (queue.Count > 0) {
				Tuple<SkillNode, int> min = queue.OrderBy(tuple => tuple.Item2).First();

				if (min.Item1 == destination) {
					return min.Item2;
				}

				queue.Remove(min);

				visited.Add(min.Item1);
				foreach (SkillNode neighbor in SimpleGraph[min.Item1].Keys) {
					if (!visited.Contains(neighbor))
						queue.Add(new Tuple<SkillNode, int>(neighbor, min.Item2 + SimpleGraph[min.Item1][neighbor]));
				}
			}

			return 0x7FFFFFFF;
		}

		private int ShortestOtherPath(SkillNode origin, SkillNode destination)
		{
			HashSet<Tuple<SkillNode, int>> queue = new HashSet<Tuple<SkillNode, int>>();
			HashSet<SkillNode> visited = new HashSet<SkillNode>();

			queue.Add(new Tuple<SkillNode, int>(origin, 0));

			while (queue.Count > 0) {
				Tuple<SkillNode, int> min = queue.OrderBy(tuple => tuple.Item2).First();

				if (min.Item1 == destination) {
					return min.Item2;
				}

				queue.Remove(min);

				visited.Add(min.Item1);
				foreach (SkillNode neighbor in SimpleGraph[min.Item1].Keys) {
					if (min.Item1 == origin && neighbor == destination)
						continue;
					if (!visited.Contains(neighbor))
						queue.Add(new Tuple<SkillNode, int>(neighbor, min.Item2 + SimpleGraph[min.Item1][neighbor]));
				}
			}

			return 0x7FFFFFFF;
		}

		private void ConstructSimpleGraph()
		{
			SimpleGraph.Clear();

			foreach (SkillNode node in Skillnodes.Values) {
				if (!SolveSet.Contains(node) && (node.spc != null || node.Mastery || node.isExternal))
					continue;

				SimpleGraph.Add(node, new Dictionary<SkillNode, int>());
				foreach (SkillNode neighbor in node.Neighbor) {
					if (!SolveSet.Contains(neighbor) && (neighbor.spc != null || neighbor.Mastery || neighbor.isExternal))
						continue;

					SimpleGraph[node].Add(neighbor, 1);
				}
			}

			while (Simplify()) ;	// good coding practices.

			Console.WriteLine("[Core Nodes] {0}", SimpleGraph.Count);
			Console.WriteLine("[Edges] {0}", SimpleGraph.Sum(dict => dict.Value.Count));
			Console.WriteLine("[Order] {0}", SimpleGraph.Average(dict => dict.Value.Count));
		}

        public static SkillTree CreateSkillTree(startLoadingWindow start = null, UpdateLoadingWindow update = null, closeLoadingWindow finish = null)
        {

            string skilltreeobj = "";
            if (Directory.Exists("Data"))
            {
                if (File.Exists("Data\\Skilltree.txt"))
                {
                    skilltreeobj = File.ReadAllText("Data\\Skilltree.txt");
                }
            }
            else
            {
                Directory.CreateDirectory("Data");
                Directory.CreateDirectory("Data\\Assets");
            }

            if (skilltreeobj == "")
            {
                bool displayProgress = (start != null && update != null && finish != null);
                if (displayProgress)
                    start();
                //loadingWindow.Dispatcher.Invoke(DispatcherPriority.Background,new Action(delegate { }));


                string uriString = "http://www.pathofexile.com/passive-skill-tree/";
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uriString);
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                string code = new StreamReader(resp.GetResponseStream()).ReadToEnd();
                Regex regex = new Regex("var passiveSkillTreeData.*");
                skilltreeobj = regex.Match(code).Value.Replace("root", "main").Replace("\\/", "/");
                skilltreeobj = skilltreeobj.Substring(27, skilltreeobj.Length - 27 - 2) + "";
                File.WriteAllText("Data\\Skilltree.txt", skilltreeobj);
                if (displayProgress)
                    finish();
            }

            return new SkillTree(skilltreeobj, start, update, finish);
        }
        public SkillTree(String treestring, startLoadingWindow start = null, UpdateLoadingWindow update = null, closeLoadingWindow finish = null)
        {
            bool displayProgress = ( start != null && update != null && finish != null );
           // RavenJObject jObject = RavenJObject.Parse( treestring.Replace( "Additional " , "" ) );
          JsonSerializerSettings jss = new JsonSerializerSettings
            {
            Error = delegate(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
            {
            Debug.WriteLine(args.ErrorContext.Error.Message);
            args.ErrorContext.Handled = true;
            }
            };

          var inTree = JsonConvert.DeserializeObject<PoEClasses.PoESkillTree>(treestring.Replace("Additional ", ""), jss);
            int qindex = 0;


            foreach (var obj in inTree.skillSprites)
            {
                if (obj.Key.Contains("inactive"))
                    continue;
                iconActiveSkills.Images[obj.Value[3].filename] = null;
                foreach (var o in obj.Value[3].coords)
                {
                    iconActiveSkills.SkillPositions[o.Key] = new KeyValuePair<Rect, string>(new Rect(o.Value.x, o.Value.y, o.Value.w, o.Value.h), obj.Value[3].filename);
                }
            }
            foreach (var obj in inTree.skillSprites)
            {
                if (obj.Key.Contains("active"))
                    continue;
                iconActiveSkills.Images[obj.Value[3].filename] = null;
                foreach (var o in obj.Value[3].coords)
                {
                    iconActiveSkills.SkillPositions[o.Key] = new KeyValuePair<Rect, string>(new Rect(o.Value.x, o.Value.y, o.Value.w, o.Value.h), obj.Value[3].filename);
                }
            }   
            qindex = 0;    

            foreach(var ass in inTree.assets)
            {
               
                assets[ass.Key] = new Asset(ass.Key,ass.Value.ContainsKey(0.3835f)?ass.Value[0.3835f]:ass.Value.Values.First());
                                     
            }
           
            if ( displayProgress )
                start( );
            iconActiveSkills.OpenOrDownloadImages(update );
            iconInActiveSkills.OpenOrDownloadImages(update );
            if ( displayProgress )
                finish( );
            foreach( var c in inTree.characterData)
            {
                CharBaseAttributes[c.Key] = new Dictionary<string, float>() { { "+# to Strength", c.Value.base_str }, { "+# to Dexterity", c.Value.base_dex }, { "+# to Intelligence", c.Value.base_int } };
            }
           foreach (var nd in inTree.nodes)
           {
               Skillnodes.Add(nd.id, new SkillTree.SkillNode()
               {
                   id = nd.id,                
                   name = nd.dn,
                   attributes = nd.sd,
                   orbit = nd.o,
                   orbitIndex =nd.oidx,
                   icon = nd.icon,
                   linkID =nd.ot,
                   g = nd.g,
                   da = nd.da,
                   ia = nd.ia,
                   ks = nd.ks,
                   not = nd.not,
                   sa = nd.sa,
                   Mastery = nd.m,
                   spc=nd.spc.Count()>0?(int?)nd.spc[0]:null
               });
           }         
            List<ushort[]> links = new List<ushort[]>( );
            foreach ( var skillNode in Skillnodes )
            {
                foreach ( ushort i in skillNode.Value.linkID )
                {
                    if (
                        links.Count(
                            nd => ( nd[ 0 ] == i && nd[ 1 ] == skillNode.Key ) || nd[ 0 ] == skillNode.Key && nd[ 1 ] == i ) ==
                        1 )
                    {
                        continue;
                    }
                    links.Add( new ushort[] { skillNode.Key , i } );
                }
            }
            foreach ( ushort[] ints in links )
            {
                if ( !Skillnodes[ ints[ 0 ] ].Neighbor.Contains( Skillnodes[ ints[ 1 ] ] ) )
                    Skillnodes[ ints[ 0 ] ].Neighbor.Add( Skillnodes[ ints[ 1 ] ] );
                if ( !Skillnodes[ ints[ 1 ] ].Neighbor.Contains( Skillnodes[ ints[ 0 ] ] ) )
                    Skillnodes[ ints[ 1 ] ].Neighbor.Add( Skillnodes[ ints[ 0 ] ] );
            }
           
            foreach(var gp in inTree.groups )
            {
                NodeGroup ng = new NodeGroup();

                ng.OcpOrb = gp.Value.oo;
                ng.Position = new Vector2D(gp.Value.x, gp.Value.y);
                ng.Nodes = gp.Value.n;
                NodeGroups.Add(ng);
            }
          
            foreach ( SkillTree.NodeGroup group in NodeGroups )
            {
                foreach ( ushort node in group.Nodes )
                {
                    Skillnodes[ node ].NodeGroup = group;
                }
            }

            TRect = new Rect2D( new Vector2D( inTree.min_x * 1.1 , inTree.min_y * 1.1 ) ,
                               new Vector2D(inTree.max_x * 1.1, inTree.max_y * 1.1));




            InitNodeSurround( );
            DrawNodeSurround( );
            DrawNodeBaseSurround( );
            DrawSkillIconLayer( );
            DrawBackgroundLayer( );
            InitFaceBrushesAndLayer( );
            DrawLinkBackgroundLayer( );
            InitOtherDynamicLayers( );
            CreateCombineVisual( );


            Regex regexAttrib = new Regex( "[0-9]*\\.?[0-9]+" );
            foreach ( var skillNode in Skillnodes )
            {
                skillNode.Value.Attributes = new Dictionary<string , List<float>>( );
                foreach ( string s in skillNode.Value.attributes )
                {

                    List<float> values = new List<float>( );

                    foreach ( Match m in regexAttrib.Matches( s ) )
                    {

                        if ( !AttributeTypes.Contains( regexAttrib.Replace( s , "#" ) ) )
                            AttributeTypes.Add( regexAttrib.Replace( s , "#" ) );
                        if ( m.Value == "" )
                            values.Add( float.NaN );
                        else
                            values.Add( float.Parse( m.Value , System.Globalization.CultureInfo.InvariantCulture ) );

                    }
                    string cs = ( regexAttrib.Replace( s , "#" ) );

                    skillNode.Value.Attributes[ cs ] = values;



                }
            }

			CalculateAllShortestPaths();
        }
        public Dictionary<string, List<float>> ImplicitAttributes(Dictionary<string, List<float>> attribs)
        {
            Dictionary<string, List<float>> retval = new Dictionary<string, List<float>>();
            // +# to Strength", co["base_str"].Value<int>() }, { "+# to Dexterity", co["base_dex"].Value<int>() }, { "+# to Intelligence", co["base_int"].Value<int>() } };
            retval["+# to maximum Mana"] = new List<float>() { attribs["+# to Intelligence"][0] / IntPerMana + level * ManaPerLevel };
            retval["+#% Energy Shield"] = new List<float>() { attribs["+# to Intelligence"][0] / IntPerES };

            retval["+# to maximum Life"] = new List<float>() { attribs["+# to Strength"][0] / IntPerMana + level * LifePerLevel };
            retval["+#% increased Melee Physical Damage"] = new List<float>() { attribs["+# to Strength"][0] / StrPerED };

            retval["+# Accuracy Rating"] = new List<float>() { attribs["+# to Dexterity"][0] / DexPerAcc };
            retval["Evasion Rating: #"] = new List<float>() { level * EvasPerLevel };
            retval["#% increased Evasion Rating"] = new List<float>() { attribs["+# to Dexterity"][0] / DexPerEvas };
            return retval;
        }
        public int Level
        {
            get
            {
                return level;
            }
            set
            {
                level = value;
            }
        }
        public int Chartype
        {
            get
            {
                return chartype;
            }
            set
            {
				var oldChar = Skillnodes.First(nd => nd.Value.name.ToUpper() == CharName[chartype]);

				if (SolveSet.Contains(oldChar.Value))
					SolveSet.Remove(oldChar.Value);

                chartype = value;
                SkilledNodes.Clear();
                var node = Skillnodes.First(nd => nd.Value.name.ToUpper() == CharName[chartype]);
				SolveSet.Add(node.Value);
                SkilledNodes.Add(node.Value.id);
                UpdateAvailNodes();
                DrawFaces();
				Solve();
            }
        }
        public List<ushort> GetShortestPathTo(ushort targetNode)
        {
            if (SkilledNodes.Contains(targetNode))
                return new List<ushort>();
            if (AvailNodes.Contains(targetNode))
                return new List<ushort>() { targetNode };
            HashSet<ushort> visited = new HashSet<ushort>(SkilledNodes);
            Dictionary<int, int> distance = new Dictionary<int, int>();
            Dictionary<ushort, ushort> parent = new Dictionary<ushort, ushort>();
            Queue<ushort> newOnes = new Queue<ushort>();
            foreach (var node in SkilledNodes)
            {
                distance.Add(node, 0);
            }
            foreach (var node in AvailNodes)
            {
                newOnes.Enqueue(node);
                distance.Add(node, 1);
            }
            while (newOnes.Count > 0)
            {
                ushort newNode = newOnes.Dequeue();
                int dis = distance[newNode];
                visited.Add(newNode);
                foreach (var connection in Skillnodes[newNode].Neighbor.Select(nd => nd.id))
                {
                    if (visited.Contains(connection))
                        continue;
                    if (distance.ContainsKey(connection))
                        continue;
                    if (Skillnodes[newNode].spc.HasValue)
                        continue;
                    if (Skillnodes[newNode].Mastery)
                        continue;
                    distance.Add(connection, dis + 1);
                    newOnes.Enqueue(connection);

                    parent.Add(connection, newNode);

                    if (connection == targetNode)
                        break;
                }
            }

            if (!distance.ContainsKey(targetNode))
                return new List<ushort>();

            Stack<ushort> path = new Stack<ushort>();
            ushort curr = targetNode;
            path.Push(curr);
            while (parent.ContainsKey(curr))
            {
                path.Push(parent[curr]);
                curr = parent[curr];
            }

            List<ushort> result = new List<ushort>();
            while (path.Count > 0)
                result.Add(path.Pop());

            return result;
        }
        public HashSet<ushort> ForceRefundNodePreview(ushort nodeId)
        {
            if (!SkilledNodes.Remove(nodeId))
                return new HashSet<ushort>();

            SkilledNodes.Remove(nodeId);

            HashSet<ushort> front = new HashSet<ushort>();
            front.Add(SkilledNodes.First());
            foreach (var i in Skillnodes[SkilledNodes.First()].Neighbor)
                if (SkilledNodes.Contains(i.id))
                    front.Add(i.id);

            HashSet<ushort> skilled_reachable = new HashSet<ushort>(front);
            while (front.Count > 0)
            {
                HashSet<ushort> newFront = new HashSet<ushort>();
                foreach (var i in front)
                    foreach (var j in Skillnodes[i].Neighbor.Select(nd => nd.id))
                        if (!skilled_reachable.Contains(j) && SkilledNodes.Contains(j))
                        {
                            newFront.Add(j);
                            skilled_reachable.Add(j);
                        }

                front = newFront;
            }

            HashSet<ushort> unreachable = new HashSet<ushort>(SkilledNodes);
            foreach (var i in skilled_reachable)
                unreachable.Remove(i);
            unreachable.Add(nodeId);

            SkilledNodes.Add(nodeId);

            return unreachable;
        }
        public void ForceRefundNode(ushort nodeId)
        {
            if (!SkilledNodes.Remove(nodeId))
                throw new InvalidOperationException();

            //SkilledNodes.Remove(nodeId);

            HashSet<ushort> front = new HashSet<ushort>();
            front.Add(SkilledNodes.First());
            foreach (var i in Skillnodes[SkilledNodes.First()].Neighbor)
                if (SkilledNodes.Contains(i.id))
                    front.Add(i.id);
            HashSet<ushort> skilled_reachable = new HashSet<ushort>(front);
            while (front.Count > 0)
            {
                HashSet<ushort> newFront = new HashSet<ushort>();
                foreach (var i in front)
                    foreach (var j in Skillnodes[i].Neighbor.Select(nd => nd.id))
                        if (!skilled_reachable.Contains(j) && SkilledNodes.Contains(j))
                        {
                            newFront.Add(j);
                            skilled_reachable.Add(j);
                        }

                front = newFront;
            }

            SkilledNodes = skilled_reachable;
            AvailNodes = new HashSet<ushort>();
            UpdateAvailNodes();
        }
        public void LoadFromURL(string url)
        {
            string s = url.Substring(TreeAddress.Length + (url.StartsWith("https") ? 1 : 0)).Replace("-", "+").Replace("_", "/");
            byte[] decbuff = Convert.FromBase64String(s);
            var i = BitConverter.ToInt32(new byte[] { decbuff[3], decbuff[2], decbuff[1], decbuff[1] }, 0);
            var b = decbuff[4];
            var j = 0L;
            if (i > 0)
                j = decbuff[5];
            List<UInt16> nodes = new List<UInt16>();
            for (int k = 6; k < decbuff.Length; k += 2)
            {
                byte[] dbff = new byte[] { decbuff[k + 1], decbuff[k + 0] };
                if (Skillnodes.Keys.Contains(BitConverter.ToUInt16(dbff, 0)))
                    nodes.Add((BitConverter.ToUInt16(dbff, 0)));

            }
            Chartype = b;
            SkilledNodes.Clear();
            SkillTree.SkillNode startnode = Skillnodes.First(nd => nd.Value.name.ToUpper() == CharName[Chartype].ToUpper()).Value;
            SkilledNodes.Add(startnode.id);
            foreach (ushort node in nodes)
            {
                SkilledNodes.Add(node);
            }
            UpdateAvailNodes();
        }
        public string SaveToURL()
        {
            byte[] b = new byte[(SkilledNodes.Count - 1) * 2 + 6];
            var b2 = BitConverter.GetBytes(2);
            b[0] = b2[3];
            b[1] = b2[2];
            b[2] = b2[1];
            b[3] = b2[0];
            b[4] = (byte)(Chartype);
            b[5] = (byte)(0);
            int pos = 6;
            foreach (var inn in SkilledNodes)
            {
                if (CharName.Contains(Skillnodes[inn].name.ToUpper()))
                    continue;
                byte[] dbff = BitConverter.GetBytes((Int16)inn);
                b[pos++] = dbff[1];
                b[pos++] = dbff[0];
            }
            return TreeAddress + Convert.ToBase64String(b).Replace("/", "_").Replace("+", "-");

        }
        public void UpdateAvailNodes()
        {
            AvailNodes.Clear();
            foreach (ushort inode in SkilledNodes)
            {
                SkillNode node = Skillnodes[inode];
                foreach (SkillNode skillNode in node.Neighbor)
                {
                    if (!CharName.Contains(skillNode.name) && !SkilledNodes.Contains(skillNode.id))
                        AvailNodes.Add(skillNode.id);
                }
            }
            //  picActiveLinks = new DrawingVisual();

            Pen pen2 = new Pen(Brushes.Yellow, 15f);

            using (DrawingContext dc = picActiveLinks.RenderOpen())
            {
                foreach (var n1 in SkilledNodes)
                {
                    foreach (var n2 in Skillnodes[n1].Neighbor)
                    {
                        if (SkilledNodes.Contains(n2.id))
                        {
                            DrawConnection(dc, pen2, n2, Skillnodes[n1]);
                        }
                    }
                }
            }
            // picActiveLinks.Clear();
            DrawNodeSurround();
        }
        public Dictionary<string, List<float>> SelectedAttributes
        {
            get
            {
                Dictionary<string, List<float>> temp = SelectedAttributesWithoutImplicit;

                foreach (var a in ImplicitAttributes(temp))
                {
                    if (!temp.ContainsKey(a.Key))
                        temp[a.Key] = new List<float>();
                    for (int i = 0; i < a.Value.Count; i++)
                    {

                        if (temp.ContainsKey(a.Key) && temp[a.Key].Count > i)
                            temp[a.Key][i] += a.Value[i];
                        else
                        {
                            temp[a.Key].Add(a.Value[i]);
                        }
                    }
                }
                return temp;
            }
        }
        public Dictionary<string, List<float>> SelectedAttributesWithoutImplicit
        {
            get
            {
                Dictionary<string, List<float>> temp = new Dictionary<string, List<float>>();
                foreach (var attr in CharBaseAttributes[Chartype])
                {
                    if (!temp.ContainsKey(attr.Key))
                        temp[attr.Key] = new List<float>();

                    if (temp.ContainsKey(attr.Key) && temp[attr.Key].Count > 0)
                        temp[attr.Key][0] += attr.Value;
                    else
                    {
                        temp[attr.Key].Add(attr.Value);
                    }
                }

                foreach (var attr in BaseAttributes)
                {
                    if (!temp.ContainsKey(attr.Key))
                        temp[attr.Key] = new List<float>();

                    if (temp.ContainsKey(attr.Key) && temp[attr.Key].Count > 0)
                        temp[attr.Key][0] += attr.Value;
                    else
                    {
                        temp[attr.Key].Add(attr.Value);
                    }
                }

                foreach (ushort inode in SkilledNodes)
                {
                    SkillNode node = Skillnodes[inode];
                    foreach (var attr in node.Attributes)
                    {
                        if (!temp.ContainsKey(attr.Key))
                            temp[attr.Key] = new List<float>();
                        for (int i = 0; i < attr.Value.Count; i++)
                        {

                            if (temp.ContainsKey(attr.Key) && temp[attr.Key].Count > i)
                                temp[attr.Key][i] += attr.Value[i];
                            else
                            {
                                temp[attr.Key].Add(attr.Value[i]);
                            }
                        }

                    }
                }

                return temp;
            }
        }
        public class SkillIcons
        {

            public enum IconType
            {
                normal,
                notable,
                keystone
            }

            public Dictionary<string, KeyValuePair<Rect, string>> SkillPositions = new Dictionary<string, KeyValuePair<Rect, string>>();
            public Dictionary<String, BitmapImage> Images = new Dictionary<string, BitmapImage>();
            public static string urlpath = "http://www.pathofexile.com/image/build-gen/passive-skill-sprite/";
            public void OpenOrDownloadImages(UpdateLoadingWindow update = null)
            {
                //Application
                int count = 0;
                foreach (var image in Images.Keys.ToArray())
                {
                    if (!File.Exists("Data\\Assets\\" + image))
                    {
                        System.Net.WebClient _WebClient = new System.Net.WebClient();
                        _WebClient.DownloadFile(urlpath + image, "Data\\Assets\\" + image);
                    }
                    Images[image] = new BitmapImage(new Uri("Data\\Assets\\" + image, UriKind.Relative));
                    if (update != null)
                        update(count * 100 / Images.Count, 100);
                    ++count;
                }
            }
        }
        public class NodeGroup
        {
            public Vector2D Position;// "x": 1105.14,"y": -5295.31,
            public Dictionary<int, bool> OcpOrb = new Dictionary<int, bool>(); //  "oo": {"1": true},
            public List<int> Nodes = new List<int>();// "n": [-28194677,769796679,-1093139159]

        }
        public class SkillNode
        {
            static public float[] skillsPerOrbit = { 1, 6, 12, 12, 12 };
            static public float[] orbitRadii = { 0, 81.5f, 163, 326, 489 };
            public HashSet<int> Connections = new HashSet<int>();
            public bool skilled = false;
            public UInt16 id; // "id": -28194677,
            public string icon;// icon "icon": "Art/2DArt/SkillIcons/passives/tempint.png",
            public bool ks; //"ks": false,
            public bool not;   // not": false,
            public string name;//"dn": "Block Recovery",
            public int a;// "a": 3,
            public string[] attributes;// "sd": ["8% increased Block Recovery"],
            public Dictionary<string, List<float>> Attributes;
            // public List<string> AttributeNames;
            //public List<> AttributesValues;
            public int g;// "g": 1,
            public int orbit;//  "o": 1,
            public int orbitIndex;// "oidx": 3,
            public int sa;//s "sa": 0,
            public int da;// "da": 0,
            public int ia;//"ia": 0,
            public List<int> linkID = new List<int>();// "out": []
            public bool Mastery;
            public int? spc;

            public List<SkillNode> Neighbor = new List<SkillNode>();
            public NodeGroup NodeGroup;
            public Vector2D Position
            {
                get
                {
                if(NodeGroup==null) return new Vector2D();
                    double d = orbitRadii[this.orbit];
                    double b = (2 * Math.PI * this.orbitIndex / skillsPerOrbit[this.orbit]);
                    return (NodeGroup.Position - new Vector2D(d * Math.Sin(-b), d * Math.Cos(-b)));
                }
            }
            public double Arc
            {
                get
                {
                    return (2 * Math.PI * this.orbitIndex / skillsPerOrbit[this.orbit]);
                }
            }

			public bool isExternal { get; set; }

			public bool isInternal { get; set; }
		}
        public class Asset
        {
            public string Name;
            public BitmapImage PImage;
            public string URL;
            public Asset(string name, string url)
            {
                Name = name;
                URL = url;
                if (!File.Exists("Data\\Assets\\" + Name + ".png"))
                {

                    System.Net.WebClient _WebClient = new System.Net.WebClient();
                    _WebClient.DownloadFile(URL, "Data\\Assets\\" + Name + ".png");



                }
                PImage = new BitmapImage(new Uri("Data\\Assets\\" + Name + ".png", UriKind.Relative));

            }

        }

        public void HighlightNodes(string search, bool useregex)
        {
            if (search == "")
            {
                DrawHighlights(highlightnodes = new List<SkillTree.SkillNode>());
                highlightnodes = null;
                return;
            }

            if (useregex)
            {
                try
                {
                    List<SkillTree.SkillNode> nodes = highlightnodes = Skillnodes.Values.Where(nd => nd.attributes.Where(att => new Regex(search, RegexOptions.IgnoreCase).IsMatch(att)).Count() > 0 || new Regex(search, RegexOptions.IgnoreCase).IsMatch(nd.name) && !nd.Mastery).ToList();
                    DrawHighlights(highlightnodes);
                }
                catch (Exception)
                {
                }

            }
            else
            {
                highlightnodes = Skillnodes.Values.Where(nd => nd.attributes.Where(att => att.ToLower().Contains(search.ToLower())).Count() != 0 || nd.name.ToLower().Contains(search.ToLower()) && !nd.Mastery).ToList();

                DrawHighlights(highlightnodes);
            }
        }
        public void SkillAllHighligtedNodes()
        {
            if (highlightnodes == null)
                return;
            HashSet<int> nodes = new HashSet<int>();
            foreach (var nd in highlightnodes)
            {
                nodes.Add(nd.id);
            }
            SkillStep(nodes);

        }
        private HashSet<int> SkillStep(HashSet<int> hs)
        {
            List<List<ushort>> pathes = new List<List<ushort>>();
            foreach (var nd in highlightnodes)
            {
                pathes.Add(GetShortestPathTo(nd.id));


            }
            pathes.Sort((p1, p2) => p1.Count.CompareTo(p2.Count));
            pathes.RemoveAll(p => p.Count == 0);
            foreach (ushort i in pathes[0])
            {
                hs.Remove(i);
                SkilledNodes.Add(i);
            }
            UpdateAvailNodes();

            return hs.Count == 0 ? hs : SkillStep(hs);
        }

    }


}
