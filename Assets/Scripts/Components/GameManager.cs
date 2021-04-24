using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// class managing game
public class GameManager : MonoBehaviour {

    // static game settings reference
    public static GameSettings s_gameSettings;

    // game settings to use
    public GameSettings m_gameSettings;

    // reference to player controller
    public PlayerActorController m_player;

    // the corridor (temp)
    public int t_corridorLength;
    Corridor t_corridor;

    // start call
    void Start () {
        
        // apply game settings
        s_gameSettings = m_gameSettings;

        // create corridor
        t_corridor = new Corridor(t_corridorLength);

        // initialize player
        m_player.Initialize(t_corridor, 0);
    }
}
