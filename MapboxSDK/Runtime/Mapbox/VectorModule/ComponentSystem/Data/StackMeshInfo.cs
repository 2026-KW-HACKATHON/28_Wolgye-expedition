namespace Mapbox.VectorModule.ComponentSystem.Data
{
    public class StackMeshInfo
    {
        public int TotalPointCount;
        public int TotalTriangleCount;
        public int[] vertexRanges;
        public int[] vertexSize;
        public int[] triRanges;
        public int[] triSize;

        public StackMeshInfo(int featureCount)
        {
            vertexRanges = new int[featureCount];
            vertexSize = new int[featureCount];
            triRanges = new int[featureCount];
            triSize = new int[featureCount];
            TotalPointCount = 0;
        }
    }
}