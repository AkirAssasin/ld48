using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyActorController : PoolableObject<EnemyActorController>, IActorController {

    // reference to self
    protected override EnemyActorController self => this;

    // components
    Actor m_actor;

    // initialize function
    public void Initialize (GameManager gameManager, int depth, int cell) {

        // do poolable initialize
        base.Initialize();

        // get and initialize actor
        m_actor = GetComponent<Actor>();
        m_actor.Initialize(this, gameManager, depth, cell);
    }

    // pool function
    public new bool Pool () => base.Pool();
}
