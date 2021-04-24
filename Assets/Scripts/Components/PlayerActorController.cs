using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActorController : MonoBehaviour, IActorController {
    
    // components
    Actor m_actor;

    // initialize function
    public void Initialize (GameManager gameManager, int depth, int cell) {

        // get and initialize actor
        m_actor = GetComponent<Actor>();
        m_actor.Initialize(this, gameManager, depth, cell);
    }

    // update call
    void Update () {

        // temporary
        if (Input.GetKey(KeyCode.LeftArrow)) m_actor.Move(-1);
        if (Input.GetKey(KeyCode.RightArrow)) m_actor.Move(1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) m_actor.Descend();
        if (Input.GetKeyDown(KeyCode.UpArrow)) m_actor.Ascend();
        if (Input.GetKey(KeyCode.Space)) m_actor.FireProjectile();
    }

    // cannot pool player
    public bool Pool () {
        throw new System.InvalidOperationException("Cannot pool player actor!");
    }
}
