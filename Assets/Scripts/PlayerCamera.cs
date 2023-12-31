using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public float sensX = 2.0f;
    public float sensY = 2.0f;
    public float movementSpeed = 2.0f;

    float xRotation;
    float yRotation;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * sensX * Time.unscaledDeltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sensY * Time.unscaledDeltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);

        float xAxisValue = Input.GetAxisRaw("Horizontal") * movementSpeed * Time.unscaledDeltaTime;
        float yAxisValue = Input.GetAxisRaw("Jump") * movementSpeed * Time.unscaledDeltaTime;
        float zAxisValue = Input.GetAxisRaw("Vertical") * movementSpeed * Time.unscaledDeltaTime;
        transform.Translate(new Vector3(xAxisValue, yAxisValue, zAxisValue));
    }
}
