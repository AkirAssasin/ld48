using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// wall state enum
public enum WallState { Normal, Door };

// class representing a single cell
public class Cell {

    // position
    GameObject m_root;
    Vector2 m_position;

    // wall state
    public WallState m_wallState;
    public bool m_hasHole;

    // wall display
    public WallDisplay m_wallDisplay;

    // hole display
    public WallDisplay m_holeDisplay;

    // constructor
    public Cell (WallState wallState, Vector2 position, GameObject root) {

        // initialize position
        m_position = position;
        m_root = root;

        // initialize wall state and display
        m_wallState = wallState;
        m_wallDisplay = WallDisplay.GetFromPool(GameManager.s_gameSettings.wallDisplayPrefab);
        m_wallDisplay.Initialize(m_root.transform, m_position, GameManager.s_gameSettings.GetWallSprite(m_wallState));

        // start out with no hole
        m_hasHole = false;
        m_holeDisplay = null;
    }

    // helper to set wall state
    public void SetWallState (WallState wallState) {

        m_wallState = wallState;
        m_wallDisplay.SetSprite(GameManager.s_gameSettings.GetWallSprite(m_wallState));
    }

    // helper to make hole
    public void MakeHole () {

        // does nothing if already has hole
        if (m_hasHole) return;

        // initialize hole state and display
        m_hasHole = true;
        m_holeDisplay = WallDisplay.GetFromPool(GameManager.s_gameSettings.wallDisplayPrefab);
        m_holeDisplay.Initialize(m_root.transform, new Vector2(m_position.x, m_position.y - 1), GameManager.s_gameSettings.holeSprite);
    }

    // helper to pool displays
    public void Pool () {

        // pool wall display
        m_wallDisplay.Pool();
        m_wallDisplay = null;

        // pool hole display
        if (m_hasHole) {   
            m_holeDisplay.Pool();
            m_holeDisplay = null;
        }
    }
}