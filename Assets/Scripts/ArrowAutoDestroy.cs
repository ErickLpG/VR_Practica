using UnityEngine;

/// <summary>
/// Destruye la flecha si permanece casi inmóvil por cierto tiempo.
/// </summary>
public class ArrowAutoDestroy : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float tiempoMaxSinMovimiento = 3f;
    [SerializeField] private float velocidadMinima = 0.1f;

    private Rigidbody rb;
    private float tiempoQuieto = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (rb == null) return;

        // Si la velocidad es muy baja
        if (rb.linearVelocity.magnitude < velocidadMinima)
        {
            tiempoQuieto += Time.deltaTime;

            if (tiempoQuieto >= tiempoMaxSinMovimiento)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            // Si se mueve, resetea el contador
            tiempoQuieto = 0f;
        }
    }
}