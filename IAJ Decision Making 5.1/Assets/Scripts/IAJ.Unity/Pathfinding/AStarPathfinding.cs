﻿using Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures;
using Assets.Scripts.IAJ.Unity.Pathfinding.Heuristics;
using Assets.Scripts.IAJ.Unity.Pathfinding.Path;
using RAIN.Navigation.Graph;
using RAIN.Navigation.NavMesh;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.Pathfinding
{
	public class AStarPathfinding
	{
		public NavMeshPathGraph NavMeshGraph { get; protected set; }
		//how many nodes do we process on each call to the search method (this method will be called every frame when there is a pathfinding process active
		public uint NodesPerFrame { get; set; }

		public uint TotalExploredNodes { get; protected set; }
		public int MaxOpenNodes { get; protected set; }
		public float TotalProcessingTime { get; protected set; }
		public bool InProgress { get; protected set; }

		public IOpenSet Open { get; protected set; }
		public IClosedSet Closed { get; protected set; }

		public NavigationGraphNode GoalNode { get; protected set; }
		public NavigationGraphNode StartNode { get; protected set; }
		public Vector3 StartPosition { get; protected set; }
		public Vector3 GoalPosition { get; protected set; }

		//heuristic function
		public IHeuristic Heuristic { get; protected set; }

		public AStarPathfinding(NavMeshPathGraph graph, IOpenSet open, IClosedSet closed, IHeuristic heuristic)
		{
			this.NavMeshGraph = graph;
			this.Open = open;
			this.Closed = closed;
			this.NodesPerFrame = uint.MaxValue; //by default we process all nodes in a single request
			this.InProgress = false;
			this.Heuristic = heuristic;
		}

		public virtual void InitializePathfindingSearch(Vector3 startPosition, Vector3 goalPosition)
		{
			this.StartPosition = startPosition;
			this.GoalPosition = goalPosition;
			this.StartNode = this.Quantize(this.StartPosition);
			this.GoalNode = this.Quantize(this.GoalPosition);

			//if it is not possible to quantize the positions and find the corresponding nodes, then we cannot proceed
			if (this.StartNode == null || this.GoalNode == null) return;

			//I need to do this because in Recast NavMesh graph, the edges of polygons are considered to be nodes and not the connections.
			//Theoretically the Quantize method should then return the appropriate edge, but instead it returns a polygon
			//Therefore, we need to create one explicit connection between the polygon and each edge of the corresponding polygon for the search algorithm to work
			((NavMeshPoly)this.StartNode).AddConnectedPoly(this.StartPosition);
			((NavMeshPoly)this.GoalNode).AddConnectedPoly(this.GoalPosition);

			this.InProgress = true;
			this.TotalExploredNodes = 0;
			this.TotalProcessingTime = 0.0f;
			this.MaxOpenNodes = 0;

			var initialNode = new NodeRecord
			{
				gValue = 0,
				hValue = this.Heuristic.H(this.StartNode, this.GoalNode),
				node = this.StartNode
			};

			initialNode.fValue = AStarPathfinding.F(initialNode);

			this.Open.Initialize(); 
			this.Open.AddToOpen(initialNode);
			this.Closed.Initialize();
		}

		protected virtual void ProcessChildNode(NodeRecord bestNode, NavigationGraphEdge connectionEdge, int edgeIndex)
		{
			//this is where you process a child node 
			var childNode = GenerateChildNodeRecord(bestNode, connectionEdge);
			NodeRecord thisChildOpen = this.Open.SearchInOpen(childNode);
			NodeRecord thisChildClosed = this.Closed.SearchInClosed(childNode);
			bool notInOpen = (thisChildOpen == null);
			bool notInClosed = (thisChildClosed == null);
			if (notInOpen && notInClosed)
			{
				this.Open.AddToOpen(childNode);
				if (this.MaxOpenNodes < this.Open.CountOpen())
				{
					this.MaxOpenNodes = this.Open.CountOpen();
				}
			}
			else if (!notInOpen && thisChildOpen.fValue >= childNode.fValue)
			{
				this.Open.Replace(thisChildOpen, childNode);
			}
			else if (!notInClosed && thisChildClosed.fValue > childNode.fValue)
			{
				this.Closed.RemoveFromClosed(thisChildClosed);
				this.Open.AddToOpen(childNode);
				if (this.MaxOpenNodes < this.Open.CountOpen())
				{
					this.MaxOpenNodes = this.Open.CountOpen();
				}
			}
		}

		public bool Search(out GlobalPath solution, bool returnPartialSolution = false)
		{
			int nodesPerFrame = 0;
			float begin = Time.realtimeSinceStartup;
			NodeRecord bestNode = null;

			while(this.Open.CountOpen() > 0 && nodesPerFrame < this.NodesPerFrame)
			{
				this.TotalExploredNodes++;
				nodesPerFrame++;
				bestNode = this.Open.GetBestAndRemove();

				if (this.GoalNode == bestNode.node)
				{
					solution = this.CalculateSolution(bestNode, returnPartialSolution);
					this.InProgress = false;
					return true;
				}

				this.Closed.AddToClosed(bestNode);
				//to determine the connections of the selected nodeRecord you need to look at the NavigationGraphNode' EdgeOut  list
				//something like this
				var outConnections = bestNode.node.OutEdgeCount;
				for (int i = 0; i < outConnections; i++)
				{
					this.ProcessChildNode(bestNode, bestNode.node.EdgeOut(i),i);
					var countOpen = this.Open.CountOpen();
					if(countOpen > this.MaxOpenNodes)
					{
						this.MaxOpenNodes = countOpen;
					}
				}
			}
			this.TotalProcessingTime += Time.realtimeSinceStartup - begin;
			solution = null;

			if(this.Open.CountOpen() == 0)
			{
				return true;
			}

			if(returnPartialSolution && bestNode != null)
			{
				solution = CalculateSolution(bestNode, returnPartialSolution);
			}
			return false;

		}

		protected NavigationGraphNode Quantize(Vector3 position)
		{
			return this.NavMeshGraph.QuantizeToNode(position, 1.0f);
		}

		protected void CleanUp()
		{
			//I need to remove the connections created in the initialization process
			if (this.StartNode != null)
			{
				((NavMeshPoly)this.StartNode).RemoveConnectedPoly();
			}

			if (this.GoalNode != null)
			{
				((NavMeshPoly)this.GoalNode).RemoveConnectedPoly();    
			}
		}

		protected virtual NodeRecord GenerateChildNodeRecord(NodeRecord parent, NavigationGraphEdge connectionEdge)
		{
			var childNode = connectionEdge.ToNode;
			var childNodeRecord = new NodeRecord
			{
				node = childNode,
				parent = parent,
				gValue = parent.gValue + (childNode.LocalPosition-parent.node.LocalPosition).magnitude,
				hValue = this.Heuristic.H(childNode, this.GoalNode)
			};

			childNodeRecord.fValue = F(childNodeRecord);

			return childNodeRecord;
		}

		protected GlobalPath CalculateSolution(NodeRecord node, bool partial)
		{
			var path = new GlobalPath
			{
				IsPartial = partial,
				Length = node.gValue
			};
			var currentNode = node;

			path.PathPositions.Add(this.GoalPosition);

			//I need to remove the first Node and the last Node because they correspond to the dummy first and last Polygons that were created by the initialization.
			//And for instance I don't want to be forced to go to the center of the initial polygon before starting to move towards my destination.

			//skip the last node, but only if the solution is not partial (if the solution is partial, the last node does not correspond to the dummy goal polygon)
			if (!partial && currentNode.parent != null)
			{
				currentNode = currentNode.parent;
			}

			while (currentNode.parent != null)
			{
				path.PathNodes.Add(currentNode.node); //we need to reverse the list because this operator add elements to the end of the list
				path.PathPositions.Add(currentNode.node.LocalPosition);

				if (currentNode.parent.parent == null) break; //this skips the first node
				currentNode = currentNode.parent;
			}

			path.PathNodes.Reverse();
			path.PathPositions.Reverse();
			return path;

		}

		public static float F(NodeRecord node)
		{
			return F(node.gValue,node.hValue);
		}

		public static float F(float g, float h)
		{
			return g + h;
		}
	}
}
