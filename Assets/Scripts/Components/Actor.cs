using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// interface for a component that controls an actor
public interface IActorController {

    public bool Pool ();
}

// actor component - can be controlled by IActorController
public class Actor : MonoBehaviour {

    // public getters
    public int currentCell => m_currentCell;

    // public settings
    [Header("Projectile")]
    public float m_projectileSpread;
    public Vector2 m_gunPosition;

    [Header("Offset")]
    public float m_yOffset;
    
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

        // face random direction
        m_facingRight = Random.value > 0.5f;
        m_renderer.flipX = !m_facingRight;

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
        m_transform.localPosition = m_currentCorridor.GetCellPosition(m_currentCell) + new Vector2(0, m_yOffset);
    }

    // helper to move in corridor
    public void Move (int dx) {

        // check if can move
        int result = m_currentCell + dx;
        if (result < 0 || result >= m_currentCorridor.Length) return;

        // if facing wrong direction, turn instead of move
        if (m_facingRight != dx > 0) {
            m_facingRight = (dx > 0);
            m_renderer.flipX = !m_facingRight;
        } else SetToCell(result);
    }

    // helper to fire projectile
    public void FireProjectile () {

        // compute angle
        float radian = (m_facingRight ? 0 : Mathf.PI) + (Random.value - 0.5f) * m_projectileSpread * Mathf.Deg2Rad;
        
        // compute position
        Vector2 position = m_currentCorridor.GetCellPosition(m_currentCell);
        position.y += m_gunPosition.y + m_yOffset;
        position.x += m_facingRight ? m_gunPosition.x : -m_gunPosition.x;

        // spawn projectile
        Projectile projectile = Projectile.GetFromPool(GameManager.s_gameSettings.projectilePrefab);
        projectile.Initialize(m_currentCorridor, this, position, radian);
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

    // hit by projectile
    public void HitByProjectile (Projectile projectile) {

        // temporary
        Pool();
    }

    // pool function
    public void Pool () {

        // call controller pool
        if (!m_controller.Pool()) return;

        // exit from corridor
        m_transform.SetParent(null);
        m_currentCorridor?.m_actors.Remove(this);

        // disable renderer
        m_renderer.enabled = false;
    }
}
