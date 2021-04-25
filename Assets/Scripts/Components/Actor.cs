using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// actor labels
public enum ActorLabel { Enemy, Player };

// interface for a component that controls an actor
public interface IActorController {

    ActorLabel Label { get; }
    public bool Pool ();
}

// actor component - can be controlled by IActorController
public class Actor : MonoBehaviour {

    // public getters
    public int CurrentCell => m_currentCell;
    public int CurrentDepth => m_currentDepth;
    public Corridor CurrentCorridor => m_currentCorridor;
    public bool IsDead => m_isDead;
    public bool FacingRight => m_facingRight;
    public ActorLabel Label => m_controller.Label;
    public Vector2 GlobalPosition => m_transform.position;

    // public settings
    [Header("Appearance")]
    public float m_fadeInDuration;
    public Color m_color;

    [Header("Projectile")]
    public float m_projectileSpread;
    public Vector2 m_gunPosition;
    public float m_gunFireDuration;
    public float m_gunCooldownDuration;
    public float m_fireCameraTilt;

    [Header("Floor Break")]
    public Vector2 m_floorBreakGunPosition;
    public float m_floorBreakGunAngle;

    [Header("Particle")]
    public Vector2 m_muzzleFlashScale;
    public float m_muzzleFlashSpread;
    public float m_muzzleFlashDuration;

    [Header("Offset")]
    public float m_yOffset;

    [Header("Death")]
    public float m_dyingDuration;
    public float m_deathFadeDuration;
    public float m_deathOffsetStrength;

    [Header("Movement")]
    public float m_faceDirectionDuration;
    public float m_moveDuration;
    public float m_descendDuration;
    public float m_descendWithFloorBreakDuration;
    public AnimationCurve m_moveCurve;
    public AnimationCurve m_descendCurve;
    public AnimationCurve m_ascendCurve;

    [Header("SFX")]
    public float m_sfxVolume;
    
    // controller
    IActorController m_controller;

    // current facing direction
    bool m_facingRight;

    // current action
    MCoroutine m_currentAction;
    bool m_isDead;
    float m_gunCooldown;

    // game manager
    GameManager m_gameManager;
    int m_currentDepth;

    // corridor position
    Corridor m_currentCorridor;
    int m_currentCell;

    // immunity
    bool m_immuneToMelee;
    bool m_immuneToProjectile;

    // component references
    Transform m_transform;
    SpriteRenderer m_renderer;

    // initialize function
    public void Initialize (IActorController controller, GameManager gameManager, int depth, int cell, bool doFadeIn) {

        // reference controller
        m_controller = controller;

        // reference game manager
        m_gameManager = gameManager;

        // get components
        if (m_transform == null) {
            m_transform = GetComponent<Transform>();
            m_renderer = GetComponent<SpriteRenderer>();
        }

        // enter corridor
        m_currentDepth = depth;
        EnterCorridor(m_gameManager.GetCorridor(m_currentDepth), cell);

        // reset values
        m_gunCooldown = 0f;
        m_isDead = false;
        m_immuneToMelee = false;
        m_immuneToProjectile = false;

        // face random direction
        m_facingRight = Random.value > 0.5f;
        m_renderer.flipX = !m_facingRight;

        // initialize renderer
        m_renderer.color = doFadeIn ? Color.clear : m_color;
        m_renderer.sprite = doFadeIn ? GameManager.s_gameSettings.actorReadySprite : GameManager.s_gameSettings.actorAimSprite;
        m_renderer.enabled = true;

        // do fade in
        if (doFadeIn) {
            GameManager.s_gameSettings.appearSFX.Play(m_sfxVolume);
            m_currentAction = m_currentAction.StartCoroutine(this, FadeInAction());
        }
    }

    // fade in action
    IEnumerator FadeInAction () {

        // immune to melee and projectile
        m_immuneToMelee = true;
        m_immuneToProjectile = true;

        // fade in
        Color color = m_color;
        yield return new RunForDuration(m_fadeInDuration, nt => {
            
            color.a = Random.value > nt ? 0f : 1f;
            m_renderer.color = color; 

            // un-immune to melee and projectile
            if (nt > 0.7f && m_immuneToMelee) {

                m_immuneToMelee = false;
                m_immuneToProjectile = false;

                // also slap whoever's in the way
                m_currentCorridor.DoToActors(this, m_currentCell, m_currentCell, x => x.HitByMelee(0f));
            }
        });

        // set sprite
        m_renderer.sprite = GameManager.s_gameSettings.actorAimSprite;
    }

