using UnityEngine;
using System.Collections;
using System.Collections.Generic;
     
/// <summary>
/// Controls the behaviour of cars by specifying how they look as they move
/// </summary>
public class CarController : MonoBehaviour {

    public Transform steeringWheel;
    public float steeringTurnSpeed = 18;
    public float rotationRange = 60;
    Quaternion defaultSteeringWheelRotation;

    public List<AxleInfo> axleInfos; 
    public float maxMotorTorque = 1000;
    public float maxSteeringAngle = 60;
    public Vector3 centerOfMass;

    /// <summary>
    /// Setup basics before scene starts
    /// </summary>
    void Start()
    {
        GetComponent<Rigidbody>().centerOfMass = centerOfMass;
        defaultSteeringWheelRotation = steeringWheel.localRotation;
    }

    /// <summary>
    /// On each frame update, move the car to make sure it reflexts the state of the game
    /// </summary>
    private void Update()
    {
        //Rotate wheel based on left or right inputs 
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                steeringWheel.RotateAround(steeringWheel.position, steeringWheel.forward, 60.0f * Time.deltaTime);
            }
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                steeringWheel.RotateAround(steeringWheel.position, steeringWheel.forward, -60.0f * Time.deltaTime);
            }

            //Set max and min rotation of steering wheel
            Vector3 currentRotation = steeringWheel.localRotation.eulerAngles;
            if (currentRotation.z < 180)
            {
                currentRotation.z = Mathf.Clamp(currentRotation.z, 0, rotationRange);
            }
            else
            {
                currentRotation.z = Mathf.Clamp(currentRotation.z, 360 - rotationRange, 360);
            }

            steeringWheel.localRotation = Quaternion.Euler(currentRotation);
        }

        //Rotate wheel back to centre if no inputs being used
        else
        {
            Vector3 currentRotation = steeringWheel.localRotation.eulerAngles;
            if (currentRotation.z < 1 || currentRotation.z > 359)
            {
                //Do nothing
            }
            else if (currentRotation.z < 180)
            {
                steeringWheel.RotateAround(steeringWheel.position, steeringWheel.forward, -60.0f * Time.deltaTime);
            }
            else 
            {
                steeringWheel.RotateAround(steeringWheel.position, steeringWheel.forward, 60.0f * Time.deltaTime);
            }
        }
    }
    
    /// <summary>
    /// On each fixed frame update (in-line with the Physics library) updates wheel positions
    /// </summary>
    public void FixedUpdate()
    {
        //Update wheel positions based on turning and movement
        float motor = maxMotorTorque * Input.GetAxis("Vertical");
        float steering = maxSteeringAngle * Input.GetAxis("Horizontal");
     
        foreach (AxleInfo axleInfo in axleInfos) 
        {
            if (axleInfo.steering) 
            {
                axleInfo.leftWheel.steerAngle = steering;
                axleInfo.rightWheel.steerAngle = steering;
            }
            if (axleInfo.motor) 
            {
                axleInfo.leftWheel.motorTorque = motor;
                axleInfo.rightWheel.motorTorque = motor;
            }
            ApplyLocalPositionToVisuals(axleInfo.leftWheel);
            ApplyLocalPositionToVisuals(axleInfo.rightWheel);
        }
    }

    /// <summary>
    /// Finds the corresponding visual wheel and applies the transform
    /// </summary>
    /// <param name="collider">WheelCollider indicating which wheel to update</param>
    public void ApplyLocalPositionToVisuals(WheelCollider collider)
    {
        if (collider.transform.childCount == 0) 
        {
            return;
        }
     
        Transform visualWheel = collider.transform.GetChild(0);
     
        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);
     
        visualWheel.transform.position = position;
        visualWheel.transform.rotation = rotation;
    }
}
