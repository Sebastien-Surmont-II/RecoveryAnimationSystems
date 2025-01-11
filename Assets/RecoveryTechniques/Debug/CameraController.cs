using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 5f; // Speed of movement
    public float rotationSpeed = 50f; // Speed of rotation

    void Update()
    {
        // Move the camera in the X and Y direction
        float sideways = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrow
        float forward = Input.GetAxis("Vertical");   // W/S or Up/Down Arrow
        bool up = Input.GetKey(KeyCode.Mouse2);
        bool down = Input.GetKey(KeyCode.LeftShift);

        float vertical = 0;
        if (up && down)
            return;
        else if (up)
            vertical = 1;
        else if (down) 
            vertical = -1;

        Vector3 movement = new Vector3(sideways, vertical, forward) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.Self);

        // Rotate the camera using the scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f) // Add a small threshold to avoid accidental rotations
        {
            transform.Rotate(Vector3.up, -scroll * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
