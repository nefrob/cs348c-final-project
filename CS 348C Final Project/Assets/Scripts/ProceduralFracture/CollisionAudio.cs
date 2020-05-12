/*
* File:        Collision Audio
* Author:      Robert Neff
* Date:        11/25/17
* Description: Plays collision noise.
*/

using UnityEngine;

public class CollisionAudio : MonoBehaviour {
    // Source
    private AudioSource source;

    /* Get. */
	void Start () {
        source = GetComponent<AudioSource>();
	}

    /* Play sound on collision with probability and if
     * current source isn't already playing. Want to limit how
     * many sounds there are. Naive solution!!! */
    void OnCollisionEnter(Collision collision) {
        if (!source.isPlaying && Random.value < 0.02f) source.Play();
    }
}
