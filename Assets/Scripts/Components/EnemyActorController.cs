using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyAIState { Restart, Idle, MoveToPlayer, FireAtPlayer };

public class EnemyActorController : PoolableObject<EnemyActorController>, IActorController {

    // actor label
    public ActorLabel Label => ActorLabel.Enemy;

    // reference to self
    protected override EnemyActorController self => this;

    // public settings
    [Header("AI")]
    public float m_reactionSpeed;

    // reference to game manager
    GameManager m_gameManager;

    // reference to player actor
    Actor m_playerActor;

    // state machine
    EnemyAIState m_currentState;
    EnemyAIState m_nextState;
    System.Action m_currentStateInit;
    System.Action m_currentStateUpdate;
    System.Action m_currentStateExit;

    // fake reaction speed
    float m_reactionProcessing;

    // components
    Actor m_actor;

    // initialize function
    public void Initialize (GameManager gameManager, int depth, int cell, bool isAmbush) {

        // do poolable initialize
        base.Initialize();

        // get references
        m_gameManager = gameManager;
        m_playerActor = m_gameManager.m_player.GetComponent<Actor>();

        // get and initialize actor
        m_actor = GetComponent<Actor>();
        m_actor.Initialize(this, gameManager, depth, cell, isAmbush);

        // initialize state
        m_currentState = EnemyAIState.Restart;
        m_nextState = isAmbush ? EnemyAIState.MoveToPlayer : EnemyAIState.Idle;
        m_currentStateInit = null;
        UpdateAIState();
        m_reactionProcessing = 0f;
    }

    // state update function
    void UpdateAIState () {

        // no need to update state if next state is still the same
        if (m_nextState == m_currentState && m_nextState != EnemyAIState.Restart) return;

        // call state end
        m_currentStateExit?.Invoke();

        // switch state
        if (m_nextState != EnemyAIState.Restart) m_currentState = m_nextState;
        m_nextState = m_currentState;
        switch (m_currentState) {

            case EnemyAIState.Idle:
                m_currentStateInit = null;
                m_currentStateUpdate = IdleStateUpdate;
                m_currentStateExit = null;
                break;

            case EnemyAIState.MoveToPlayer:
                m_currentStateInit = MoveToPlayerStateInit;
                m_currentStateUpdate = MoveToPlayerStateUpdate;
                m_currentStateExit = null;
                break;

            case EnemyAIState.FireAtPlayer:
                m_currentStateInit = null;
                m_currentStateUpdate = FireAtPlayerUpdate;
                m_currentStateExit = null;
                break;

            default: throw new System.InvalidOperationException("Undefined enemy AI state!");
        }

        // call state begin
        m_currentStateInit?.Invoke();
        m_reactionProcessing = m_reactionSpeed;
    }

    // update function
    void Update () {

        // don't update if dead
        if (inPool) return;

        // update AI state
        UpdateAIState();

        // update reaction speed
        if (m_reactionProcessing > 0f) {
            m_reactionProcessing -= Time.deltaTime;
        } else m_currentStateUpdate?.Invoke();
    }

    // idle state: update
    void IdleStateUpdate () {

        // check if can see player
        if (!m_playerActor.IsDead && m_playerActor.CurrentDepth == m_actor.CurrentDepth) {

            // if same depth, move to player (for now)
            m_nextState = EnemyAIState.MoveToPlayer;
            return;
        }
    }

    // data members for move to player state
    int moveToPlayerCell;
    int moveToPlayerDepth;
    int moveToPlayerPreviousDepth;
    bool moveToPlayerDescend;

    // move to player state: init
    void MoveToPlayerStateInit () {

        // find hole to player
        FindHoleToPlayer();

        // track current depth
        moveToPlayerPreviousDepth = m_actor.CurrentDepth;
    }

