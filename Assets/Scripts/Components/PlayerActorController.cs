using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerActorController : MonoBehaviour, IActorController {
    
    // actor label
    public ActorLabel Label => ActorLabel.Player;

    // public settings
    [Header("Angel Wings")]
    public AngelWingParameter[] m_angelWingParams;

    // components
    Actor m_actor;

    // initialize function
    public void Initialize (GameManager gameManager, int depth, int cell) {

        // get and initialize actor
        m_actor = GetComponent<Actor>();
        m_actor.Initialize(this, gameManager, depth, cell, false);

        // spawn angel wing for actor
        foreach (AngelWingParameter param in m_angelWingParams) {
            AngelWing angelWing = AngelWing.GetFromPool(GameManager.s_gameSettings.angelWingPrefab);
            angelWing.Initialize(m_actor, param);
        }
    }

    // update call
    void Update () {

        // temporary
        if (Input.GetKey(KeyCode.LeftArrow)) m_actor.Move(-1);
        if (Input.GetKey(KeyCode.RightArrow)) m_actor.Move(1);
        if (Input.GetKey(KeyCode.DownArrow)) m_actor.Descend();
        if (Input.GetKey(KeyCode.UpArrow)) m_actor.Ascend();
        if (Input.GetKey(KeyCode.Space)) m_actor.FireProjectile();
    }

    // cannot pool player
    public bool Pool () {
        
        SceneManager.LoadScene(0);
        return true;
    }
}
