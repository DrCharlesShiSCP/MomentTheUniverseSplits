using UnityEngine;

namespace QuantumBranching
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterControllerDoorPusher : MonoBehaviour
    {
        [SerializeField] private float pushStrength = 1.5f;

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var body = hit.rigidbody;
            if (body == null || body.isKinematic)
            {
                return;
            }

            if (body.GetComponent<HingeJoint>() == null)
            {
                return;
            }

            var pushDirection = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            if (pushDirection.sqrMagnitude < 0.0001f)
            {
                pushDirection = new Vector3(transform.forward.x, 0f, transform.forward.z);
            }

            body.AddForceAtPosition(pushDirection.normalized * pushStrength, hit.point, ForceMode.VelocityChange);
        }
    }
}
