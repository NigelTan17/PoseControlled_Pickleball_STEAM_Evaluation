using System.Collections.Generic;
using UnityEngine;
using static PB.Scripts.MyBodyTrackGragh;

namespace PB.Scripts
{
    public class Test:MonoBehaviour
    {
        public MyBodyTrackGragh bodyTrackGraph;

        void start()
        {
            if (bodyTrackGraph != null)
            {
                bodyTrackGraph.OnLandmarksProcessed += OnLandmarksProcessed;
            }
        }
        void OnDestroy()
                {
                    if (bodyTrackGraph != null)
                    {
                        bodyTrackGraph.OnLandmarksProcessed -= OnLandmarksProcessed;
                    }
                }
   
        private void OnLandmarksProcessed(List<Vector3> landmarks)
        {
            Debug.Log("Landmarks processed");
            
            //打印landmarks
            foreach (var landmark in landmarks) 
            {
                Debug.Log(landmark);
            }   
        }

     
    }
}