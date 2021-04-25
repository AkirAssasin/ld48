using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Game Settings", menuName = "Scriptable Objects/Game Settings")]
public class GameSettings : ScriptableObject {

    [Header("Corridor")]
    public float paddingBetweenCorridors;

    [Header("Prefabs")]
    public GameObject wallDisplayPrefab;
    public GameObject projectilePrefab;
    public GameObject enemyPrefab;
    public GameObject particlePrefab;
    public GameObject audioClipPrefab;
    public GameObject angelWingPrefab;

    [Header("Wall Sprites")]
    public Sprite wallNormalSprite;
    public Sprite wallDoorSprite;
    public Sprite holeSprite;

    [Header("Actor Sprites")]
    public Sprite actorAimSprite;
    public Sprite actorRecoilSprite;
    public Sprite actorDyingSprite;
    public Sprite actorDeadSprite;
    public Sprite actorFallSprite;
    public Sprite actorReadySprite;
    public Sprite actorFloorBreakSprite;

    [Header("Particle Sprites")]
    public Sprite muzzleFlashParticleSprite;
    public Sprite floorBreakParticleSprite;

    [Header("Sounds")]
    public AudioGroup gunshotSFX;
    public AudioGroup jumpSFX;
    public AudioGroup moveSFX;
    public AudioGroup floorBreakSFX;
    public AudioGroup slapSFX;

    // helper functions
    public Sprite GetWallSprite (WallState state) {

        // return wall sprites based on state
        switch (state) {
            case WallState.Normal: 
                return wallNormalSprite;

            case WallState.Door: 
                return wallDoorSprite;
        }

        // throw if undefined
        throw new System.InvalidOperationException("undefined wall state sprite!");
    }
}
