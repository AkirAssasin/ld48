using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle : PoolableObject<Particle> {

    protected override Particle self => this;

    // particle values
    Vector2 m_position;
    Vector2 m_velocity;
    float m_lifetime;
    float m_life;
    Vector2 m_baseScale;

    // component references
    Transform m_transform;
    SpriteRenderer m_renderer;

    // initialize function
    public void Initialize (Vector2 position, Vector2 scale, float angle, Vector2 velocity, float lifetime) {

        // call poolable initialize
        base.Initialize();

        // get components
        if (m_transform == null) {
            m_transform = GetComponent<Transform>();
            m_renderer = GetComponent<SpriteRenderer>();
        }

        // initialize position
        m_position = position;
        m_transform.position = m_position;
        m_velocity = velocity;

        // initialize life
        m_life = m_lifetime = lifetime;

        // initialize scale
        m_baseScale = scale;
        m_transform.localScale = new Vector3(m_baseScale.x, m_baseScale.y, 1f);

        // initialize angle
        m_transform.localEulerAngles = new Vector3(0, 0, angle);

        // initialize sprite
        m_renderer.enabled = true;
    }

    // update call
    void Update () {

        // skip if pooled
        if (inPool) return;

        // get delta time
        float dt = Time.deltaTime;
        float nt = m_life / m_lifetime;

        // update position
        m_position += m_velocity * dt;
        m_transform.position = m_position;

        // update scale
        m_transform.localScale = new Vector3(m_baseScale.x * nt, m_baseScale.y * nt, 1f);

        // update life
        m_life -= dt;
        if (m_life <= 0f) Pool();
    }

    // pool function
    public new void Pool () {

        // call poolable pool
        if (!base.Pool()) return;

        // disable renderer
        m_renderer.enabled = false;
    }
}
