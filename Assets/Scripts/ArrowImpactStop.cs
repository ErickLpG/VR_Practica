using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ArrowImpactStop : MonoBehaviour
{
    [Header("Impact")]
    [SerializeField] private bool parentToHitObject = true;
    [SerializeField] private float despawnSeconds = 4f;

    [Header("World Limits")]
    [SerializeField] private float minYToDespawn = -10f;

    private Rigidbody rb;
    private bool impacted;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Kill plane simple
        if (transform.position.y <= minYToDespawn)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (impacted) return;
        impacted = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (parentToHitObject && collision.transform != null)
        {
            transform.SetParent(collision.transform, true);
        }

        StartCoroutine(DespawnRoutine());
    }

    private IEnumerator DespawnRoutine()
    {
        float t = Mathf.Max(0f, despawnSeconds);
        if (t > 0f) yield return new WaitForSeconds(t);
        Destroy(gameObject);
    }
}
