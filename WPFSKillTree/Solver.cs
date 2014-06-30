using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using POESKillTree.Utility;
using System.Diagnostics;

namespace POESKillTree
{
	public class Solver
	{
        public class Edge
		{
            public ushort left;
            public ushort right;

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
				return (left < right)
					? (left << 16) | right
					: (right << 16) | left;
			}
		}

        public class BitFieldSet<T>
        {
            private ulong[] bitField;
            private BitFieldSetFactory<T> parent;

            public BitFieldSet(ulong[] field, BitFieldSetFactory<T> parent)
            {
                this.bitField = field;
                this.parent = parent;
            }

            public void Recycle()
            {
                parent.bufferPool.Push(bitField);
                this.bitField = null;
            }

            public bool Contains(T value)
            {
                return this[parent.GetId(value)];
            }

            public void Add(T value)
            {
                this[parent.GetId(value)] = true;
            }

            public void Merge(BitFieldSet<T> other)
            {
                for (ushort i = 0; i < bitField.Length; i++)
                {
                    this.bitField[i] |= other.bitField[i];
                }
            }

            public List<ushort> GetIds(){
                List<ushort> ids = new List<ushort>();
                for(ushort i = 0; i < bitField.Length; i++){
                    if(bitField[i] == 0){
                        continue;
                    }
                    for(ushort j = 0; j < 64; j++){
                        if((bitField[i] & (1ul << j)) !=0 ){
                            ids.Add((ushort)(64*i + j));
                        }
                    }
                }
                return ids;
            }

            public List<T> ToList()
            {
                List<T> objects = new List<T>();
                for (ushort i = 0; i < bitField.Length; i++)
                {
                    if (bitField[i] == 0)
                    {
                        continue;
                    }
                    for (ushort j = 0; j < 64; j++)
                    {
                        if ((bitField[i] & (1ul << j)) != 0)
                        {
                            ushort id = (ushort)(64 * i + j);
                            objects.Add(parent.registry[id]);
                        }
                    }
                }
                return objects;
            }

            public void CopyTo(BitFieldSet<T> other)
            {
                this.bitField.CopyTo(other.bitField, 0);
            }

            public override bool Equals(object obj)
            {
                for (int i = 0; i < bitField.Length; i++)
                {
                    if (this.bitField[i] != (obj as BitFieldSet<T>).bitField[i])
                        return false;
                }
                return true;
            }

            public override int GetHashCode()
            {
                ulong hashCode = 0;
                for (int i = 0; i < bitField.Length; i++)
                    hashCode ^= bitField[i];
                return (int)hashCode ^ (int)(hashCode >> 32);
            }

            public bool this[int index]
            {
                get
                {
                     return (bitField[index >> 6] & (1uL << (index & 0x3f))) != 0;
                }
                set
                {
                    if(value){
                        bitField[index >> 6] |= 1uL << (index & 0x3f);
                    }else{
                        bitField[index >> 6] &= ~(1uL << (index & 0x3f));
                    }   
                }
            }
        }

        public class BitFieldSetFactory<T>
        {
            private int bitArraySize;
            private ushort idCount = 1;
            public Stack<ulong[]> bufferPool = new Stack<ulong[]>();
            public Dictionary<ushort, T> registry;
            public Dictionary<T, ushort> reverseRegistry;

            public BitFieldSetFactory(int size)
            {
                this.bitArraySize = (size >> 6) + 1;
                registry = new Dictionary<ushort, T>();
                reverseRegistry = new Dictionary<T, ushort>();
            }

            public void Register(T value)
            {
                if (!reverseRegistry.ContainsKey(value))
                {
                    reverseRegistry[value] = idCount;
                    registry[idCount] = value;
                    idCount++;
                }
            }

            public ushort GetId(T value)
            {
                if (!reverseRegistry.ContainsKey(value))
                {
                    reverseRegistry[value] = idCount;
                    registry[idCount] = value;
                    idCount++;
                }
                return reverseRegistry[value];
            }

