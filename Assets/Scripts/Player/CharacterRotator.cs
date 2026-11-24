using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterRotator : MonoBehaviour,
    IPointerDownHandler, IDragHandler
{
    [SerializeField]
    private Transform rotationRoot;

    [SerializeField]
    private float rotationSpeed = 0.5f;

    public void OnPointerDown(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        float deltaX = eventData.delta.x;

        rotationRoot.Rotate(
            Vector3.up,
            -deltaX * rotationSpeed,
            Space.World
        );
    }
}