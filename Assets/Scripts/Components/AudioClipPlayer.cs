using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AudioGroup {

    public AudioClip m_clip;
    public float m_volume;

    public void Play (float volume = 1f) {

        AudioClipPlayer.Play(m_clip, m_volume * volume);
    }
}

public class AudioClipPlayer : MonoBehaviour {

	public static List<AudioClipPlayer> pool = new List<AudioClipPlayer>();

    public static float volumeMultiplier = 1f;

	AudioSource audioSource;

	public static void Play (AudioClip clip, float volume) {

		if (pool.Count > 0) {
			pool[pool.Count - 1].Initialize(clip,1,volume);
		} else {
			Instantiate(GameManager.s_gameSettings.audioClipPrefab).GetComponent<AudioClipPlayer>().Initialize(clip, 1f, volume);
		}

	}

	public void Initialize (AudioClip clip, float pitch, float volume) {

		pool.Remove(this);

		DontDestroyOnLoad(gameObject);

		if (audioSource == null) audioSource = GetComponent<AudioSource>();
		audioSource.pitch = pitch;
		audioSource.volume = volume * volumeMultiplier;
		audioSource.clip = clip;
		audioSource.Play();

        StartCoroutine(UpdateForDuration());

    }

    IEnumerator UpdateForDuration () {

        while (audioSource.isPlaying) yield return null;
        pool.Add(this);

	}

}
