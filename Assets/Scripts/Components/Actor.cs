using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// interface for a component that controls an actor
public interface IActorController {

    public bool Pool ();
}

// actor component - can be controlled by IActorController
public class Actor : MonoBehaviour {

    // controller
    IActorController m_controller;

    // corridor position
    Corridor m_currentCorridor;
    int m_currentCell;

    // component references
    Transform m_transform;
    SpriteRenderer m_renderer;

    // initialize function
    public void Initialize (IActorController controller, Corridor corridor, int cell) {

        // initialize controller
        m_controller = controller;

        // get components
        if (m_transform == null) {
            m_transform = GetComponent<Transform>();
            m_renderer = GetComponent<SpriteRenderer>();
        }

        // enter corridor
        EnterCorridor(corridor, cell);

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
        m_currentCorridor?.m_actors.Add(this);

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
        SetToCell(result);
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
