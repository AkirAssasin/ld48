using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// poolable component controlling wall display
public class WallDisplay : PoolableObject<WallDisplay> {

    // self-reference for pooling
    protected override WallDisplay self => this;

    // component references
    Transform m_transform;
    SpriteRenderer m_renderer;

    // initialize function
    public void Initialize (Transform parent, Vector2 position, Sprite sprite) {

        // call poolable initialize
        base.Initialize();

        // get components
        if (m_transform == null) {
            m_transform = GetComponent<Transform>();
            m_renderer = GetComponent<SpriteRenderer>();
        }

        // initialize position
        m_transform.SetParent(parent);
        m_transform.localPosition = position;

        // initialize sprite
        m_renderer.sprite = sprite;
        m_renderer.enabled = true;
    }

    // helper to set wall sprite
    public void SetSprite (Sprite sprite) {

        m_renderer.sprite = sprite;
    }

    // pool function
    public new void Pool () {

        // call poolable pool
        if (!base.Pool()) return;

        // unparent
        m_transform.SetParent(null);

        // disable renderer
        m_renderer.enabled = false;
    }
}