            public BitFieldSet<T> NewSet()
            {
                ulong[] bitArray = null;
                while (bufferPool.Count > 0 && bitArray == null)
                {
                    bitArray = bufferPool.Pop();
                }

                if (bitArray == null)
                {
                    bitArray = new ulong[bitArraySize];
                }
                else
                {
                    Array.Clear(bitArray, 0, bitArray.Length);
                    recycleCount++;
                }
                return new BitFieldSet<T>(bitArray, this);
            }

            public BitFieldSet<T> NewSet(BitFieldSet<T> clone)
            {
                ulong[] bitArray = null;
                while (bufferPool.Count > 0 && bitArray == null)
                {
                    bitArray = bufferPool.Pop();
                }

                if (bitArray == null)
                {
                    bitArray = new ulong[bitArraySize];
                }
                else
                {
                    recycleCount++;
                }
                BitFieldSet<T> newSet = new BitFieldSet<T>(bitArray, this);
                clone.CopyTo(newSet);
                return newSet;
            }
        }

        public class TreePart
			: IEquatable<TreePart>
		{
            public TreePart(ushort start)
			{
                this.Nodes = NodeSetFactory.NewSet();
				this.Edges = EdgeSetFactory.NewSet();
				this.Size = 0;

				this.Nodes.Add(start);
			}

			public TreePart(TreePart clone)
			{
                this.Nodes = NodeSetFactory.NewSet(clone.Nodes);
                this.Edges = EdgeSetFactory.NewSet(clone.Edges);
				this.Size = clone.Size;
			}

        //    ~TreePart()
       //     {
      //          Recycle();
      //      }

			public void merge(TreePart other)
			{
				Nodes.Merge(other.Nodes);
                Edges.Merge(other.Edges);
				Size += other.Size;
			}

			public bool Equals(TreePart other)
			{
				if (this.Size != other.Size)
					return false;
				return this.Edges.Equals(other.Edges);
			}

			public override bool Equals(object obj)
			{
				if (obj is TreePart)
					return Equals(obj as TreePart);
				return false;
			}

			public override int GetHashCode()
			{
                return Edges.GetHashCode();
			}

            public void Recycle()
            {
                this.Edges.Recycle();
                this.Nodes.Recycle();
            }

			public int Size;
            public BitFieldSet<Edge> Edges;
            public BitFieldSet<ushort> Nodes;
		}

