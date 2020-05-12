/*
* File:        Sub-Fracture Generator 
* Author:      Robert Neff
* Date:        11/25/17
* Description: Generates fracture objects in fan pattern for already
*              fractured pirece. Naive implementation for further fracture
*              past already implemented Fortune Voronoi method.
*/

using UnityEngine;

public class SubFractureGenerator : MonoBehaviour {
    [SerializeField] private GameObject childObject;

    /* Method: subDivideMesh
     * ---------------------
     * Sub divides already fractured component in fan pattern 
     * into smaller pieces.
     */
    public void subDivideMesh() {
        GetComponent<MeshCollider>().enabled = false; // so no collision with newly instatiated pieces

        Mesh myMesh = GetComponent<MeshFilter>().sharedMesh;

        Vector3[] verts = new Vector3[3];
        int halfLength = myMesh.vertices.Length / 2;
        verts[0] = myMesh.vertices[0]; // could randomize point for better look

        // Generate submesh pieces
        for (int i = 1; i < halfLength - 1; i++) {
            verts[1] = myMesh.vertices[i];
            verts[2] = myMesh.vertices[i + 1];
            createChildPiece(verts, triangleCentroid(verts));
        }
        verts[1] = myMesh.vertices[halfLength - 1];
        verts[2] = myMesh.vertices[1];
        createChildPiece(verts, triangleCentroid(verts));

        Destroy(gameObject); // no longer needed, remove
    }

    /* Function: triangleCentroid
     * --------------------------
     * Returns centroid position of a triangle.
     */
    private Vector3 triangleCentroid(Vector3[] verts) {
        float x = (verts[0].x + verts[1].x + verts[2].x) / 3;
        float y = (verts[0].y + verts[1].y + verts[2].y) / 3;
        return new Vector3(x, y, 0);
    }

    /* Function: randomPointInTriangle
     * -------------------------------
     * Returns random point in the triangle.
     */
    private Vector3 randomPointInTriangle(Vector3[] verts) {
        float r1 = Random.value;
        float r2 = Random.value;
        Vector3 result = (1 - Mathf.Sqrt(r1)) * verts[0] + Mathf.Sqrt(r1) * (1 - r2) * verts[1] +
            r2 * Mathf.Sqrt(r1) * verts[2];

        return result;
    }

    /* Function: createChildPiece
     * --------------------------
     * Creates a mesh from the provided vertices. Essentially a
     * copy of code from mesh generator. Not super efficient, but
     * works so leave (not primary fracture implementation).
     */
    private void createChildPiece(Vector3[] verts, Vector3 site, float zThickness = 1) {
        int vert_per_side = verts.Length + 1;
        Vector3[] vertices = new Vector3[vert_per_side * 2];
        int triangle_verts_per_face = verts.Length * 3;
        int triangle_verts_per_side = verts.Length * 6;
        int[] triangles = new int[triangle_verts_per_face * 2 + triangle_verts_per_side];

        // Compute mesh positions
        vertices[0] = site; // center
        vertices[2 * vert_per_side - 1] = new Vector3(vertices[0].x, vertices[0].y, zThickness); // center plane 2
        for (int v = 1, t = 0; v < vert_per_side; v++, t += 3) {
            // Set vertices
            vertices[v] = verts[v - 1]; // front face
            vertices[vert_per_side * 2 - v - 1] = new Vector3(vertices[v].x, vertices[v].y, zThickness); // back face

            // Set triangles
            triangles[t] = 0; // front face
            triangles[t + 1] = v + 1;
            triangles[t + 2] = v;

            triangles[triangle_verts_per_face + t] = vertices.Length - 1; // back face
            triangles[triangle_verts_per_face + t + 1] = vertices.Length - v - 1;
            triangles[triangle_verts_per_face + t + 2] = vertices.Length - v - 2;
        }
        // Close off front fan
        triangles[triangle_verts_per_face - 1] = vert_per_side - 1;
        triangles[triangle_verts_per_face - 2] = 1;
        // Close off back fan
        triangles[2 * triangle_verts_per_face - 1] = vertices.Length - 2;
        triangles[2 * triangle_verts_per_face - 2] = vert_per_side;

        for (int v = 1, t = 0; v < vert_per_side; v++, t += 6) {
            // Middle
            triangles[2 * triangle_verts_per_face + t] = v;
            triangles[2 * triangle_verts_per_face + t + 1] = v + 1;
            triangles[2 * triangle_verts_per_face + t + 2] = vertices.Length - v - 1;

            triangles[2 * triangle_verts_per_face + t + 3] = v + 1;
            triangles[2 * triangle_verts_per_face + t + 4] = vertices.Length - v - 2;
            triangles[2 * triangle_verts_per_face + t + 5] = vertices.Length - v - 1;
        }
        // Close of middle section
        int offset = triangle_verts_per_face * 2 + triangle_verts_per_side;
        triangles[offset - 6] = vert_per_side - 1;
        triangles[offset - 5] = 1;
        triangles[offset - 4] = vert_per_side;

        triangles[offset - 3] = 1;
        triangles[offset - 2] = vertices.Length - 2;
        triangles[offset - 1] = vert_per_side;

        // Set mesh items
        GameObject child = Instantiate(childObject, transform.position, transform.rotation);
        child.transform.parent = transform.parent;
        Mesh childMesh;
        child.GetComponent<MeshFilter>().sharedMesh = childMesh = new Mesh();
        childMesh.name = child.name = "Sub-Piece ->" + gameObject.name;
        childMesh.vertices = vertices;
        childMesh.triangles = triangles;
        childMesh.RecalculateBounds();

        // Set properties
        child.GetComponent<MeshCollider>().sharedMesh = childMesh;
        child.GetComponent<MeshCollider>().enabled = true;
        child.GetComponent<Renderer>().sharedMaterial = GetComponent<Renderer>().sharedMaterial;
    }
}
