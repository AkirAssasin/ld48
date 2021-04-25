using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActorController : MonoBehaviour, IActorController {
    
    // actor label
    public ActorLabel Label => ActorLabel.Player;

    // public settings
    [Header("Angel Wings")]
    public AngelWingParameter[] m_angelWingParams;

    [Header("Health")]
    public float m_timeForEachDepth;

    // reference to game manager
    GameManager m_gameManager;

    // components
    Actor m_actor;

    // depth timer
    int m_bestDepth;
    float m_health;

    // angel wings
    AngelWing[] m_angelWings;

    // state
    bool m_isDead;

    // initialize function
    public void Initialize (GameManager gameManager, int depth, int cell) {

        // reference
        m_gameManager = gameManager;

        // get and initialize actor
        m_actor = GetComponent<Actor>();
        m_actor.Initialize(this, m_gameManager, depth, cell, true);

        // spawn angel wing for actor
        m_angelWings = new AngelWing[m_angelWingParams.Length];
        for (int i = 0; i < m_angelWings.Length; ++i) {
            AngelWing angelWing = AngelWing.GetFromPool(GameManager.s_gameSettings.angelWingPrefab);
            angelWing.Initialize(m_actor, m_angelWingParams[i]);
            m_angelWings[i] = angelWing;
        }

        // get actor depth
        m_bestDepth = m_actor.CurrentDepth;
    }

    // update call
    void Update () {

        // if dead
        if (m_isDead) return;

        // controls
        if (Input.GetKey(KeyCode.LeftArrow)) m_actor.Move(-1);
        if (Input.GetKey(KeyCode.RightArrow)) m_actor.Move(1);
        if (Input.GetKey(KeyCode.DownArrow)) m_actor.Descend();
        if (Input.GetKey(KeyCode.UpArrow)) m_actor.Ascend();
        if (Input.GetKey(KeyCode.Space)) m_actor.FireProjectile();

        // decrease health if not in depth zero
        if (m_bestDepth > 0) {

            // decrease health
            m_health -= Time.deltaTime;

            // update angel wings
            float healthScaled = (m_health / m_timeForEachDepth) * m_angelWings.Length;
            for (int i = 0; i < m_angelWings.Length; ++i) {

                m_angelWings[i].SetOscillator((healthScaled - i) * 2f);
            }
        }

        // update depth and restore health
        if (m_bestDepth < m_actor.CurrentDepth) {

            m_health = m_timeForEachDepth;
            m_bestDepth = m_actor.CurrentDepth;
        }

        // die if no health
        if (m_health < 0f) m_actor.Kill(0f);
    }

    // cannot pool player
    public bool Pool () {
        
        // cannot die twice
        if (m_isDead) return false;

        // die
        m_isDead = true;
        m_gameManager.SetLoseState();
        return true;
    }
}
