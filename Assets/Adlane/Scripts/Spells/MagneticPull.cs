using UnityEngine;
using UnityEngine.InputSystem;

public class MagneticPull : MonoBehaviour
{
    private Camera cam;
    [SerializeField] private float pullStrength = 10f; 
    [SerializeField] private float pullRange = 5f;
    void Start()
    {
        cam = Camera.main;
    }

    public void Pull(InputAction.CallbackContext context)
    {
        if (gameObject.GetComponent<SpellSelector>().currentSpell != SpellSelector.SpellType.MagneticPull) return;
        if (context.performed)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, pullRange))
            {
                if (hit.collider.CompareTag("Pullable"))
                {
                    Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 direction = (hit.point - cam.transform.position).normalized;
                        rb.AddForce(direction * pullStrength, ForceMode.Impulse);
                    }
                }
                
            }
        }
    }

    void Update()
    {
        
    }
}
