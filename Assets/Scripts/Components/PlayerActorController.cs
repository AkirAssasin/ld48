using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActorController : MonoBehaviour, IActorController {
    
    // components
    Actor m_actor;

    // initialize function
    public void Initialize (Corridor corridor, int cell) {

        // get and initialize actor
        m_actor = GetComponent<Actor>();
        m_actor.Initialize(this, corridor, cell);
    }

    // update call
    void Update () {

        // temporary
        if (Input.GetKeyDown(KeyCode.LeftArrow)) m_actor.Move(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) m_actor.Move(1);
    }

    // cannot pool player
    public bool Pool () {
        throw new System.InvalidOperationException("Cannot pool player actor!");
    }
}
