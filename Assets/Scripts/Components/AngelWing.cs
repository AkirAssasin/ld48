using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AngelWingParameter {

    public Vector2 m_anchorPosition;
    public Vector2 m_scale;
    public float m_angle;
    public float m_lerpScale;
}

public class AngelWing : PoolableObject<AngelWing> {

    protected override AngelWing self => this;

    // public settings
    [Header("Movement")]
    public float m_baseLerpRate;
    public float m_destroyParticleLife;

    [Header("Appearance")]
    public Color m_color;

    [Header("Beeping")]
    public float m_minimumBeepInterval;

    // anchoring info
    Vector2 m_anchorPosition;
    Actor m_anchorActor;

    // position
    Vector2 m_position;

    // scale
    Vector2 m_scale;

    // angle
    float m_angle;
    float m_trueAngle;

    // actual lerp speed
    float m_lerpRate;

    // oscillation
    float m_oscillator;
    bool m_oscillateState;
    float m_oscillateProgress;
    float m_timeSinceBeep;

    // component references
    Transform m_transform;
    SpriteRenderer m_renderer;

    // initialize function
    public void Initialize (Actor anchorActor, AngelWingParameter param) {

        // call poolable initialize
        base.Initialize();

        // get components
        if (m_transform == null) {
            m_transform = GetComponent<Transform>();
            m_renderer = GetComponent<SpriteRenderer>();
        }

        // initialize anchor
        m_anchorActor = anchorActor;
        m_anchorPosition = param.m_anchorPosition;

        // initialize scale
        m_scale = param.m_scale;
        m_transform.localScale = new Vector3(m_scale.x, m_scale.y, 1f);

        // initialize position
        m_position = GetFinalPosition();
        m_transform.position = m_position;

        // initialize angle
        m_angle = param.m_angle;
        UpdateAngle();

        // initialize lerp rate
        m_lerpRate = m_baseLerpRate * param.m_lerpScale;

        // initialize sprite
        SetOscillator(1f);
        m_renderer.enabled = true;
    }

    // helper to compute target position
    Vector2 GetFinalPosition () {

        Vector2 result = m_anchorActor.GlobalPosition;
        result.y += m_anchorPosition.y;
        result.x += m_anchorActor.FacingRight ? m_anchorPosition.x : -m_anchorPosition.x;
        return result;
    }

    // helper to update angle
    void UpdateAngle () {

        // based on position, not actor facing
        bool facingRight = m_position.x < m_anchorActor.GlobalPosition.x;
        m_trueAngle = 90f + (facingRight ? m_angle : -m_angle);
        m_transform.localEulerAngles = new Vector3(0, 0, m_trueAngle);
    }

    // update call
    void Update () {

        // skip if pooled
        if (inPool) return;

        // pool if actor dead
        if (m_anchorActor.IsDead) {

            // spawn particles
            if (m_oscillator > 0f) {
                Particle p = Particle.GetFromPool(GameManager.s_gameSettings.particlePrefab);
                p.Initialize(m_renderer.sprite, m_position, m_scale, m_trueAngle, Vector2.zero, m_destroyParticleLife);
            }

            Pool();
            return;
        }

        // get delta time
        float dt = Time.deltaTime;
        
        // update beep
        if (m_timeSinceBeep > 0f) m_timeSinceBeep -= dt;

        // lerp to actor's position
        m_position = Vector2.Lerp(m_position, GetFinalPosition(), dt * m_lerpRate);
        m_transform.position = m_position;

        // update oscillator
        if (m_oscillator > 0f && m_oscillator < 1f) {

            m_oscillateProgress += dt;
            if (m_oscillateProgress > m_oscillator) {

                // oscillate color
                m_oscillateProgress -= m_oscillator;
                m_oscillateState = !m_oscillateState;
                m_renderer.color = m_oscillateState ? m_color : Color.clear;

                // do beeping
                if (m_oscillateState && m_timeSinceBeep <= 0f) {

                    m_timeSinceBeep = m_minimumBeepInterval;
                    GameManager.s_gameSettings.beepSFX.Play();
                }
            }
        }

        // update angle
        UpdateAngle();
    }

    // set oscillator
    public void SetOscillator (float oscillator) {

        m_oscillator = oscillator;
        if (m_oscillator <= 0f) {
            
            m_renderer.color = Color.clear;
            m_oscillateState = false;
        
        } else if (m_oscillator >= 1f) {
            
            m_renderer.color = m_color;
            m_oscillateState = true;
        }
    }

    // pool function
    public new void Pool () {

        // call poolable pool
        if (!base.Pool()) return;

        // disable renderer
        m_renderer.enabled = false;
    }
}
