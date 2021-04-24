using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// interface for a component that controls an actor
public interface IActorController {

    public bool Pool ();
}

// actor component - can be controlled by IActorController
public class Actor : MonoBehaviour {

    // public settings
    [Header("Projectile")]
    public float m_projectileSpread;
    
    // controller
    IActorController m_controller;

    // current facing direction
    bool m_facingRight;

    // game manager
    GameManager m_gameManager;
    int m_currentDepth;

    // corridor position
    Corridor m_currentCorridor;
    int m_currentCell;

    // component references
    Transform m_transform;
    SpriteRenderer m_renderer;

    // initialize function
    public void Initialize (IActorController controller, GameManager gameManager, int depth, int cell) {

        // reference controller
        m_controller = controller;

        // reference game manager
        m_gameManager = gameManager;

        // get components
        if (m_transform == null) {
            m_transform = GetComponent<Transform>();
            m_renderer = GetComponent<SpriteRenderer>();
        }

        // enter corridor
        m_currentDepth = depth;
        EnterCorridor(m_gameManager.GetCorridor(m_currentDepth), cell);
        m_facingRight = true;

        // initialize renderer
        m_renderer.enabled = true;
    }

    // helper to enter corridor
    public void EnterCorridor (Corridor corridor, int cell) {

        // exit from previous corridor
        m_currentCorridor?.m_actors.Remove(this);

        // enter new corridor
        m_currentCorridor = corridor;
        m_transform.SetParent(m_currentCorridor.m_root.transform);
        m_currentCorridor.m_actors.Add(this);

        // update position in corridor
        SetToCell(cell);
    }

    // helper to set position in corridor
    public void SetToCell (int cell) {

        // move corridor and transform position
        m_currentCell = cell;
        m_transform.localPosition = m_currentCorridor.GetCellPosition(m_currentCell);
    }

    // helper to move in corridor
    public void Move (int dx) {

        // check if can move
        int result = m_currentCell + dx;
        if (result < 0 || result >= m_currentCorridor.Length) return;

        // move (no animation yet)
        m_facingRight = (dx > 0);
        SetToCell(result);
    }

    // helper to fire projectile
    public void FireProjectile () {

        // compute angle
        float radian = (m_facingRight ? 0 : Mathf.PI) + (Random.value - 0.5f) * m_projectileSpread * Mathf.Deg2Rad;

        // spawn projectile
        Projectile projectile = Projectile.GetFromPool(GameManager.s_gameSettings.projectilePrefab);
        projectile.Initialize(m_currentCorridor, m_currentCorridor.GetCellPosition(m_currentCell), radian);
    }

    // helper to go deeper
    public void Descend () {

        // make hole in corridor
        m_currentCorridor.MakeHole(m_currentCell);

        // enter next corridor
        EnterCorridor(m_gameManager.GetCorridor(++m_currentDepth), m_currentCell);
    }

    // helper to go up
    public void Ascend () {

        // cannot go up if depth is highest
        if (m_currentDepth == 0) return;

        // can only go up if there is hole
        Corridor topCorridor = m_gameManager.GetCorridor(m_currentDepth - 1);
        if (!topCorridor.m_cells[m_currentCell].m_hasHole) return;

        // ok climb up
        EnterCorridor(topCorridor, m_currentCell);
        --m_currentDepth;
    }

    // pool function
    public void Pool () {

        // call controller pool
        if (!m_controller.Pool()) return;

        // unparent
        m_transform.SetParent(null);

        // disable renderer
        m_renderer.enabled = false;
    }
}
