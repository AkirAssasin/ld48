using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Game Settings", menuName = "Scriptable Objects/Game Settings")]
public class GameSettings : ScriptableObject {

    [Header("Prefabs")]
    public GameObject wallDisplayPrefab;
    public GameObject projectilePrefab;
    public GameObject enemyPrefab;

    [Header("Wall Sprites")]
    public Sprite wallNormalSprite; 
    public Sprite holeSprite;
}
