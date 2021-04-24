using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    // public settings
    [Header("Positioning")]
    public Transform m_followTarget;
    public float m_zPosition;
    public float m_positionLerpRate;

    [Header("Tilting")]
    public float m_maxTilt;
    public float m_tiltMinDistance;
    public float m_tiltMaxDistance;

    // current position
    Vector2 m_position;

    // references
    Transform m_transform;
    Camera m_camera;

    // start call
    void Start () {

        // get references
        m_transform = GetComponent<Transform>();
        m_camera = GetComponent<Camera>();

        // get position
        m_position = m_followTarget.position;
    }

    // update call
    void Update () {
        
        // get delta time
        float dt = Time.deltaTime;

        // get target position
        Vector2 targetPosition = m_followTarget.position;

        // tilt to target
        float delta = targetPosition.x - m_position.x;
        float absDelta = Mathf.Abs(delta);
        float tilt = 0f;
        if (absDelta > m_tiltMinDistance) {

            tilt = m_maxTilt * Mathf.Clamp01(absDelta - m_tiltMinDistance / (m_tiltMaxDistance - m_tiltMinDistance));
            if (delta < 0) tilt = -tilt;
        }

        // lerp to target
        m_position = Vector2.Lerp(m_position, targetPosition, dt * m_positionLerpRate);

        // set transform
        m_transform.position = new Vector3(m_position.x, m_position.y, m_zPosition);
        m_transform.eulerAngles = new Vector3(0, 0, tilt);
    }
}