    // move to player state: update
    void MoveToPlayerStateUpdate () {

        // if player dead, idle
        if (m_playerActor.IsDead) {

            m_nextState = EnemyAIState.Idle;
            return;
        }

        // if player or we moved, re-find hole
        if (moveToPlayerPreviousDepth != m_actor.CurrentDepth || 
            moveToPlayerDepth != m_playerActor.CurrentDepth) FindHoleToPlayer();

        // fire at player if we reached the same depth
        if (moveToPlayerDepth == m_actor.CurrentDepth) {
            
            m_nextState = EnemyAIState.FireAtPlayer;
            return;
        }

        // move to player
        if (m_actor.CurrentCell == moveToPlayerCell) {

            // descend or ascend depending
            moveToPlayerPreviousDepth = m_actor.CurrentDepth;
            if (moveToPlayerDescend) {
                
                // go if no friend blocking
                if (!FriendlyInPosition(m_gameManager.GetCorridor(m_actor.CurrentDepth + 1), m_actor.CurrentCell)) {
                    m_actor.Descend(); 
                }

            } else {

                // go if no friend blocking
                if (!FriendlyInPosition(m_gameManager.GetCorridor(m_actor.CurrentDepth - 1), m_actor.CurrentCell)) {
                    m_actor.Ascend(); 
                }
            }
        
        } else {
            
            // decide which direction to go
            int dir = m_actor.CurrentCell < moveToPlayerCell ? 1 : -1;

            // go if no friend blocking
            if (!FriendlyInPosition(m_actor.CurrentCorridor, m_actor.CurrentCell + dir)) {
                m_actor.Move(dir);
            }
        }
    }

    // helper to check friendly melee
    bool FriendlyInPosition (Corridor corridor, int targetCell) {

        // check all actors in corridor
        for (int i = 0; i < corridor.m_actors.Count; ++i) {

            // ignore self or dead
            Actor other = corridor.m_actors[i];
            if (other == m_actor || other.IsDead) continue;

            // check position
            if (other.Label == ActorLabel.Enemy && other.CurrentCell == targetCell) return true;
        }

        // no friend in position
        return false;
    }

    // helper function to "pathfind"
    void FindHoleToPlayer () {

        // see if we need to go up or down
        moveToPlayerDepth = m_playerActor.CurrentDepth;
        if (moveToPlayerDepth < m_actor.CurrentDepth) {

            // we need to go up - find hole in upper corridor
            Corridor topCorridor = m_gameManager.GetCorridor(m_actor.CurrentDepth - 1);
            moveToPlayerCell = topCorridor.GetClosestHole(m_actor.CurrentCell);
            moveToPlayerDescend = false;
        
        } else if (moveToPlayerDepth > m_actor.CurrentDepth) {

            // we need to go down - find hole in current corridor
            Corridor currentCorridor = m_gameManager.GetCorridor(m_actor.CurrentDepth);
            moveToPlayerCell = currentCorridor.GetClosestHole(m_actor.CurrentCell);
            moveToPlayerDescend = true;
        }
    }

    // fire at player: update
    void FireAtPlayerUpdate () {

        // if player dead, idle
        if (m_playerActor.IsDead) {

            m_nextState = EnemyAIState.Idle;
            return;
        }

        // if player moved to different depth, follow
        if (m_actor.CurrentDepth != m_playerActor.CurrentDepth) {

            m_nextState = EnemyAIState.MoveToPlayer;
            return;
        }

        // if standing on same spot as player, move out
        if (m_actor.CurrentCell == m_playerActor.CurrentCell) {

            if (m_actor.CurrentCell == 0) {
                m_actor.Move(1);
            } else if (m_actor.CurrentCell == m_actor.CurrentCorridor.Length - 1) {
                m_actor.Move(-1);
            } else m_actor.Move(m_actor.FacingRight ? 1 : -1);

        } else {

            // check if we are facing the right direction
            bool shouldFaceRight = m_playerActor.CurrentCell > m_actor.CurrentCell;
            if (shouldFaceRight != m_actor.FacingRight) {

                // turn to player
                m_actor.Move(shouldFaceRight ? 1 : -1);
        
            } else if (!FriendlyInLineOfFire()) {

                // fire at player
                m_actor.FireProjectile();
            }
        }
    }

    // helper to check friendly fire
    bool FriendlyInLineOfFire () {

        // check all actors in corridor
        for (int i = 0; i < m_actor.CurrentCorridor.m_actors.Count; ++i) {

            // ignore self or dead
            Actor other = m_actor.CurrentCorridor.m_actors[i];
            if (other == m_actor || other.IsDead) continue;

            // check line of fire
            bool inLineOfFire = ((other.CurrentCell > m_actor.CurrentCell) == m_actor.FacingRight);
            if (inLineOfFire && other.Label == ActorLabel.Enemy) return true;
        }

        // no friendly fire
        return false;
    }

    // pool function
    public new bool Pool () => base.Pool();
}
