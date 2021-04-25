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
            Vector2 pos = new Vector2(0, (-1f - s_gameSettings.paddingBetweenCorridors) * currentDepth);

            // create corridor
            Corridor newCorridor = new Corridor(pos, m_corridorLength);
            m_corridors.Add(newCorridor);
            GenerateCorridorContent(newCorridor, currentDepth);
        }

        return m_corridors[depth];
    }

    // create a new corridor
    void GenerateCorridorContent (Corridor corridor, int depth) {

        // populate with enemies
        if (depth > 0) {

            // generate list of cells
            List<int> unoccupiedCells = new List<int>();
            for (int i = 0; i < corridor.Length; ++i) unoccupiedCells.Add(i);

            // how many enemies to add
            int enemyCount = Mathf.Clamp(1 + depth / 4, 1, 3);
            for (int i = 0; i < enemyCount; ++i) {

                // choose random cell
                int r = Random.Range(0, unoccupiedCells.Count);

                // add enemy
                EnemyActorController enemy = EnemyActorController.GetFromPool(s_gameSettings.enemyPrefab);
                enemy.Initialize(this, depth, unoccupiedCells[r]);

                // remove chosen cell
                unoccupiedCells.RemoveAt(r);
            }
        }
    }
}
