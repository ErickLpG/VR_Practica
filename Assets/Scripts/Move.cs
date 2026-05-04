using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class Move : MonoBehaviour
{
    #region Variables

    [Header("Movimiento")]
    public float speed;
    public float runMultiplier;
    public float gravity = -9.81f;
    public float jumpHeight;
    public float rotationSpeed;

    private CharacterController characterController;
    private Vector3 velocity;
    private bool isRunning = false;
    private bool isGrounded;
    private PlayerInputAction inputActions;
    private Vector2 moveInput;
    private float rotateInput;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputActions = new PlayerInputAction();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Run.performed += ctx => isRunning = true;
        inputActions.Player.Run.canceled += ctx => isRunning = false;

        inputActions.Player.Jump.performed += ctx => Jump();

        inputActions.Player.Rotate.performed += ctx => rotateInput = ctx.ReadValue<float>();
        inputActions.Player.Rotate.canceled += ctx => rotateInput = 0f;

        inputActions.Player.Reset.performed += ctx => ReiniciarEscena();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        isGrounded = characterController.isGrounded;

        if(isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        move = transform.TransformDirection(move);

        float currentSpeed = isRunning ? speed * runMultiplier : speed;
        characterController.Move(move * currentSpeed * Time.deltaTime);

        float rotation = rotateInput * rotationSpeed * Time.deltaTime;
        transform.Rotate(0, rotation, 0);

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    #endregion

    #region Move Methods

    private void Jump()
    {
        if(isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    public void ReiniciarEscena()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    #endregion
}