    // update call
    void Update () {
        
        // update gun cooldown
        if (m_gunCooldown > 0f) m_gunCooldown -= Time.deltaTime;
    }

    // helper to enter corridor
    public void EnterCorridor (Corridor corridor, int cell) {

        // exit from previous corridor
        m_currentCorridor?.m_actors.Remove(this);

        // enter new corridor
        m_currentCorridor = corridor;
        m_transform.SetParent(m_currentCorridor.m_root.transform);
        m_currentCorridor.m_actors.Add(this);

        // update position in corridor
        SetToCell(cell);
    }

    // helper to set position in corridor
    public void SetToCell (int cell) {

        // move corridor and transform position
        m_currentCell = cell;
        m_transform.localPosition = m_currentCorridor.GetCellPosition(m_currentCell) + new Vector2(0, m_yOffset);
    }

    // face direction action
    IEnumerator FaceDirectionAction (bool faceRight) {

        yield return new RunForDuration(m_faceDirectionDuration, nt => {
        
            // for now we do the meme rotation thing
            m_transform.localEulerAngles = new Vector3(0f, nt * 180f, 0f);
        });

        m_transform.localEulerAngles = Vector3.zero;
        m_facingRight = faceRight;
        m_renderer.flipX = !m_facingRight;
    }

    // move action
    IEnumerator MoveAction (int targetCell) {

        // immune to melee
        m_immuneToMelee = true;

        // get end position
        Vector2 start = m_transform.localPosition;
        Vector2 end = m_currentCorridor.GetCellPosition(targetCell) + new Vector2(0, m_yOffset);
        
        // move
        yield return new RunForDuration(m_moveDuration, nt => {
            
            m_transform.localPosition = Vector2.Lerp(start, end, m_moveCurve.Evaluate(nt));
        });
        SetToCell(targetCell);

        // un-immune
        m_immuneToMelee = false;
    }

    // helper to move in corridor
    public void Move (int dx) {

        // only if no action
        if (m_currentAction?.running == true) return;

        // check if can move
        int result = m_currentCell + dx;
        if (result < 0 || result >= m_currentCorridor.Length) return;

        // if facing wrong direction, turn instead of move
        if (m_facingRight != dx > 0) {
            
            GameManager.s_gameSettings.moveSFX.Play(m_sfxVolume);
            m_currentAction = m_currentAction.StartCoroutine(this, FaceDirectionAction(dx > 0));

        } else {
            
            // kill the guy there
            m_currentCorridor.DoToActors(this, result, result, x => x.HitByMelee(dx));

            // move
            GameManager.s_gameSettings.moveSFX.Play(m_sfxVolume);
            m_currentAction = m_currentAction.StartCoroutine(this, MoveAction(result));
        }
    }

    // projectile fire action
    IEnumerator ProjectileFireAction () {

        // set recoil sprite
        m_renderer.sprite = GameManager.s_gameSettings.actorRecoilSprite;

        // compute angle
        float radian = (m_facingRight ? 0 : Mathf.PI) + (Random.value - 0.5f) * m_projectileSpread * Mathf.Deg2Rad;

        // fire projectile
        FireProjectile(m_gunPosition, radian);

        // wait
        yield return new WaitForSeconds(m_gunFireDuration);

        // set back to aiming sprite
        m_renderer.sprite = GameManager.s_gameSettings.actorAimSprite;
        m_gunCooldown = m_gunCooldownDuration;
    }

    // helper to fire projectile
    void FireProjectile (Vector2 gunPosition, float radian) {

        // play sfx
        GameManager.s_gameSettings.gunshotSFX.Play(m_sfxVolume);

        // tilt camera
        CameraController.s_extraTilt += m_facingRight ? -m_fireCameraTilt : m_fireCameraTilt;

        // update delta
        if (!m_facingRight) gunPosition.x = -gunPosition.x;
        gunPosition.y += m_yOffset;

        // spawn projectile
        Projectile projectile = Projectile.GetFromPool(GameManager.s_gameSettings.projectilePrefab);
        projectile.Initialize(m_currentCorridor, this, m_currentCorridor.GetCellPosition(m_currentCell) + gunPosition, radian);

        // spawn muzzle flashes
        Vector2 globalPos = transform.position;
        globalPos.x += gunPosition.x;
        globalPos.y += gunPosition.y - m_yOffset;
        SpawnMuzzleFlash(globalPos, radian * Mathf.Rad2Deg);
        SpawnMuzzleFlash(globalPos, radian * Mathf.Rad2Deg);
    }

