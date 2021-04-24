using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void ActorAction (Actor actor);

// class representing a single corridor
public class Corridor {

    // getters
    public int Length => m_cells.Length;

    // cells in this corridor
    public Cell[] m_cells;

    // actors in this corridor
    public List<Actor> m_actors;

    // the root gameobject of this corridor
    public GameObject m_root;

    // constructor
    public Corridor (Vector2 position, int length) {

        // create root gameobject
        m_root = new GameObject("Corridor");
        m_root.transform.position = position;

        // create cell array
        m_cells = new Cell[length];
        for (int i = 0; i < Length; ++i) {

            // create each individual cell
            m_cells[i] = new Cell(WallState.Normal, GetCellPosition(i), m_root);
        }

        // create actor list
        m_actors = new List<Actor>();
    }

    // helper to get local position of cell
    public Vector2 GetCellPosition (int cell) {

        // just like this for now
        return new Vector2(cell, 0);
    }

    // helper to make hole
    public void MakeHole (int cell) {

        // make hole in cell
        m_cells[cell].MakeHole();
    }

    // helper to find hole
    public int GetClosestHole (int cell) {

        // check left
        int leftBestHole = cell, leftBestStep = Length;
        for (int i = cell; i >= 0; --i) {
            if (m_cells[i].m_hasHole) {
                leftBestHole = i;
                leftBestStep = cell - i;
                break;
            }
        }

        // check right
        int rightBestHole = cell, rightBestStep = Length;
        for (int i = cell + 1; i < Length; ++i) {
            if (m_cells[i].m_hasHole) {
                rightBestHole = i;
                rightBestStep = i - cell;
                break;
            }
        }

        // choose best
        return (leftBestStep < rightBestStep) ? leftBestHole : rightBestHole;
    }

    // helper to damage actors in cells
    public void DoToActors (Actor actorToIgnore, int minCell, int maxCell, ActorAction action) {

        // check all actors in the corridor
        for (int i = 0; i < m_actors.Count; ++i) {

            // get actor that is not ignored or dead
            Actor actor = m_actors[i];
            if (actor == actorToIgnore) continue;
            if (actor.IsDead) continue;

            // hit the actor
            if (actor.CurrentCell >= minCell && actor.CurrentCell <= maxCell) action(actor);
        }
    }

    // delete root and pool all inside
    public void Pool () {

        // pool all cells' wall displays
        for (int i = 0; i < Length; ++i) m_cells[i].Pool();

        // pool all actors
        for (int i = 0; i < m_actors.Count; ++i) m_actors[i].Pool();

        // destroy root
        GameObject.Destroy(m_root);
    }
}