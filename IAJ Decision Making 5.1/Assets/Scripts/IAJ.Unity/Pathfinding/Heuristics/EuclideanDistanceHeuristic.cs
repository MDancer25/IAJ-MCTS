using System;
using RAIN.Navigation.Graph;

namespace Assets.Scripts.IAJ.Unity.Pathfinding.Heuristics
{
    public class EuclideanDistanceHeuristic : IHeuristic
    {
        public float H(NavigationGraphNode node, NavigationGraphNode goalNode)
        {
            return (goalNode.Position - node.Position).magnitude;
            //return (float)Math.Sqrt((goalNode.Position.x * node.Position.x) - (goalNode.Position.z * node.Position.z));
        }
    }
}
