using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class MagneticPull : MonoBehaviour
{
    private Camera cam;
    [SerializeField] private float pullStrength = 10f;
    [SerializeField] private float pullRange = 5f;
    [SerializeField] private float telekinesisDuration = 2f;
    [SerializeField] private float telekinesisHeight = 2f;
    [SerializeField] private float snapSpeed = 5f;
    [SerializeField] private float aimTolerance = 0.5f;
    private bool buttonDown;

    void Start()
    {
        cam = Camera.main;
    }
    public void Telekinesis(InputAction.CallbackContext context)
    {
        if (gameObject.GetComponent<SpellSelector>().currentSpell != SpellSelector.SpellType.MagneticPull) return;
        if (context.performed)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, pullRange))
            {
                buttonDown = true;
                if (hit.rigidbody == null) return;
                if (!hit.collider.CompareTag("Pullable")) return;
                hit.rigidbody.useGravity = false;
                hit.rigidbody.AddForce(Vector3.up * telekinesisHeight, ForceMode.Impulse);
                StartCoroutine(ResetGravity(hit.rigidbody));
            }
        }
        
    }

    public void Pull(InputAction.CallbackContext context)
    {
        if (gameObject.GetComponent<SpellSelector>().currentSpell != SpellSelector.SpellType.MagneticPull) return;
        if (context.performed)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.SphereCast(ray, aimTolerance, out RaycastHit hit, pullRange))
            {
                if (hit.rigidbody == null) return;
                if (!hit.collider.CompareTag("Pullable")) return;
                buttonDown = true;
                Vector3 direction = (cam.transform.position - hit.collider.transform.position).normalized;
                hit.rigidbody.AddForce(direction * pullStrength, ForceMode.VelocityChange);
                hit.rigidbody.linearVelocity = Vector3.Lerp(hit.rigidbody.linearVelocity, Vector3.zero, snapSpeed * Time.deltaTime);
            }
        }
        if (buttonDown && context.canceled)
        {
            buttonDown = false;
        }
    }


    private IEnumerator ResetGravity(Rigidbody rb)
    {
        yield return new WaitForSeconds(telekinesisDuration);
        rb.useGravity = true;
    }

    void OnDrawGizmosSelected()
    {
            if (cam != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(cam.transform.position, pullRange);
        }
    }
}