        public class TreeGroup
			: IEquatable<TreeGroup>
		{
			public TreeGroup(TreePart a, TreePart[] b)
			{
				this.Parts = new TreePart[b.Length];
				this.Smallest = a;
                this.Edges = EdgeSetFactory.NewSet(a.Edges);
				
				this.Size = a.Size;
				int index = 0;
				for (int i = 0; i < b.Length; ++i) {
					this.Size += b[i].Size;

                    this.Edges.Merge(b[i].Edges);

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
                this.Edges = EdgeSetFactory.NewSet();

				this.Size = a[0].Size;
				int index = 0;
				for (int i = 1; i < a.Length; ++i) {
					this.Size += a[i].Size;

                    this.Edges.Merge(a[i].Edges);

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
                return Edges.GetHashCode();
			}

			public bool Equals(TreeGroup other)
			{
				if (this.Size != other.Size)
					return false;
				
				return this.Edges.Equals(other.Edges);
			}

			public override bool Equals(object obj)
			{
				if (obj is TreeGroup)
					return Equals(obj as TreeGroup);
				return false;
			}

            public void Recycle()
            {
                Edges.Recycle();
            }

			public int Size;
			public TreePart[] Parts;
			public TreePart Smallest;
            public BitFieldSet<Edge> Edges;
		}

		public List<TreePart> Solve(HashSet<ushort> solveSet)
		{
			var graph = ConstructSimpleGraph(solveSet);
			
			RemoveExternals(graph, solveSet);

            
            Solver.EdgeSetFactory = new BitFieldSetFactory<Edge>(graph.Values.Sum(n => n.Count));
            Solver.NodeSetFactory = new BitFieldSetFactory<ushort>(graph.Count);

			Stopwatch simplifyTime = Stopwatch.StartNew();
			while (Simplify(graph, solveSet)) ;
			simplifyTime.Stop();
			Console.WriteLine("Simplify time: {0}", simplifyTime.Elapsed);

			Console.WriteLine("Nodes: {0}", graph.Count);
			Console.WriteLine("Edges: {0}", graph.Sum(dict => dict.Value.Count));

			Stopwatch solutionTime = Stopwatch.StartNew();

            recycleCount = 0;
            Solver.EdgeSetFactory = new BitFieldSetFactory<Edge>(graph.Values.Sum(n => n.Count));
            Solver.NodeSetFactory = new BitFieldSetFactory<ushort>(graph.Count);

			var solutions = SolveSimpleGraph(graph, solveSet, 120);
			solutionTime.Stop();
			Console.WriteLine("{0} solutions found!", solutions.Count);
			if (solutions.Count > 0)
				Console.WriteLine("{0} points used.", solutions.First().Size);
			Console.WriteLine("Solve time: {0}", solutionTime.Elapsed);
            Console.WriteLine(recycleCount);
			return solutions;
		}

		public Solver(Dictionary<ushort, SkillTree.SkillNode> graph)
		{
			this.Graph = graph;
			this.ShortestPathTable = new Dictionary<ushort, Dictionary<ushort, Tuple<HashSet<ushort>, int>>>();

            CalculateAllShortestPaths();
		}


		private void CalculateAllShortestPaths()
		{
			foreach (var skillNode in Graph.Values) {
				ShortestPathTable[skillNode.id] = new Dictionary<ushort, Tuple<HashSet<ushort>, int>>();
				ShortestPathTable[skillNode.id][skillNode.id] = new Tuple<HashSet<ushort>, int>(new HashSet<ushort>(), 0);
				foreach (var neighbor in skillNode.Neighbor) {
					ShortestPathTable[skillNode.id][neighbor.id] = new Tuple<HashSet<ushort>, int>(new HashSet<ushort>(), 1);
					ShortestPathTable[skillNode.id][neighbor.id].Item1.Add(neighbor.id);
				}
			}

			foreach (var originNode in Graph.Values) {
				HashSet<SkillTree.SkillNode> visited = new HashSet<SkillTree.SkillNode>();
				HashSet<SkillTree.SkillNode> frontier = new HashSet<SkillTree.SkillNode>();
				visited.Add(originNode);
				frontier.UnionWith(originNode.Neighbor);
				while (frontier.Count > 0) {
					HashSet<SkillTree.SkillNode> newFrontier = new HashSet<SkillTree.SkillNode>();
					foreach (var frontierNode in frontier) {
						foreach (var neighbor in frontierNode.Neighbor) {
							if (!visited.Contains(neighbor) && !frontier.Contains(neighbor)) {
								newFrontier.Add(neighbor);
								if (!ShortestPathTable[originNode.id].ContainsKey(neighbor.id))
									ShortestPathTable[originNode.id][neighbor.id] = new Tuple<HashSet<ushort>, int>(new HashSet<ushort>(), ShortestPathTable[originNode.id][frontierNode.id].Item2 + 1);
								ShortestPathTable[originNode.id][neighbor.id].Item1.UnionWith(ShortestPathTable[originNode.id][frontierNode.id].Item1);
							}
						}
					}
					visited.UnionWith(frontier);
					frontier = newFrontier;
				}
			}
		}

		private void RemoveExternalRecursively(Dictionary<ushort, Dictionary<ushort, int>> graph, ushort node, HashSet<ushort> isInternal)
		{
			if (isInternal.Contains(node) || !graph.ContainsKey(node)) {
				return;
			}

			IEnumerable<ushort> neighbors = graph[node].Keys;
			graph.Remove(node);
			
			foreach (ushort neighbor in neighbors)
				graph[neighbor].Remove(node);
			foreach (ushort neighbor in neighbors)
				RemoveExternalRecursively(graph, neighbor, isInternal);
		}

		private void RemoveExternals(Dictionary<ushort, Dictionary<ushort, int>> graph, HashSet<ushort> solveSet)
		{
			HashSet<ushort> isInternal = new HashSet<ushort>();

			HashSet<ushort> remaining = new HashSet<ushort>(solveSet);
			foreach (var skillA in solveSet) {
				remaining.Remove(skillA);
				isInternal.Add(skillA);
				foreach (var skillB in remaining) {
					foreach (ushort node in GetShortestPathNodes(skillA, skillB)) {
						isInternal.Add(node);
					}
				}
			}

			List<ushort> external = new List<ushort> { 40633, 34098, 9660, 12926, 31961, 44941, 59763, 18663, 22535, 31703, 22115, 24426, 14914, 54922 };
			foreach (ushort externalId in external) {
				RemoveExternalRecursively(graph, externalId, isInternal);
			}
		}

		private void GetShortestPathNodes(ushort from, ushort to, HashSet<ushort> currentSet)
		{
			if (from == to) {
				currentSet.Add(to);
				return;
			}

			currentSet.Add(from);

			foreach (ushort pathStart in ShortestPathTable[from][to].Item1) {
				if (!currentSet.Contains(pathStart)) {
					GetShortestPathNodes(pathStart, to, currentSet);
				}
			}
		}

		private HashSet<ushort> GetShortestPathNodes(ushort from, ushort to)
		{
			HashSet<ushort> shortestPathNodes = new HashSet<ushort>();
			GetShortestPathNodes(from, to, shortestPathNodes);
			return shortestPathNodes;
		}

		private Dictionary<ushort, Dictionary<ushort, int>> ConstructSimpleGraph(HashSet<ushort> solveSet)
		{
			Dictionary<ushort, Dictionary<ushort, int>> graph = new Dictionary<ushort, Dictionary<ushort, int>>();

			foreach (SkillTree.SkillNode node in Graph.Values) {
				if (!solveSet.Contains(node.id) && (node.spc != null || node.Mastery))
					continue;

				graph.Add(node.id, new Dictionary<ushort, int>());
				foreach (SkillTree.SkillNode neighbor in node.Neighbor) {
					if (!solveSet.Contains(neighbor.id) && (neighbor.spc != null || neighbor.Mastery))
						continue;

					graph[node.id].Add(neighbor.id, 1);
				}
			}

			return graph;
		}

		private bool Simplify(Dictionary<ushort, Dictionary<ushort, int>> graph, HashSet<ushort> solveSet)
		{
            foreach (ushort node in graph.Keys) {
				if (solveSet.Contains(node)/* || SkilledNodes.Contains(node.id)*/)
					continue;

				Dictionary<ushort, int> neighbors = graph[node];

				if (neighbors.ContainsKey(node)) {
					neighbors.Remove(node);
					return true;
				}

				if (neighbors.Count == 0) {
					graph.Remove(node);
					return true;
				}

				if (neighbors.Count == 1) {
					graph[neighbors.Keys.First()].Remove(node);
					graph.Remove(node);
					return true;
				}

				if (graph[node].Count == 2) {
					ushort neighborA = neighbors.Keys.First();
					ushort neighborB = neighbors.Keys.Last();

					int cost = graph[node][neighborA]
							 + graph[node][neighborB];

					if (!graph[neighborA].ContainsKey(neighborB) || graph[neighborA][neighborB] > cost) {
						graph[neighborA][neighborB] = cost;
						graph[neighborB][neighborA] = cost;
					}

					graph[neighborA].Remove(node);
					graph[neighborB].Remove(node);

					graph.Remove(node);
					return true;
				}
			}

			// Prune edges to neighbor if a shorter non-direct path exists.
			// May be able to swap this with solve simple graph.
			foreach (ushort origin in graph.Keys) {
				foreach (ushort destination in graph[origin].Keys) {
					int distance = graph[origin][destination];
					List<TreePart> solutions = SolveSimpleGraph(graph, new ushort[] { origin, destination }, distance);
					if (solutions.Count > 1 || solutions.First().Size < distance) {

						graph[origin].Remove(destination);
						graph[destination].Remove(origin);
						return true;
					}
				}
			}

			// Removes edges that move away from everything.
			foreach (ushort node in graph.Keys) {
				foreach (var neighbor in graph[node].Keys) {
					int best = 0x7ffffff;
					foreach (ushort ep in solveSet) {
						int myDist = ShortestPathTable[node][ep].Item2;
						int itDist = ShortestPathTable[neighbor][ep].Item2;
						int cost = graph[node][neighbor];
						int diff = (itDist + cost) - myDist;
						if (diff < best)
							best = diff;
					}

					if (best > 0) {
						graph[node].Remove(neighbor);
						graph[neighbor].Remove(node);
						return true;
					}
				}
			}

			// MST test
			foreach (ushort node in graph.Keys) {
				if (solveSet.Contains(node))
					continue;

				if (graph[node].Count != 3)
					continue;

				int max = graph[node].Sum(kvp => kvp.Value);
				List<TreePart> solutions = SolveSimpleGraph(graph, graph[node].Keys, max);

				if (solutions.Count > 1 || solutions.First().Size < max) {
					ushort a = graph[node].ElementAt(0).Key;
					ushort b = graph[node].ElementAt(1).Key;
					ushort c = graph[node].ElementAt(2).Key;

					if (!graph[a].ContainsKey(b) || graph[a][b] > graph[node][a] + graph[node][b]) graph[a][b] = graph[b][a] = graph[node][a] + graph[node][b];
					if (!graph[a].ContainsKey(c) || graph[a][c] > graph[node][a] + graph[node][c]) graph[a][c] = graph[c][a] = graph[node][a] + graph[node][c];
					if (!graph[b].ContainsKey(c) || graph[b][c] > graph[node][b] + graph[node][c]) graph[b][c] = graph[c][b] = graph[node][b] + graph[node][c];

					graph[a].Remove(node);
					graph[b].Remove(node);
					graph[c].Remove(node);
					graph.Remove(node);

					return true;
				}
			}

			return false;
		}


		private List<TreePart> SolveSimpleGraph(Dictionary<ushort, Dictionary<ushort, int>> graph, IEnumerable<ushort> solveSet, int maxSize)
		{
			BucketQueue<TreeGroup> groups = new BucketQueue<TreeGroup>();

			List<TreePart> initialParts = new List<TreePart>();
			{
				foreach (ushort node in solveSet) {
					initialParts.Add(new TreePart(node));
				}
			}

			TreeGroup initialGroup = new TreeGroup(initialParts.ToArray());

			groups.Enqueue(initialGroup, initialGroup.Size);

			int generation = 0;

			HashSet<TreePart> solutions = new HashSet<TreePart>(EqualityComparer<TreePart>.Default);

			// DO WORK.
			while (!groups.IsEmpty()) {
				generation++;
				TreeGroup group = groups.Dequeue();

				if (group.Size > maxSize)
					break;

				if (generation % 100000 == 0) {
					Console.WriteLine(generation + " " + group.Size);
				}

				TreePart smallest = group.Smallest;

				foreach (ushort node in smallest.Nodes.ToList()) {
					int remaining = maxSize - group.Size;
					foreach (var next in graph[node]) {
						if (smallest.Nodes.Contains(next.Key))
							continue;

						if (next.Value > remaining)
							continue;

						TreePart part = new TreePart(smallest);
						Edge edge = new Edge(node, next.Key);

						part.Nodes.Add(next.Key);
						part.Edges.Add(edge);
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

						if (newGroup.Parts.Length == 0) {
							if (newGroup.Size < maxSize) {
								solutions.Clear();
								maxSize = newGroup.Size;
							}

							solutions.Add(newGroup.Smallest);
							//	groups.CapPriority(maxSize);
							continue;
						}
						
						if (newGroup.Size >= maxSize) {
							continue;
						}

						groups.Enqueue(newGroup, newGroup.Size);
					}
				}
                group.Recycle();
			}

			return solutions.ToList();
		}

		public HashSet<ushort> NextStepBetween(ushort a, ushort b)
		{
			return ShortestPathTable[a][b].Item1;
		}
        private static int recycleCount = 0;

		private Dictionary<ushort, SkillTree.SkillNode> Graph;
		private Dictionary<ushort, Dictionary<ushort, Tuple<HashSet<ushort>, int>>> ShortestPathTable;
        private static BitFieldSetFactory<ushort> NodeSetFactory;
        private static BitFieldSetFactory<Edge> EdgeSetFactory;
	}
}
