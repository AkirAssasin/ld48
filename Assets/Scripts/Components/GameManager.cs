using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbushEnemyInfo {

    public int m_depth;
    public int m_cell;
    public float m_time;
    public bool m_active;

    public AmbushEnemyInfo (int depth, int cell, float time) {

        m_depth = depth;
        m_cell = cell;
        m_time = time;
        m_active = false;
    }
}

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

    // corridors
    [Header("Corridors")]
    public int m_corridorLength;
    public float m_ambushMinTime;
    public float m_ambushMaxTime;
    
    // reference to player actor
    Actor m_playerActor;

    // list of corridors
    List<Corridor> m_corridors;

    // list of ambushes
    List<AmbushEnemyInfo> m_ambushes;

    // start call
    void Start () {
        
        // apply game settings
        s_gameSettings = m_gameSettings;

        // create corridor list
        m_corridors = new List<Corridor>();

        // create ambush list
        m_ambushes = new List<AmbushEnemyInfo>();

        // initialize player
        m_player.Initialize(this, 0, m_corridorLength / 2);
        m_playerActor = m_player.GetComponent<Actor>();
    }

    // update call
    void Update () {

        // get delta time
        float dt = Time.deltaTime;

        // update all ambushes
        for (int i = m_ambushes.Count - 1; i >= 0; --i) {

            // check if ambush is active
            AmbushEnemyInfo ambush = m_ambushes[i];
            if (ambush.m_active) {

                // if active, countdown until enemy spawn
                if (ambush.m_time > 0f) {
                    ambush.m_time -= dt;
                } else {
                    
                    // spawn enemy and remove ambush when countdown is over
                    EnemyActorController enemy = EnemyActorController.GetFromPool(s_gameSettings.enemyPrefab);
                    enemy.Initialize(this, ambush.m_depth, ambush.m_cell, true);
                    m_ambushes.RemoveAt(i);
                    continue;
                }

            } else if (m_playerActor.CurrentDepth == ambush.m_depth) ambush.m_active = true;
        }
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

        // generate list of cells
        List<int> unoccupiedCells = new List<int>();
        for (int i = 0; i < corridor.Length; ++i) unoccupiedCells.Add(i);

        // populate with enemies
        if (depth > 0) {

            // how many enemies to add
            int enemyCount = Mathf.Clamp(1 + depth / 4, 1, 3);
            for (int i = 0; i < enemyCount; ++i) {

                // choose random cell
                int r = Random.Range(0, unoccupiedCells.Count);

                // add enemy
                EnemyActorController enemy = EnemyActorController.GetFromPool(s_gameSettings.enemyPrefab);
                enemy.Initialize(this, depth, unoccupiedCells[r], false);

                // remove chosen cell
                unoccupiedCells.RemoveAt(r);
            }
        }

        // add some doors
        int doorCount = Random.Range(-1, Mathf.Clamp(depth, 0, 3));
        for (int i = 0; i < doorCount; ++i) {

            // choose random cell
            int r = Random.Range(0, unoccupiedCells.Count);
            int rCell = unoccupiedCells[r];

            // set as door
            corridor.SetWallState(WallState.Door, rCell);

            // add ambush here
            m_ambushes.Add(new AmbushEnemyInfo(depth, rCell, Random.Range(m_ambushMinTime, m_ambushMaxTime)));

            // remove chosen cell
            unoccupiedCells.RemoveAt(r);
        }
    }
}
