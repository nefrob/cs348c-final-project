/*
* File:        Projectile Generator
* Author:      Robert Neff
* Date:        11/29/17
* Description: Spawns projectiles to fracture the plane.
*/

using UnityEngine;

public class ProjectileGenerator : MonoBehaviour {
    // Projectile object
    [SerializeField] private GameObject projectile;
    // Handler script
    [SerializeField] private Handler handlerScript;
    [SerializeField] private bool allowSubdivision = true;
    // For movement
    [SerializeField] private float force = 10000f;
    // Audio
    private AudioSource source;

    /* Get relevant elements. */
    void Start() {
        if (handlerScript == null) handlerScript = FindObjectOfType<Handler>();
        source = GetComponent<AudioSource>();
    }

    /* Fire projectile on input. */
    void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            source.Play();

            GameObject sphere = Instantiate(projectile, randomStartPosition(), Quaternion.identity);
            sphere.transform.parent = transform;
            sphere.GetComponent<Projectile>().force = force;
            sphere.GetComponent<Projectile>().canSubdivide = allowSubdivision;
            Rigidbody rb = sphere.GetComponent<Rigidbody>();
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(new Vector3(0, 0, 1) * force, ForceMode.Force);
        }
    }

    /* Function: randomStartPosition
     * -----------------------------
     *  Generate random start position within object bounds.
     */
    private Vector3 randomStartPosition() {
        return new Vector3(Random.Range(Handler.voronoiBounds.min.x, Handler.voronoiBounds.max.x),
            Random.Range(Handler.voronoiBounds.min.z, Handler.voronoiBounds.max.z),
            -50.0f);
    }
}
