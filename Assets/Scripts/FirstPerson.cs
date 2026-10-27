using UnityEngine;

namespace HINENI
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPerson : MonoBehaviour
    {
        public Camera view;
        public float sensitivity = 1.6f;
        public bool invertY;
        public bool CanMove { get; set; }
        public Interactable Focus { get; private set; }
        CharacterController body;
        float pitch, gravity, stepDistance;
        Vector3 lastPosition;

        void Awake() { body = GetComponent<CharacterController>(); lastPosition = transform.position; }
        void Update()
        {
            Focus = null;
            if (!CanMove) return;
            float mx = Controls.Axis("Mouse X") * sensitivity;
            float my = Controls.Axis("Mouse Y") * sensitivity * (invertY ? -1f : 1f);
            transform.Rotate(0, mx, 0);
            pitch = Mathf.Clamp(pitch - my, -84, 75);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector3 motion = transform.right * Controls.Axis("Horizontal") + transform.forward * Controls.Axis("Vertical");
            motion = Vector3.ClampMagnitude(motion, 1) * 2.05f;
            gravity = body.isGrounded ? -2f : Mathf.Max(-15, gravity - 20 * Time.deltaTime);
            body.Move((motion + Vector3.up * gravity) * Time.deltaTime);
            stepDistance += Vector3.Distance(lastPosition, transform.position);
            lastPosition = transform.position;
            if (stepDistance > 1.55f && body.isGrounded)
            { stepDistance = 0; DemoGame.Instance?.Footstep(); }
            var hits = Physics.RaycastAll(view.transform.position, view.transform.forward, 3.2f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            if (hits.Length > 0)
            {
                Focus = hits[0].collider.GetComponentInParent<Interactable>();
                // The first box hit may be a prop's whole volume (a bed): look for an interactable resting inside it.
                if (!Focus)
                    for (int i = 1; i < hits.Length && !Focus; i++)
                        if (hits[0].collider.bounds.Contains(hits[i].point)) Focus = hits[i].collider.GetComponentInParent<Interactable>();
            }
        }
        public void Teleport(Vector3 position, float yaw)
        {
            body.enabled = false; transform.position = position;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            pitch = 0; view.transform.localRotation = Quaternion.identity;
            body.enabled = true; lastPosition = position;
        }
    }
}
