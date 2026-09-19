using UnityEngine;

namespace Mapbox.Example.Scripts.Map
{
    public class SnapPositionToPosition : MonoBehaviour
    {
        public Transform Target;

        private void Update()
        {
            transform.position = Target.position;
        }
    }
}
