using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    public Transform player; // Mario's Transform
    public Transform endLimit; // GameObject that indicates end of map
    private float offset; // initial x-offset between camera and Mario
    private float startX; // smallest x-coordinate of the Camera
    private float endX; // largest x-coordinate of the camera
    private float viewportHalfWidth;
    public bool followY = false;
    private float yOffset;
    private float startY;
    public float yFollowSpeed = 5f;

    void Start()
    {
        // get coordinate of the bottomleft of the viewport
        // z doesn't matter since the camera is orthographic
        Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)); // the z-component is the distance of the resulting plane from the camera 
        viewportHalfWidth = Mathf.Abs(bottomLeft.x - this.transform.position.x);
        offset = transform.position.x - player.position.x;
        yOffset = transform.position.y - player.position.y;
        startY = transform.position.y;
        startX = transform.position.x;
        endX = endLimit.transform.position.x - viewportHalfWidth;
    }

    void Update()
    {
        float desiredX = player.position.x + offset;
        float desiredY = player.position.y + yOffset;

        float newX = transform.position.x;

        // Restrict horizontal movement to the level.
        if (desiredX >= startX && desiredX <= endX)
            newX = desiredX;

        // Keep the current height unless Mario moves beyond the dead zone to avoid tracking micro movements
        float newY = followY ? transform.position.y : startY;

        if (followY && Mathf.Abs(desiredY - transform.position.y) > 0.05f)
        {
            newY = desiredY;
        }

        transform.position = new Vector3(newX, newY, transform.position.z);
    }

    // reset camera to start
    public void ResetCamera()
    {
        float resetY = followY ? startY : transform.position.y;

        transform.position = new Vector3(
            startX,
            resetY,
            transform.position.z
        );
    }

}
