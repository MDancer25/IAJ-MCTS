using Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures.GoalBounding;
using Assets.Scripts.IAJ.Unity.Pathfinding.Heuristics;
using RAIN.Navigation.NavMesh;
using Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures;
using RAIN.Navigation.Graph;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.Pathfinding.GoalBounding
{
    public class GoalBoundingPathfinding : NodeArrayAStarPathfinding
    {
        public GoalBoundingTable GoalBoundingTable { get; protected set; }
        public int DiscardedEdges { get; protected set; }
        public int TotalEdges { get; protected set; }
        public NodeGoalBounds nodeBounds;
        public GoalBoundsDijkstraMapFlooding dikjstra;


        public GoalBoundingPathfinding(NavMeshPathGraph graph, IHeuristic heuristic, GoalBoundingTable goalBoundsTable) : base(graph, heuristic)
        {
            this.GoalBoundingTable = goalBoundsTable;
            Debug.Log(this.GoalBoundingTable);
            dikjstra = new GoalBoundsDijkstraMapFlooding(graph);
        }


        public override void InitializePathfindingSearch(Vector3 startPosition, Vector3 goalPosition)
        {
            this.DiscardedEdges = 0;
            this.TotalEdges = 0;
            base.InitializePathfindingSearch(startPosition, goalPosition);
        }

        protected override void ProcessChildNode(NodeRecord parentNode, NavigationGraphEdge connectionEdge, int edgeIndex)
        {
            TotalEdges++;
            var childNode = connectionEdge.ToNode;
            var childNodeRecord = this.NodeRecordArray.GetNodeRecord(childNode);

            if (this.StartNode.Equals(parentNode.node) || this.StartNode.Equals(childNode))
            {
                base.ProcessChildNode(parentNode, connectionEdge, edgeIndex);
                return;
            }

            if (this.GoalNode.Equals(childNodeRecord.node) || this.GoalNode.Equals(childNode))
            {
                base.ProcessChildNode(parentNode, connectionEdge, edgeIndex);
                return;
            }            

            this.dikjstra.StartNode = parentNode.node;
            
            if (nodeBounds == null)
            {
                //null entries on table are processed as normal A*
                base.ProcessChildNode(parentNode, connectionEdge, edgeIndex);
                return;
            }

            if (edgeIndex < nodeBounds.connectionBounds.Length)
            {
                if (nodeBounds.connectionBounds[edgeIndex].PositionInsideBounds(GoalPosition))
                {
                    base.ProcessChildNode(parentNode, connectionEdge, edgeIndex);
                    return;
                }
            }
            DiscardedEdges++;
        }














            //protected override void ProcessChildNode(NodeRecord parentNode, NavigationGraphEdge connectionEdge, int edgeIndex)
            //{
            //    NodeGoalBounds nodeBounds = this.GoalBoundingTable.table[parentNode.node.NodeIndex];
            //    var childNode = connectionEdge.ToNode;
            //    var childNodeRecord = this.NodeRecordArray.GetNodeRecord(childNode);
            //    this.TotalEdges++;

            //    if (nodeBounds == null)
            //    {
            //        base.ProcessChildNode(parentNode, connectionEdge, edgeIndex);
            //    }
            //    else
            //    {
            //        if (nodeBounds.connectionBounds.Length <= edgeIndex)
            //        {
            //            base.ProcessChildNode(parentNode, connectionEdge, edgeIndex);
            //            return;
            //        }
            //        else if (nodeBounds.connectionBounds[edgeIndex].PositionInsideBounds(this.GoalPosition))
            //        {
            //            base.ProcessChildNode(parentNode, connectionEdge, edgeIndex);
            //            return;
            //        }
            //        this.DiscardedEdges++;
            //    }
            //}
    }
}