    // helper to spawn muzzle flash
    void SpawnMuzzleFlash (Vector2 position, float angle) {

        Particle p = Particle.GetFromPool(GameManager.s_gameSettings.particlePrefab);
        p.Initialize(GameManager.s_gameSettings.muzzleFlashParticleSprite, position, m_muzzleFlashScale, 
            angle + (Random.value - 0.5f) * m_muzzleFlashSpread, Vector2.zero, m_muzzleFlashDuration);
    }

    // helper to fire projectile
    public void FireProjectile () {

        // only if no action and cooldown
        if (m_currentAction?.running == true || m_gunCooldown > 0f) return;
        m_currentAction = m_currentAction.StartCoroutine(this, ProjectileFireAction());
    }

    // floor break action
    IEnumerator FloorBreakAction () {

        // shoot the floor a few times
        m_renderer.sprite = GameManager.s_gameSettings.actorFloorBreakSprite;
        for (int i = 2; i >= 1; --i) {

            // fire projectile
            float angle = m_floorBreakGunAngle;
            if (!m_facingRight) angle = -180f - angle;
            FireProjectile(m_floorBreakGunPosition, angle * Mathf.Deg2Rad);

            // break the floor with the last shot
            if (i == 1) {

                // make hole in corridor
                m_currentCorridor.MakeHole(m_currentCell);
                
                // play sound
                GameManager.s_gameSettings.floorBreakSFX.Play(m_sfxVolume);

                // spawn particles
                int particleCount = Random.Range(4, 7);
                Vector2 particlePos = m_transform.position;
                Vector2 particleScale = new Vector2(1f / particleCount, GameManager.s_gameSettings.paddingBetweenCorridors * 0.5f);
                particlePos.y -= m_yOffset + 0.5f + GameManager.s_gameSettings.paddingBetweenCorridors * 0.5f;
                particlePos.x += (0.5f / particleCount) - 0.5f;
                for (int k = 0; k < particleCount; ++k) {

                    // random speed
                    Vector2 velocity = new Vector2(0, -1f - Random.value);

                    // spawn particle
                    Particle p = Particle.GetFromPool(GameManager.s_gameSettings.particlePrefab);
                    p.Initialize(GameManager.s_gameSettings.floorBreakParticleSprite, particlePos, particleScale, 
                        0f, velocity, 0.4f);

                    // offset position
                    particlePos.x += 1f / particleCount;
                }
            }

            // reload
            yield return new WaitForSeconds(m_gunFireDuration + m_gunCooldownDuration);
        }

        // reset sprite
        m_renderer.sprite = GameManager.s_gameSettings.actorAimSprite;
    }

    // descend action
    IEnumerator DescendAction (Corridor nextCorridor) {

        // immune to melee and projectile
        m_immuneToMelee = true;
        m_immuneToProjectile = true;

        // set sprite
        m_renderer.sprite = GameManager.s_gameSettings.actorFallSprite;

        // get end position
        Vector2 start = m_transform.localPosition;
        Vector2 end = start;
        end.y -= 1f + GameManager.s_gameSettings.paddingBetweenCorridors;
        
        // descend
        bool dropKicked = false;
        yield return new RunForDuration(m_descendDuration, nt => {
            
            // lerp position
            m_transform.localPosition = Vector2.Lerp(start, end, m_descendCurve.Evaluate(nt));
            
            // do drop kick
            if (!dropKicked && nt > 0.5f) {

                nextCorridor.DoToActors(this, m_currentCell, m_currentCell, x => x.HitByMelee(0f));
                dropKicked = true;
            }
        });

        // play landing sound
        GameManager.s_gameSettings.moveSFX.Play(m_sfxVolume);

        // enter next corridor
        EnterCorridor(nextCorridor, m_currentCell);
        ++m_currentDepth;

        // reset sprite
        m_renderer.sprite = GameManager.s_gameSettings.actorAimSprite;

        // un-immune to melee and projectile
        m_immuneToMelee = false;
        m_immuneToProjectile = false;
    }

