using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// poolable component controlling projectile
public class Projectile : PoolableObject<Projectile> {

    // public getters
    public Vector2 velocity => m_unitVelocity;

    // self-reference for pooling
    protected override Projectile self => this;

    // public settings
    [Header("Movement and Collision")]
    public float m_speed;

    [Header("Trail")]
    public float m_trailWidth;
    public float m_trailLength;

    [Header("Particle")]
    public Vector2 m_particleScale;
    public float m_particleLife;

    // corridor position
    Corridor m_corridor;
    Vector2 m_startingPosition;
    Vector2 m_position;
    int m_currentCell;

    // movement
    bool m_collidedWithCorridor;
    Vector2 m_unitVelocity;
    float m_angle;

    // trail after collision
    float m_trailLengthLeft;

    // actor to ignore
    Actor m_actorToIgnore;

    // component references
    Transform m_transform;
    SpriteRenderer m_renderer;

    // initialize function
    public void Initialize (Corridor corridor, Actor actorToIgnore, Vector2 position, float radian) {

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
        m_currentCell = Mathf.RoundToInt(m_position.x);

        // initialize movement
        m_unitVelocity = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));
        m_transform.localEulerAngles = new Vector3(0, 0, radian * Mathf.Rad2Deg);
        m_angle = radian * Mathf.Rad2Deg;

        // reset collision state
        m_actorToIgnore = actorToIgnore;
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
            
            // if collided, spawn particle
            if (m_collidedWithCorridor) {

                Vector2 particlePos = m_corridor.m_root.transform.position;
                particlePos += m_position;

                Particle p = Particle.GetFromPool(GameManager.s_gameSettings.particlePrefab);
                p.Initialize(GameManager.s_gameSettings.muzzleFlashParticleSprite, particlePos, m_particleScale, m_angle, m_unitVelocity * -0.2f, m_particleLife);
            }

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

            // compute collision with actors
            int newCell = Mathf.Clamp(Mathf.RoundToInt(m_position.x), 0, m_corridor.Length - 1);
            if (m_currentCell != newCell) {

                int min, max;
                if (m_currentCell < newCell) {
                    min = m_currentCell + 1;
                    max = newCell;
                } else {
                    min = newCell;
                    max = m_currentCell - 1;
                }

                // hit actors in corridor
                m_corridor.DoToActors(m_actorToIgnore, min, max, x => x.HitByProjectile(this));

                // update current cell
                m_currentCell = newCell;
            }
        }
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
            if (nextPosition.x < -0.5f) {

                // intersected with left edge
                intersected = true;
                intersectTime = Mathf.Min(intersectTime, (-0.5f - m_position.x) / delta.x);
        
            } else if (nextPosition.x >= m_corridor.Length - 0.5f) {

                // intersected with right edge
                intersected = true;
                intersectTime = Mathf.Min(intersectTime, (m_corridor.Length - 0.5f - m_position.x) / delta.x);
            }
        }

        // check for y-intersections
        if (delta.y != 0) {
            if (nextPosition.y < -0.5f) {

                // intersected with bottom edge
                intersected = true;
                intersectTime = Mathf.Min(intersectTime, (-0.5f - m_position.y) / delta.y);
        
            } else if (nextPosition.y >= 0.5f) {

                // intersected with top edge
                intersected = true;
                intersectTime = Mathf.Min(intersectTime, (0.5f - m_position.y) / delta.y);
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
