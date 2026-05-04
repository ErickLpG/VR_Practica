using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;

using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

/// <summary>
/// Controlador de arco en VR.
/// </summary>
public class VRBow : MonoBehaviour
{
    #region Referencias

    [Header("Hand Anchors")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    [Header("Bow Attach")]
    [SerializeField] private Vector3 localPosInHand = Vector3.zero;
    [SerializeField] private Vector3 localRotEulerInHand = Vector3.zero;

    [Header("Spawn")]
    [SerializeField] private Transform arrowSpawn;
    [SerializeField] private GameObject arrowPrefab;

    #endregion

    #region Configuración de disparo

    [Header("Shot")]
    [SerializeField] private float arrowSpeed = 25f;
    [SerializeField] private float shotCooldown = 0.25f;

    [Header("Draw Settings")]
    [SerializeField] private float maxDrawDistance = 0.5f;
    [SerializeField] private float minDrawToShoot = 0.1f;

    #endregion

    #region XR Input

    [Header("XR Input")]
    [SerializeField] private XRNode leftNode = XRNode.LeftHand;
    [SerializeField] private XRNode rightNode = XRNode.RightHand;
    [SerializeField] private bool usePrimaryButtonToSwap = true;

    #endregion

    #region Editor fallback

    //[Header("Editor Input Fallback")]
    //[SerializeField] private KeyCode editorSwapKey = KeyCode.Tab;
    //[SerializeField] private int editorFireMouseButton = 0;

    #endregion

    #region Movimiento (debug)

    [Header("Rig")]
    [SerializeField] private Transform xrOrigin;

    [Header("Dev Locomotion Mode")]
    [SerializeField] private bool allowDevLocomotionToggle = true;
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float yawDegreesPerSecond = 120f;

    #endregion

    #region Debug

    [Header("Debug")]
    [SerializeField] private bool logWhenFiring = false;

    #endregion

    #region Variables internas

    private bool bowInRightHand = true;
    private float nextShotTime;

    private XRInputDevice leftDevice;
    private XRInputDevice rightDevice;

    private bool prevSwapLeft;
    private bool prevSwapRight;

    private bool devLocomotionEnabled = false;
    private bool prevLeftStickClick;

    private bool isDrawing = false;
    private float currentDraw = 0f;

    #endregion

    #region Unity

    /// <summary>
    /// Inicializa dispositivos XR y posiciona el arco en la mano activa.
    /// </summary>
    private void OnEnable()
    {
        leftDevice = InputDevices.GetDeviceAtXRNode(leftNode);
        rightDevice = InputDevices.GetDeviceAtXRNode(rightNode);
        AttachBowToCurrentHand();
    }

    /// <summary>
    /// Loop principal:
    /// - Actualiza dispositivos
    /// - Maneja input XR o fallback de editor
    /// - Controla locomoción de prueba
    /// </summary>
    private void Update()
    {
        RefreshDevicesIfNeeded();

#if UNITY_EDITOR
        if (!leftDevice.isValid && !rightDevice.isValid)
        {
            HandleEditorInput();
            return;
        }
#endif

        HandleBowInput();
        HandleDevLocomotion();
    }

    #endregion

    #region Input - Editor

    /// <summary>
    /// Maneja entrada en editor cuando no hay dispositivos XR.
    /// Permite disparar con mouse y cambiar de mano con teclado.
    /// </summary>
    private void HandleEditorInput()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame)
                ToggleHand();
        }

        if (Mouse.current != null && Time.time >= nextShotTime)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                nextShotTime = Time.time + shotCooldown;
                ReleaseArrow();
            }
        }
    }

    #endregion

    #region Input - Arco

    /// <summary>
    /// Maneja la lógica principal del arco:
    /// - Cambio de mano
    /// - Detección de tensión
    /// - Liberación de flecha
    /// </summary>
    private void HandleBowInput()
    {
        if (GetSwapPressedDown())
            ToggleHand();

        XRInputDevice drawHand = bowInRightHand ? leftDevice : rightDevice;
        bool triggerHeld  = GetTriggerHeld(drawHand);

        if (triggerHeld)
        {
            UpdateDraw(drawHand);
        }
        else if (isDrawing)
        {
            ReleaseArrow();
            isDrawing = false;
        }
    }

    /// <summary>
    /// Actualiza la cantidad de tensión del arco en función de la distancia entre manos.
    /// </summary>
    private void UpdateDraw(XRInputDevice drawHand)
    {
        isDrawing = true;

        Transform drawTransform = bowInRightHand ? leftHand : rightHand;

        float distance = Vector3.Distance(drawTransform.position, transform.position);
        currentDraw = Mathf.Clamp(distance, 0f, maxDrawDistance);
    }

    #endregion

    #region Disparo

    /// <summary>
    /// Instancia una flecha y aplica velocidad basada en la tensión acumulada.
    /// </summary>
    private void ReleaseArrow()
    {
        if (currentDraw < minDrawToShoot)
            return;

        if (arrowPrefab == null || arrowSpawn == null)
        {
            Debug.LogWarning("[VRBow] Prefab o spawn no asignados.");
            return;
        }

        float normalized = currentDraw / maxDrawDistance;
        float finalSpeed = arrowSpeed * normalized;

        GameObject arrowGO = Instantiate(arrowPrefab, arrowSpawn.position, arrowSpawn.rotation);

        Rigidbody rb = arrowGO.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("[VRBow] La flecha no tiene Rigidbody.");
            return;
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearVelocity = arrowSpawn.forward * finalSpeed;

        currentDraw = 0f;

        if (logWhenFiring)
            Debug.Log("[VRBow] Flecha disparada con fuerza: " + finalSpeed);
    }

    #endregion

    #region Input Helpers

    /// <summary>
    /// Detecta si se presionó el botón para cambiar el arco de mano.
    /// </summary>
    private bool GetSwapPressedDown()
    {
        bool leftSwap = false;
        bool rightSwap = false;

        if (usePrimaryButtonToSwap)
        {
            leftDevice.TryGetFeatureValue(XRCommonUsages.primaryButton, out leftSwap);
            rightDevice.TryGetFeatureValue(XRCommonUsages.primaryButton, out rightSwap);
        }
        else
        {
            leftDevice.TryGetFeatureValue(XRCommonUsages.secondaryButton, out leftSwap);
            rightDevice.TryGetFeatureValue(XRCommonUsages.secondaryButton, out rightSwap);
        }

        bool down = (leftSwap && !prevSwapLeft) || (rightSwap && !prevSwapRight);

        prevSwapLeft = leftSwap;
        prevSwapRight = rightSwap;

        return down;
    }

    /// <summary>
    /// Devuelve true si el usuario está presionando el trgiger trasero en el control.
    /// </summary>
    private bool GetTriggerHeld(XRInputDevice device)
    {
        bool grip;
        device.TryGetFeatureValue(XRCommonUsages.triggerButton, out grip);
        return grip;
    }

    #endregion

    #region Utilidad

    /// <summary>
    /// Reasigna el arco a la mano activa.
    /// </summary>
    private void ToggleHand()
    {
        bowInRightHand = !bowInRightHand;
        AttachBowToCurrentHand();
    }

    /// <summary>
    /// Adjunta el arco al transform de la mano correspondiente.
    /// </summary>
    private void AttachBowToCurrentHand()
    {
        Transform hand = bowInRightHand ? rightHand : leftHand;
        if (hand == null) return;

        transform.SetParent(hand, false);
        transform.localPosition = localPosInHand;
        transform.localRotation = Quaternion.Euler(localRotEulerInHand);
    }

    /// <summary>
    /// Refresca los dispositivos XR si pierden validez.
    /// </summary>
    private void RefreshDevicesIfNeeded()
    {
        if (!leftDevice.isValid) leftDevice = InputDevices.GetDeviceAtXRNode(leftNode);
        if (!rightDevice.isValid) rightDevice = InputDevices.GetDeviceAtXRNode(rightNode);
    }

    #endregion

    #region Movimiento (debug)

    /// <summary>
    /// Permite movimiento en el mundo usando sticks del control para pruebas.
    /// </summary>
    private void HandleDevLocomotion()
    {
        if (!allowDevLocomotionToggle || xrOrigin == null) return;

        bool click;
        leftDevice.TryGetFeatureValue(XRCommonUsages.primary2DAxisClick, out click);

        if (click && !prevLeftStickClick)
            devLocomotionEnabled = !devLocomotionEnabled;

        prevLeftStickClick = click;

        if (!devLocomotionEnabled) return;

        Vector2 move;
        leftDevice.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out move);

        if (move.sqrMagnitude > 0.01f)
        {
            Transform head = Camera.main != null ? Camera.main.transform : xrOrigin;

            Vector3 forward = head.forward;
            Vector3 right = head.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            Vector3 dir = forward * move.y + right * move.x;
            xrOrigin.position += dir * moveSpeed * Time.deltaTime;
        }

        Vector2 turn;
        rightDevice.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out turn);

        if (Mathf.Abs(turn.x) > 0.1f)
        {
            float yaw = turn.x * yawDegreesPerSecond * Time.deltaTime;
            xrOrigin.Rotate(0f, yaw, 0f, Space.World);
        }
    }

    #endregion
}