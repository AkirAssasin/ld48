using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    // best score, static because I'm lazy
    public static int s_bestScore = 0;

    // public getters
    public int TopCorridorDepth => m_topCorridorDepth;

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
    public int m_maxCorridorsAbove;

    // transition
    [Header("Black Screen")]
    public Transform m_blackScreenTransform;
    public float m_blackScreenDuration;
    public AnimationCurve m_blackScreenCurve;

    // score
    [Header("Score")]
    public Transform m_scoreTextTransform;

    // title
    [Header("Title")]
    public TMPro.TextMeshPro m_titleTextMesh;

    // lose state
    [Header("Lose State")]
    public Transform m_retryPromptTransform;
    public float m_loseTransitionDuration;
    public CameraController m_cameraController;

    // lose state
    bool m_isRetry;
    MCoroutine m_loseCoroutine;

    // score
    TMPro.TextMeshPro m_scoreTextMesh;
    int m_currentBestDepth;

    // black screen progress
    float m_blackScreenProgress;
    float m_blackScreenTrueDuration;
    float m_blackScreenHeight;
    float m_blackScreenBottomY;

    // reference to player actor
    Actor m_playerActor;

    // list of corridors
    List<Corridor> m_corridors;
    int m_topCorridorDepth;

    // list of ambushes
    List<AmbushEnemyInfo> m_ambushes;

    // start call
    void Start () {
        
        // apply game settings
        s_gameSettings = m_gameSettings;

        // create corridor list
        m_corridors = new List<Corridor>();
        m_topCorridorDepth = 0;

        // create ambush list
        m_ambushes = new List<AmbushEnemyInfo>();

        // hide black screen
        m_blackScreenTransform.localScale = Vector3.zero;

        // setup score
        m_scoreTextMesh = m_scoreTextTransform.GetComponent<TMPro.TextMeshPro>();
        m_currentBestDepth = 0;
        UpdateScore();

        // initialize player
        m_player.Initialize(this, 0, m_corridorLength / 2);
        m_playerActor = m_player.GetComponent<Actor>();
    }

    // update call
    void Update () {

        // get delta time
        float dt = Time.deltaTime;

        // update black screen
        if (m_blackScreenProgress > 0f) {

            m_blackScreenProgress -= dt;
            SetBlackScreenPosition();
        }

        // do game update if not retry
        if (m_isRetry) {

            // any key to retry
            if (Input.anyKeyDown && m_loseCoroutine?.running != true) {
                m_loseCoroutine = m_loseCoroutine.StartCoroutine(this, LoseCoroutine());
            }

        } else GameUpdate(dt);
    }

    IEnumerator LoseCoroutine () {

        // wait for black screen, just in case
        while (m_blackScreenProgress > 0f) yield return null;

        // setup black screen
        float blackScreenStart = m_topCorridorDepth * (-1f - s_gameSettings.paddingBetweenCorridors) + 0.5f;
        float totalHeight = m_corridors.Count * (1f + s_gameSettings.paddingBetweenCorridors);

        // run animation
        yield return new RunForDuration(m_loseTransitionDuration, nt => { 
            
            float currentHeight = totalHeight * m_blackScreenCurve.Evaluate(nt);
            m_blackScreenTransform.position = new Vector3(0f, blackScreenStart - currentHeight / 2);
            m_blackScreenTransform.localScale = new Vector3(m_corridorLength, currentHeight, 1f);
        });
        
        SceneManager.LoadScene(0);
    }

    void GameUpdate (float dt) {

        // delete corridors above max
        int requiredTopDepth = m_playerActor.CurrentDepth - m_maxCorridorsAbove;
        if (m_topCorridorDepth < requiredTopDepth) {

            // hide title if top is removed
            if (m_topCorridorDepth == 0) m_titleTextMesh.enabled = false;

            for (int i = 0; i < requiredTopDepth - m_topCorridorDepth; ++i) {
                m_corridors[i].Pool();
            }
            m_corridors.RemoveRange(0, requiredTopDepth - m_topCorridorDepth);
            m_topCorridorDepth = requiredTopDepth;
        }

        // update all ambushes
        for (int i = m_ambushes.Count - 1; i >= 0; --i) {

            // discard if out of depth
            AmbushEnemyInfo ambush = m_ambushes[i];
            if (ambush.m_depth < m_topCorridorDepth) {
                m_ambushes.RemoveAt(i);
                continue;
            }

            // otherwise run normally
            if (ambush.m_active) {

                // if active, countdown until enemy spawn
                if (ambush.m_time > 0f) {
                    
                    ambush.m_time -= dt;
                
                } else if (m_playerActor.CurrentCell != ambush.m_cell) {
                    
                    // spawn enemy and remove ambush when countdown is over
                    EnemyActorController enemy = EnemyActorController.GetFromPool(s_gameSettings.enemyPrefab);
                    enemy.Initialize(this, ambush.m_depth, ambush.m_cell, true);
                    m_ambushes.RemoveAt(i);
                    continue;
                }

            } else if (m_playerActor.CurrentDepth > ambush.m_depth) ambush.m_active = true;
        }

        // update score
        if (m_playerActor.CurrentDepth > m_currentBestDepth) {
            m_currentBestDepth = m_playerActor.CurrentDepth;
            if (m_currentBestDepth > s_bestScore) s_bestScore = m_currentBestDepth;
            UpdateScore();
        }
    }

    // set lose state
    public void SetLoseState () {

        // cannot lose while losing
        if (m_isRetry) return;
        m_isRetry = true;

        m_cameraController.m_positionXStrength = 0f;
        m_retryPromptTransform.position = new Vector3(0f, (-1f - s_gameSettings.paddingBetweenCorridors) * m_playerActor.CurrentDepth);
    }

    // update score
    void UpdateScore () {

        m_scoreTextTransform.position = new Vector3(m_corridorLength * 0.5f, (-1f - s_gameSettings.paddingBetweenCorridors) * m_currentBestDepth);
        m_scoreTextMesh.text = (m_currentBestDepth == 0 ? s_bestScore : m_currentBestDepth).ToString("00");
    }

    // get corridor, or make if it doesn't exist
    public Corridor GetCorridor (int depth) {

        // if the depth doesn't exist, keep making corridors
        while (depth - m_topCorridorDepth >= m_corridors.Count) {

            // add corridor
            AddCorridor(m_topCorridorDepth + m_corridors.Count);
        }

        return m_corridors[depth - m_topCorridorDepth];
    }

    // set black screen position
    void SetBlackScreenPosition () {

        if (m_blackScreenProgress > 0f) {

            // compute current height
            float currentHeight = m_blackScreenHeight * m_blackScreenCurve.Evaluate(m_blackScreenProgress / m_blackScreenTrueDuration);
            Vector2 currentPos = new Vector2(0, m_blackScreenBottomY + currentHeight * 0.5f);
            m_blackScreenTransform.position = currentPos;
            m_blackScreenTransform.localScale = new Vector3(m_corridorLength, currentHeight, 1f);

        } else m_blackScreenTransform.localScale = Vector3.zero;
    }

    // create a new corridor
    void AddCorridor (int depth, int recurse = 0) {

        // compute position
        Vector2 pos = new Vector2((m_corridorLength - 1f) * -0.5f, (-1f - s_gameSettings.paddingBetweenCorridors) * depth);

        // create corridor
        Corridor newCorridor = new Corridor(pos, m_corridorLength);
        m_corridors.Add(newCorridor);

        // populate corridor
        GenerateCorridorContent(newCorridor, depth);

        // check if this a recursed call
        if (recurse > 1) {

            // make holes then continue
            PerforateCorridor(newCorridor, 2);
            AddCorridor(depth + 1, recurse - 1);

        } else {
            
            // height of this thing
            int height = 1; 

            // check if we start recursing
            if (recurse == 0 && depth > 6 && Random.value > 0.5f) {
            
                // make holes then start recursing
                PerforateCorridor(newCorridor, 2);
                height = Random.Range(1,3) + 1;
                AddCorridor(depth + 1, height - 1);
            }

            // set black screen
            m_blackScreenProgress = m_blackScreenTrueDuration = m_blackScreenDuration * height;
            m_blackScreenHeight = (1f + s_gameSettings.paddingBetweenCorridors) * height;
            m_blackScreenBottomY = pos.y + 0.5f + s_gameSettings.paddingBetweenCorridors - m_blackScreenHeight;
            SetBlackScreenPosition();
        }
    }

    // perforate corridor with holes
    void PerforateCorridor (Corridor corridor, int holeCount) {

        List<int> cells = new List<int>();
        for (int i = 0; i < corridor.Length; ++i) cells.Add(i);

        for (int i = 0; i < holeCount; ++i) {

            // choose random cell
            int r = Random.Range(0, cells.Count);

            // add hole without sound
            corridor.MakeHole(cells[r]);

            // remove chosen cell
            cells.RemoveAt(r);
        }
    }

    // populate a corridor (assumed empty)
    void GenerateCorridorContent (Corridor corridor, int depth) {

        // generate list of cells
        List<int> unoccupiedCells = new List<int>();
        for (int i = 0; i < corridor.Length; ++i) unoccupiedCells.Add(i);

        // populate with enemies
        if (depth > 0) {

            // how many enemies to add
            int enemyCount = Mathf.Clamp(1 + depth / 4, 1, 2);
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
