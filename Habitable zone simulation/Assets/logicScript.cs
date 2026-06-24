// 10 AU = 1 unity unit


using InputActions;
using UnityEngine;
using UnityEngine.UI;

public class logicScript : MonoBehaviour
{
    public Camera cam;
    public float zoomSpeed = 10f;
    public float minZoom = -100f;
    public float maxZoom = 100f;
    public float mouseScrollY;
    InputSystem_Actions defaultControl;

    private void Awake()
    {
        defaultControl = new InputSystem_Actions();
        defaultControl.Player.Scroll.performed += x => mouseScrollY = x.ReadValue<float>();
    } 
    void Update()
    {
        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - mouseScrollY * zoomSpeed, minZoom, maxZoom);
    }

    void OnEnable()
    {
        defaultControl.Enable();
    }
}


