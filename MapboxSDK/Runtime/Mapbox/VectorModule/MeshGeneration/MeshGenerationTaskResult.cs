using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tasks;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration
{
    public class MeshGenerationTaskResult : TaskResult
    {
        public TaskResultType ResultType;
        public bool InvalidateAndRetry = false;
        public IEnumerable<GameObject> GeneratedObjects;

        public MeshGenerationTaskResult(TaskResultType result, IEnumerable<GameObject> visuals = null)
        {
            ResultType = result;
            GeneratedObjects = visuals;
        }
    }
}