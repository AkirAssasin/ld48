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

    // list of corridors
    public int m_corridorLength;
    List<Corridor> m_corridors;

    // start call
    void Start () {
        
        // apply game settings
        s_gameSettings = m_gameSettings;

        // create corridor list
        m_corridors = new List<Corridor>();

        // initialize player
        m_player.Initialize(this, 0, m_corridorLength / 2);
    }

    // get corridor, or make if it doesn't exist
    public Corridor GetCorridor (int depth) {

        // if the depth doesn't exist, keep making corridors
        while (depth >= m_corridors.Count) {
            
            // compute position
            Vector2 pos = new Vector2(0, depth * -2f);
            m_corridors.Add(new Corridor(pos, m_corridorLength));
        }
        return m_corridors[depth];
    }
}
