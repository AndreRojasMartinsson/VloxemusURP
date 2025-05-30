using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
	
	[Header("Movement")] public float movementSpeed = 10f;
	public float boostMultiplier = 4f;

	[Header("Look")] public float lookSensitivity = 2f;

	private float _yaw;
	private float _pitch;

	public Vector3 Position => transform.position;

	private void Start()
	{
		var angles = transform.eulerAngles;
		
		_yaw = angles.y;
		_pitch = angles.x;

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	private void Update()
	{
		var mouseX = Input.GetAxisRaw("Mouse X") * lookSensitivity;
		var mouseY = Input.GetAxisRaw("Mouse Y") * lookSensitivity * -1;
		

		_yaw += mouseX;
		_pitch = Mathf.Clamp(_pitch + mouseY, -90f, 90f);


		var inputDirection = new Vector3(
			Input.GetAxisRaw("Horizontal"),
			0f,
			Input.GetAxisRaw("Vertical")
		);

		if (Input.GetKey(KeyCode.E)) inputDirection.y += 1f;
		if (Input.GetKey(KeyCode.Q)) inputDirection.y -= 1f;

		var move = transform.TransformDirection(inputDirection.normalized);

		var speed = movementSpeed;
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) speed *= boostMultiplier;
		if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) speed /= boostMultiplier;
		
		transform.eulerAngles = new Vector3(_pitch, _yaw, 0f);
		transform.position += move * (speed * Time.deltaTime);
		
		if (!Input.GetKeyDown(KeyCode.Escape)) return;

		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}
}
