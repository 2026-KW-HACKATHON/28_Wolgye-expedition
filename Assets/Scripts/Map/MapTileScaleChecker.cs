using Mapbox.Example.Scripts.Map;
using UnityEngine;

public class MapTileScaleChecker : MonoBehaviour
{
    [SerializeField]
    private MapboxMapBehaviour _mapBehaviour;

    [ContextMenu("Measure Map Meshes")]
    public void Measure()
    {
        if (_mapBehaviour == null ||
            _mapBehaviour.MapboxMap == null)
        {
            Debug.LogError("Map이 아직 준비되지 않았습니다.");
            return;
        }

        Transform mapRoot =
            _mapBehaviour.MapboxMap.UnityContext.MapRoot;

        MeshFilter[] meshes =
            mapRoot.GetComponentsInChildren<MeshFilter>();

        Debug.Log(
            $"===== Map Mesh Measurement =====\n" +
            $"Map Scale = {_mapBehaviour.MapboxMap.MapInformation.Scale}\n" +
            $"Mesh Count = {meshes.Length}"
        );

        for (int i = 0; i < meshes.Length; i++)
        {
            MeshFilter meshFilter = meshes[i];

            if (meshFilter.sharedMesh == null)
                continue;

            Transform t = meshFilter.transform;
            Mesh mesh = meshFilter.sharedMesh;

            Bounds localBounds = mesh.bounds;
            Bounds worldBounds = meshFilter.GetComponent<Renderer>()?.bounds ?? default;

            Debug.Log(
                $"===== Mesh {i} =====\n" +
                $"Name = {t.name}\n" +
                $"Mesh = {mesh.name}\n" +
                $"Vertex Count = {mesh.vertexCount}\n" +
                $"Local Bounds Size = {localBounds.size}\n" +
                $"Local Scale = {t.localScale}\n" +
                $"Lossy Scale = {t.lossyScale}\n" +
                $"World Bounds Size = {worldBounds.size}\n" +
                $"World Position = {t.position}"
            );
        }
    }
}