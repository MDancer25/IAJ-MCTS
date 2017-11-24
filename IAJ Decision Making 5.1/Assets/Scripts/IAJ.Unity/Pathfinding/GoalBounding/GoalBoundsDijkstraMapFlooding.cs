using Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures;
using Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures.GoalBounding;
using RAIN.Navigation.Graph;
using RAIN.Navigation.NavMesh;
using System.Collections.Generic;
using System.Diagnostics;

namespace Assets.Scripts.IAJ.Unity.Pathfinding.GoalBounding
{
    //The Dijkstra algorithm is similar to the A* but with a couple of differences
    //1) no heuristic function
    //2) it will not stop until the open list is empty
    //3) we dont need to execute the algorithm in multiple steps (because it will be executed offline)
    //4) we don't need to return any path (partial or complete)
    //5) we don't need to do anything when a node is already in closed
    public class GoalBoundsDijkstraMapFlooding
    {
        public NavMeshPathGraph NavMeshGraph { get; protected set; }
        public NavigationGraphNode StartNode { get; set; }
        public NodeGoalBounds NodeGoalBounds { get; protected set; }
        protected NodeRecordArray NodeRecordArray { get; set; }

        public IOpenSet Open { get; protected set; }
        public IClosedSet Closed { get; protected set; }

        public GoalBoundsDijkstraMapFlooding(NavMeshPathGraph graph)
        {
            this.NavMeshGraph = graph;
            //do not change this
            var nodes = this.GetNodesHack(graph);
            this.NodeRecordArray = new NodeRecordArray(nodes);
            this.Open = this.NodeRecordArray;
            this.Closed = this.NodeRecordArray;

            this.Open.Initialize();
            this.Closed.Initialize();
        }

        public void Search(NavigationGraphNode startNode, NodeGoalBounds nodeGoalBounds)
        {
            this.Open.Initialize();
            this.Closed.Initialize();

            StartNode = startNode;
            var startNodeRecord = this.NodeRecordArray.GetNodeRecord(startNode);
            startNodeRecord.gValue = 0;
            startNodeRecord.hValue = 0;
            startNodeRecord.fValue = F(startNodeRecord);
            Closed.AddToClosed(startNodeRecord);

            var outConnectionsStart = startNodeRecord.node.OutEdgeCount;

            for (int i = 0; i < outConnectionsStart; i++)
            {
                ProcessChildNode(startNodeRecord, startNodeRecord.node.EdgeOut(i), i);

                NavigationGraphNode childNode = startNodeRecord.node.EdgeOut(i).ToNode;
                var childNodeRecord = this.NodeRecordArray.GetNodeRecord(childNode);
                nodeGoalBounds.connectionBounds[i].InitializeBounds(childNodeRecord.node.LocalPosition);
            }

            while (Open.CountOpen() > 0)
            {
                var bestNode = Open.GetBestAndRemove();
                this.Closed.AddToClosed(bestNode);
                //UnityEngine.Debug.Log("Parent index: " + bestNode.StartNodeOutConnectionIndex);
                nodeGoalBounds.connectionBounds[bestNode.StartNodeOutConnectionIndex].UpdateBounds(bestNode.node.LocalPosition);
                var outConnections = bestNode.node.OutEdgeCount;
                for (int i = 0; i < outConnections; i++)
                {
                    ProcessChildNode(bestNode, bestNode.node.EdgeOut(i), bestNode.StartNodeOutConnectionIndex);
                }

            }
        }

        protected void ProcessChildNode(NodeRecord parent, NavigationGraphEdge connectionEdge, int connectionIndex)
        {
            NavigationGraphNode childNode = connectionEdge.ToNode;
            var childNodeRecord = this.NodeRecordArray.GetNodeRecord(childNode);

            //UnityEngine.Debug.Log("connectionIndex: "+ connectionIndex);
            var open = Open.SearchInOpen(childNodeRecord);
            var close = Closed.SearchInClosed(childNodeRecord);
            if (open == null && close == null)
            {
                float g = parent.gValue + (childNodeRecord.node.LocalPosition - parent.node.LocalPosition).magnitude;

                UpdateNode(parent, childNodeRecord, g, 0, g, connectionIndex);
                //UnityEngine.Debug.Log("Morri por dentro: "+ childNodeRecord.StartNodeOutConnectionIndex);
                Open.AddToOpen(childNodeRecord);
            }
            else if (open != null)
            {
                //UnityEngine.Debug.Log("Nunca entro aqui.");
                var g = parent.gValue + (childNodeRecord.node.LocalPosition - parent.node.LocalPosition).magnitude;

                if (g < childNodeRecord.gValue)
                {
                    UpdateNode(parent, childNodeRecord, g, 0, g, connectionIndex);
                    Open.Replace(childNodeRecord, childNodeRecord);
                }
            }
        }

        protected void UpdateNode(NodeRecord bestNode, NodeRecord childNode, float g, float h, float f, int index)
        {
            childNode.gValue = g;
            childNode.hValue = h;
            childNode.fValue = f;
            childNode.parent = bestNode;
            childNode.StartNodeOutConnectionIndex = index;
        }
        private float F(NodeRecord node)
        {
            return node.gValue;
        }


        private List<NavigationGraphNode> GetNodesHack(NavMeshPathGraph graph)
        {
            //this hack is needed because in order to implement NodeArrayA* you need to have full acess to all the nodes in the navigation graph in the beginning of the search
            //unfortunately in RAINNavigationGraph class the field which contains the full List of Nodes is private
            //I cannot change the field to public, however there is a trick in C#. If you know the name of the field, you can access it using reflection (even if it is private)
            //using reflection is not very efficient, but it is ok because this is only called once in the creation of the class
            //by the way, NavMeshPathGraph is a derived class from RAINNavigationGraph class and the _pathNodes field is defined in the base class,
            //that's why we're using the type of the base class in the reflection call
            return (List<NavigationGraphNode>)Utils.Reflection.GetInstanceField(typeof(RAINNavigationGraph), graph, "_pathNodes");
        }

    }
}
