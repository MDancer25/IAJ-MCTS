using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.IAJ.Unity.Pathfinding.Path;
using RAIN.Navigation.NavMesh;
using Assets.Scripts.IAJ.Unity.Utils;
using System;

namespace Assets.Scripts.IAJ.Unity.Pathfinding
{
    public class PathSmoothing
    {
        public static GlobalPath SmoothPath(Vector3 startPosition, Vector3 endPosition, GlobalPath globalPath, NavMeshPathGraph navMesh)
        {
            var currentPosition = startPosition;
            int lookAhead = 1;
            int count = 0;
            Vector3 endPos = endPosition;
            var smoothedPath = new GlobalPath
            {
                IsPartial = globalPath.IsPartial
            };
            if (CheckifHit(startPosition, endPos, navMesh)) {         
                currentPosition = globalPath.PathNodes[0].LocalPosition;
                for (int i = 0; i < globalPath.PathNodes.Count; i++)
                {
                    if(i+lookAhead < globalPath.PathNodes.Count) { 
                        endPos = globalPath.PathNodes[i+lookAhead].LocalPosition;
                    }
                    if (CheckifHit(currentPosition, endPos, navMesh) || count >= 15){
                        currentPosition = globalPath.PathNodes[i].LocalPosition;
                        smoothedPath.PathPositions.Add(currentPosition);
                        count = 0;
                    }
                    count++;
                }
            }
            smoothedPath.PathPositions.Add(endPosition);
            return smoothedPath;
        }
        
        public static bool CheckifHit(Vector3 start, Vector3 end, NavMeshPathGraph navMesh)
        {
            float delta=0.0f;
            for (delta = 0.02f; delta < 1; delta += 0.02f) 
            {
                if(!navMesh.IsPointOnGraph(Vector3.Lerp(start, end, delta), 1)){
                    return true;
                }
            }
            return false;
        }
    }
}