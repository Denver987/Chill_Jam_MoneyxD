using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class knockback : MonoBehaviour
{
    private Camera cam;
    private bool canHit = true;
    [SerializeField] private float hitCooldown = 0.5f;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && canHit)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                IHitable hitable = hit.transform.GetComponent<IHitable>();
                if (hitable != null)
                {
                    hitable.Execute(transform);
                    StartCoroutine(HitCooldown());
                }
            }
        }
    }

    private IEnumerator HitCooldown()
    {
        canHit = false;
        yield return new WaitForSeconds(hitCooldown);
        canHit = true;
    }
}
