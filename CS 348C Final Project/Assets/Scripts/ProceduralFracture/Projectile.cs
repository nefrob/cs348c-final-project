/*
* File:        Projectile
* Author:      Robert Neff
* Date:        11/29/17
* Description: Projectile to impact with fracture object.
*/

using UnityEngine;

public class Projectile : MonoBehaviour {
    // Fracture object script
    private Handler handler;
    // For movement
    public float force = 10000f;
    private Rigidbody rb;
    // For sub-fracture (limit how many)
    public bool canSubdivide = true;

    /* Get fracture script. */
	void Start () {
        handler = FindObjectOfType<Handler>();
        rb = GetComponent<Rigidbody>();
    }

    /* Far enough out or range, remove from scene. */
    void Update() {
        if (transform.position.z > 30) Destroy(gameObject);
    }

    /* Handle collision with fracturable object. */
    void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.tag == "VFGlass") {
            handler.vf.fractureObject(transform.position.x, transform.position.y);
            rb.AddForce(new Vector3(0, 0, 1) * force, ForceMode.Force);
        } else if (collision.gameObject.tag == "JFGlass") {
            handler.jf.fractureObject();
            rb.AddForce(new Vector3(0, 0, 1) * force, ForceMode.Force);
        } else if (collision.gameObject.tag == "Piece" && canSubdivide) {
            collision.gameObject.GetComponent<MeshGenerator>().subDivideMesh();
            canSubdivide = false;
        }
    }
}
