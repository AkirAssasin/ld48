using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// interface for a component that controls an actor
public interface IActorController {

    public bool Pool ();
}

// actor component - can be controlled by IActorController
public class Actor : MonoBehaviour {

    // public getters
    public int currentCell => m_currentCell;

    // public settings
    [Header("Appearance")]
    public Color m_color;

    [Header("Projectile")]
    public float m_projectileSpread;
    public Vector2 m_gunPosition;
    public float m_gunFireDuration;
    public float m_gunCooldownDuration;

    [Header("Offset")]
    public float m_yOffset;

    [Header("Death")]
    public float m_dyingDuration;
    public float m_deathFadeDuration;

    [Header("Movement")]
    public float m_faceDirectionDuration;
    public float m_moveDuration;
    public AnimationCurve m_moveCurve;
    
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

    // component references
    Transform m_transform;
    SpriteRenderer m_renderer;

    // initialize function
    public void Initialize (IActorController controller, GameManager gameManager, int depth, int cell) {

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

        // not dead anymore
        m_isDead = false;

        // face random direction
        m_facingRight = Random.value > 0.5f;
        m_renderer.flipX = !m_facingRight;

        // initialize renderer
        m_renderer.color = m_color;
        m_renderer.sprite = GameManager.s_gameSettings.actorAimSprite;
        m_renderer.enabled = true;
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

        // get end position
        Vector2 start = m_transform.localPosition;
        Vector2 end = m_currentCorridor.GetCellPosition(targetCell) + new Vector2(0, m_yOffset);
        
        // move
        yield return new RunForDuration(m_moveDuration, nt => {
            
            m_transform.localPosition = Vector2.Lerp(start, end, m_moveCurve.Evaluate(nt));
        });
        SetToCell(targetCell);
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
            
            m_currentAction = m_currentAction.StartCoroutine(this, FaceDirectionAction(dx > 0));

        } else m_currentAction = m_currentAction.StartCoroutine(this, MoveAction(result));
    }

    // projectile fire action
    IEnumerator ProjectileFireAction () {

        // set recoil sprite
        m_renderer.sprite = GameManager.s_gameSettings.actorRecoilSprite;

        // compute angle
        float radian = (m_facingRight ? 0 : Mathf.PI) + (Random.value - 0.5f) * m_projectileSpread * Mathf.Deg2Rad;
        
        // compute position
        Vector2 position = m_currentCorridor.GetCellPosition(m_currentCell);
        position.y += m_gunPosition.y + m_yOffset;
        position.x += m_facingRight ? m_gunPosition.x : -m_gunPosition.x;

        // spawn projectile
        Projectile projectile = Projectile.GetFromPool(GameManager.s_gameSettings.projectilePrefab);
        projectile.Initialize(m_currentCorridor, this, position, radian);

        // wait
        yield return new WaitForSeconds(m_gunFireDuration);

        // set back to aiming sprite
        m_renderer.sprite = GameManager.s_gameSettings.actorAimSprite;
        m_gunCooldown = m_gunCooldownDuration;
    }

    // helper to fire projectile
    public void FireProjectile () {

        // only if no action and cooldown
        if (m_currentAction?.running == true || m_gunCooldown > 0f) return;
        m_currentAction = m_currentAction.StartCoroutine(this, ProjectileFireAction());
    }

    // helper to go deeper
    public void Descend () {

        // only if no action
        if (m_currentAction?.running == true) return;

        // make hole in corridor
        m_currentCorridor.MakeHole(m_currentCell);

        // get next corridor
        Corridor nextCorridor = m_gameManager.GetCorridor(++m_currentDepth);

        // drop-kick the guy below
        for (int i = 0; i < nextCorridor.m_actors.Count; ++i) {

            Actor actor = nextCorridor.m_actors[i];
            if (actor.currentCell == m_currentCell) actor.Kill();
        }

        // enter next corridor
        EnterCorridor(nextCorridor, m_currentCell);
    }

    // helper to go up
    public void Ascend () {

        // only if no action
        if (m_currentAction?.running == true) return;

        // cannot go up if depth is highest
        if (m_currentDepth == 0) return;

        // can only go up if there is hole
        Corridor topCorridor = m_gameManager.GetCorridor(m_currentDepth - 1);
        if (!topCorridor.m_cells[m_currentCell].m_hasHole) return;

        // ok climb up
        EnterCorridor(topCorridor, m_currentCell);
        --m_currentDepth;
    }

    // death action
    IEnumerator DeathAction () {

        // set dying sprite
        m_renderer.sprite = GameManager.s_gameSettings.actorDyingSprite;

        // wait
        yield return new WaitForSeconds(m_dyingDuration);

        // set to dead sprite
        m_renderer.sprite = GameManager.s_gameSettings.actorDeadSprite;

        // fade to nothingness
        Color c = m_color;
        yield return new RunForDuration(m_deathFadeDuration, nt => {
        
            c.a = 1f - nt;
            m_renderer.color = c;
        });

        // pool
        Pool();
    }

    // hit by projectile
    public void HitByProjectile (Projectile projectile) {

        // die instantly for now
        Kill();
    }

    // people die when they are killed
    public void Kill () {

        // cannot die if dead
        if (m_isDead) return;
        m_isDead = true;

        // force death action
        m_currentAction?.Stop();
        m_currentAction = m_currentAction.StartCoroutine(this, DeathAction());
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