    // helper to go deeper
    public void Descend () {

        // only if no action
        if (m_currentAction?.running == true) return;

        // get next corridor
        Corridor nextCorridor = m_gameManager.GetCorridor(m_currentDepth + 1);

        // do we need to break the floor
        if (m_currentCorridor.HasHole(m_currentCell)) {
            m_currentAction = m_currentAction.StartCoroutine(this, DescendAction(nextCorridor));
        } else m_currentAction = m_currentAction.StartCoroutine(this, FloorBreakAction());
        
    }

    // ascend action
    IEnumerator AscendAction (Corridor nextCorridor) {

        // immune to melee and projectile
        m_immuneToMelee = true;
        m_immuneToProjectile = true;

        // set sprite
        m_renderer.sprite = GameManager.s_gameSettings.actorFallSprite;

        // get end position
        Vector2 start = m_transform.localPosition;
        Vector2 end = start;
        end.y += 1f + GameManager.s_gameSettings.paddingBetweenCorridors;
        
        // descend
        bool uppercut = false;
        yield return new RunForDuration(m_descendDuration, nt => {
            
            // lerp position
            m_transform.localPosition = Vector2.LerpUnclamped(start, end, m_ascendCurve.Evaluate(nt));
            
            // do uppercut
            if (!uppercut && nt > 0.5f) {

                nextCorridor.DoToActors(this, m_currentCell, m_currentCell, x => x.HitByMelee(0f));
                uppercut = true;
            }
        });

        // enter next corridor
        EnterCorridor(nextCorridor, m_currentCell);
        --m_currentDepth;

        // reset sprite
        m_renderer.sprite = GameManager.s_gameSettings.actorAimSprite;

        // un-immune to melee and projectile
        m_immuneToMelee = false;
        m_immuneToProjectile = false;
    }

    // helper to go up
    public void Ascend () {

        // only if no action
        if (m_currentAction?.running == true) return;

        // cannot go up if depth is highest
        if (m_currentDepth == m_gameManager.TopCorridorDepth) return;

        // can only go up if there is hole
        Corridor topCorridor = m_gameManager.GetCorridor(m_currentDepth - 1);
        if (!topCorridor.m_cells[m_currentCell].m_hasHole) return;

        // play jump sound
        GameManager.s_gameSettings.jumpSFX.Play(m_sfxVolume);

        // ok climb up
        m_currentAction = m_currentAction.StartCoroutine(this, AscendAction(topCorridor));
    }

    // death action
    IEnumerator DeathAction (float offset) {

        // set dying sprite
        m_renderer.sprite = GameManager.s_gameSettings.actorDyingSprite;

        // offset
        Vector2 pos = m_transform.position;
        yield return new RunForDuration(m_dyingDuration, nt => {

            pos.x += offset * nt * Time.deltaTime;
            m_transform.position = pos;
        });

        // set to dead sprite
        m_renderer.sprite = GameManager.s_gameSettings.actorDeadSprite;

        // fade to nothingness
        Color c = m_color;
        yield return new RunForDuration(m_deathFadeDuration, nt => {
        
            c.a = (Random.value > nt) ? 1f : 0f;
            m_renderer.color = c;
        });

        // pool
        Pool();
    }

    // hit by projectile
    public void HitByProjectile (Projectile projectile) {
        
        // check if immune
        if (m_immuneToProjectile) return;

        // die instantly for now
        Kill(projectile.velocity.x);
    }

    // hit by melee
    public void HitByMelee (float offset) {
        
        // check if immune
        if (m_immuneToMelee) return;

        // die instantly for now
        Kill(offset);
    }

    // people die when they are killed
    public void Kill (float offset) {

        // cannot die if dead
        if (m_isDead) return;
        m_isDead = true;

        // slap
        GameManager.s_gameSettings.slapSFX.Play(m_sfxVolume);

        // force death action
        m_currentAction?.Stop();
        m_transform.localEulerAngles = Vector3.zero;
        m_currentAction = m_currentAction.StartCoroutine(this, DeathAction(offset * m_deathOffsetStrength));
    }

    // pool function
    public void Pool () {

        // call controller pool
        if (!m_controller.Pool()) return;

        // stop action (just in case)
        m_currentAction?.Stop();

        // exit from corridor
        m_transform.SetParent(null);
        m_currentCorridor?.m_actors.Remove(this);

        // disable renderer
        m_renderer.enabled = false;
    }
}
