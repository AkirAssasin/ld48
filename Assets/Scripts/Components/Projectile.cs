using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// poolable component controlling projectile
public class Projectile : PoolableObject<Projectile> {

    // self-reference for pooling
    protected override Projectile self => this;

    // public settings
    [Header("Movement and Collision")]
    public float m_speed;
    public float m_colliderRadius;

    [Header("Trail")]
    public float m_trailWidth;
    public float m_trailLength;

    // corridor position
    Corridor m_corridor;
    Vector2 m_startingPosition;
    Vector2 m_position;
    int m_currentCell;

    // movement
    bool m_collidedWithCorridor;
    Vector2 m_unitVelocity;

    // trail after collision
    float m_trailLengthLeft;

    // component references
    Transform m_transform;
    SpriteRenderer m_renderer;

    // initialize function
    public void Initialize (Corridor corridor, Vector2 position, float radian) {

        // call poolable initialize
        base.Initialize();

        // get components
        if (m_transform == null) {
            m_transform = GetComponent<Transform>();
            m_renderer = GetComponent<SpriteRenderer>();
        }

        // initialize into corridor
        m_corridor = corridor;
        m_transform.SetParent(m_corridor.m_root.transform);

        // initialize position in corridor
        m_startingPosition = m_position = position;
        m_transform.localPosition = m_position;
        m_currentCell = Mathf.FloorToInt(m_position.x);

        // initialize movement
        m_unitVelocity = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
        m_transform.localEulerAngles = new Vector3(0, 0, radian * Mathf.Rad2Deg);

        // reset collision state
        m_collidedWithCorridor = false;

        // initialize renderer
        m_renderer.enabled = true;
    }

    // update function
    void Update () {

        // skip if pooled
        if (inPool) return;

        // get delta time
        float dt = Time.deltaTime;

        // check if collided
        if (m_collidedWithCorridor) {

            // diminish trail
            m_trailLengthLeft -= m_speed * dt;
            if (m_trailLengthLeft <= 0) {

                // pool if no more trail left
                Pool();
            
            } else {

                // update remaining length
                m_transform.localScale = new Vector3(m_trailLengthLeft, m_trailWidth, 1f);
                m_transform.localPosition = m_position - m_unitVelocity * (m_trailLengthLeft * 0.5f);
            }

        } else {

            // update position
            m_collidedWithCorridor = UpdatePosition(dt);

            // compute length
            float fullLength = Vector2.Distance(m_startingPosition, m_position);
            if (fullLength < m_trailLength) {

                // if the length hasn't reached max
                m_transform.localScale = new Vector3(fullLength, m_trailWidth, 1f);
                m_transform.localPosition = Vector2.Lerp(m_position, m_startingPosition, 0.5f);
                m_trailLengthLeft = fullLength;

            } else {

                // shorten length if reached max
                m_transform.localScale = new Vector3(m_trailLength, m_trailWidth, 1f);
                m_transform.localPosition = m_position - m_unitVelocity * (m_trailLength * 0.5f);
                m_trailLengthLeft = m_trailLength;
            }
        }

        // update position
        bool intersected = UpdatePosition(dt);
    }

    // move with collision against corridor edges
    // returns true if collided
    // idk why I'm not using the built-in collision
    public bool UpdatePosition (float dt) {

        // compute next position
        Vector2 delta = m_unitVelocity * (m_speed * dt);
        Vector2 nextPosition = m_position + delta;
        
        // check for intersections
        float intersectTime = 1f;
        bool intersected = false;

        // check for x-intersections
        if (delta.x != 0) {
            if (nextPosition.x < m_colliderRadius - 0.5f) {

                // intersected with left edge
                intersected = true;
                intersectTime = Mathf.Min(intersectTime, (m_position.x + 0.5f - m_colliderRadius) / delta.x);
        
            } else if (nextPosition.x >= m_corridor.Length - 0.5f - m_colliderRadius) {

                // intersected with right edge
                intersected = true;
                intersectTime = Mathf.Min(intersectTime, (m_corridor.Length - 0.5f - m_colliderRadius - m_position.x) / delta.x);
            }
        }

        // check for y-intersections
        if (delta.y != 0) {
            if (nextPosition.y < m_colliderRadius - 0.5f) {

                // intersected with bottom edge
                intersected = true;
                intersectTime = Mathf.Min(intersectTime, (m_position.y + 0.5f - m_colliderRadius) / delta.y);
        
            } else if (nextPosition.y >= 0.5f - m_colliderRadius) {

                // intersected with top edge
                intersected = true;
                intersectTime = Mathf.Min(intersectTime, (0.5f - m_colliderRadius - m_position.y) / delta.y);
            }
        }

        // if intersected, we don't go all the way
        if (intersected) {
            m_position += delta * intersectTime;
            return true;
        }
        
        // otherwise just go to next position
        m_position = nextPosition;
        return false;
    }

    // pool function
    public new void Pool () {

        // call poolable pool
        if (!base.Pool()) return;

        // unparent
        m_transform.SetParent(null);

        // disable renderer
        m_renderer.enabled = false;
    }
}
