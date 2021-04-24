using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// class managing game
public class GameManager : MonoBehaviour {

    // static game settings reference
    public static GameSettings s_gameSettings;

    // game settings to use
    [Header("Settings")]
    public GameSettings m_gameSettings;

    // reference to player controller
    [Header("Player")]
    public PlayerActorController m_player;

    // list of corridors
    [Header("Corridors")]
    public float m_paddingBetweenCorridors;
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
            int currentDepth = m_corridors.Count;
            Vector2 pos = new Vector2(0, (-1f - m_paddingBetweenCorridors) * currentDepth);

            // create corridor
            Corridor newCorridor = new Corridor(pos, m_corridorLength);
            m_corridors.Add(newCorridor);

            // populate with one enemy
            if (currentDepth > 0) {

                EnemyActorController enemy = EnemyActorController.GetFromPool(s_gameSettings.enemyPrefab);
                enemy.Initialize(this, currentDepth, Random.Range(0, m_corridorLength));
            }
        }

        return m_corridors[depth];
    }
}
