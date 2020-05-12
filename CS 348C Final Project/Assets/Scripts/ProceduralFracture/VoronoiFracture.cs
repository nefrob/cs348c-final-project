/*
* File:        Voronoi Fracture
* Author:      Robert Neff
* Date:        12/04/17
* Description: Uses Fortune Voronoi algorithm dividing planar object into fractured meshes.
*/

using System.Collections.Generic;
using UnityEngine;
using Fracture;

public class VoronoiFracture : MonoBehaviour {
    // Private variables
    private GameObject fracPiece; // prefab
    private GameObject startObj; // positioning
    private GameObject piecesContainer;

    // For generation
    private Bounds objectBounds;
    private int nSites;
    private bool fullRand;
    private int standardRelaxIterations;
    [Range(0, 0.5f)] [SerializeField] private float sitesToImpactCloseness = 0.5f;

    // Fracture items
    private List<Site> sites;
    private FortuneVoronoi fv;
    private VoronoiDiagram diagram;
    private List<GameObject> fracturedPieces;
    private bool gizmos; // drawing diagram separately

    /*
     * Constructor (can't set up normally since extending MonoBehaviour)
     */
    public void init(Bounds bounds, GameObject fracPiece, GameObject startObj, GameObject piecesContainer,
        int nSites = 10, bool randSites = false, int relaxIter = 1, bool gizmos = false) {

        objectBounds = bounds;
        this.fracPiece = fracPiece;
        this.startObj = startObj;
        this.piecesContainer = piecesContainer;
        fullRand = randSites;
        standardRelaxIterations = relaxIter;
        this.nSites = nSites;
        this.gizmos = gizmos;

        sites = new List<Site>();
        fv = new FortuneVoronoi();
        fracturedPieces = new List<GameObject>();
    }

    /* Method: reset
     * -------------
     * Resets the scene to start unfractured state.
     */
    public void reset() {
        // Destory fracture pieces
        foreach (Transform child in piecesContainer.transform)
            Destroy(child.gameObject);

        // Re-enable start object
        startObj.SetActive(true);
    }

    /* Method: fractureObject
     * ----------------------
     * Handles collision with object or input that causes fracture.
     */
    public void fractureObject(float x, float y) {
        startObj.SetActive(false);
        generateRandomSites(x, y);
        computeNewDiagram();
        generateFracturePieces();
    }

    /* Method: normalInputFracture
     * ---------------------------
     * Fracture diagram from keycode input.
     */
    public void normalInputFracture() {
        reset();
        fractureObject(Random.Range(objectBounds.min.x, objectBounds.max.x),
            Random.Range(objectBounds.min.z, objectBounds.max.z));
    }

    /* Method: relaxInputFracture
     * --------------------------
     * Relax diagram then fracture from keycode input.
     */
    public void relaxInputFracture() {
        reset();
        startObj.SetActive(false);
        relaxSites(standardRelaxIterations);
        generateFracturePieces();
    }

    /* Method: invertFullRandomSites
     * -----------------------------
     * Set if sites are generated clustered or fully random.
     */
    public void invertFullRandomSites() {
        fullRand = !fullRand;
    }

    /* Function: computeNewDiagram
     * ---------------------------
     * Computes a new diagram.
     */
    private void computeNewDiagram() {
        fv.initialize(sites, objectBounds);
        diagram = fv.computeVoronoiDiagram();
    }

    /* Function: generateFracturePieces
     * --------------------------------
     * Generates fracture parts of original.
     */
    void generateFracturePieces()  {
        // Reset for new generation
        foreach (GameObject obj in fracturedPieces) Destroy(obj);
        fracturedPieces.Clear();

        // Generate new fracture pieces
        foreach (VoronoiCell cell in diagram.cells) {
            GameObject piece = Instantiate(fracPiece, cell.site.ToVector3(), Quaternion.identity) as GameObject;
            piece.transform.parent = piecesContainer.transform;
            piece.name = "Piece " + cell.site.id;
            fracturedPieces.Add(piece);
            piece.GetComponent<MeshGenerator>().createMesh(cell);
        }
    }

    /* Function: generateSites
     * -----------------------
     * Generates number of random sites within the bounds. The sites are concentrated
     * about the provided x, y coordinates. 
     */
    private void generateRandomSites(float x, float y, bool clear = true) {
        if (clear) sites.Clear();

        // Randomly generate site positions
        for (int i = 0; i < nSites; i++)
            addSiteCloseToImpact(x, y);
    }

    /* Function: getSiteCloseToImpact
    * -------------------------------
    * Generates a random site with high probability of being near
    * the impact point (x, y).
    */
    private void addSiteCloseToImpact(float x, float y) {
        float newX = Random.Range(objectBounds.min.x, objectBounds.max.x);
        float newY = Random.Range(objectBounds.min.z, objectBounds.max.z);

        if (!fullRand) {
            newX += (x - newX) * Random.Range(sitesToImpactCloseness, 1.0f);
            newY += (y - newY) * Random.Range(sitesToImpactCloseness, 1.0f);
        }

        sites.Add(new Site(newX, newY));
    }

    /* Function: relaxSites
     * --------------------
     * Relaxes the cell sites for the desired amount of iterations.
     * 
     * Note: Modelled after: Jeremie St-Amand
     * https://github.com/jesta88/Unity-Voronoi/blob/master/Assets/Voronoi/Demos/VoronoiDemo.cs
     */
    private void relaxSites(int nIterations) {
        if (diagram == null) return;

        for (int i = 0; i < nIterations; i++) {
            sites.Clear();

            float probability = 1 / diagram.cells.Count * 0.1f;

            // Relax each site
            float distance = 0;
            Site site;
            for (int j = diagram.cells.Count - 1; j >= 0; j--) {
                VoronoiCell cell = diagram.cells[j];
                float random = Random.value;
                if (random < probability) continue;

                site = getCentroid(cell);
                distance = getDistance(site, cell.site);

                if (distance > 2.0f) { // slow relaxation
                    site.x = (site.x + cell.site.x) / 2.0f;
                    site.y = (site.y + cell.site.y) / 2.0f;
                }

                if (random > (1 - probability)) {
                    distance /= 2.0f;
                    sites.Add(new Site(site.x + (site.x - cell.site.x) / distance,
                        site.y + (site.y - cell.site.y) / distance));
                }
                sites.Add(site);
            }
            computeNewDiagram(); // update sites
        }
    }

   /* Function: getCentroid
    * ---------------------
    * Gets the centroid of the cell.
    */
    private Site getCentroid(VoronoiCell cell) {
        float x, y, mult;
        x = y = 0.0f;
        Vertex v1, v2;

        for (int i = cell.halfEdges.Count - 1; i >= 0; i--) {
            HalfEdge he = cell.halfEdges[i];
            v1 = he.getStartVertex();
            v2 = he.getEndVertex();
            mult = v1.x * v2.y - v1.y * v2.x;
            x += (v1.x + v2.x) * mult;
            y += (v1.y + v2.y) * mult;
        }
        mult = getArea(cell) * 6.0f;
        return new Site(x / mult, y / mult);
    }

   /* Function: getDistance
    * ---------------------
    * Gets the distance bewteen a generated site and the cell site.
    */
    private float getDistance(Site site, Site cellSite) {
        float dx = site.x - cellSite.x;
        float dy = site.y - cellSite.y;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

   /* Function: getArea
    * -----------------
    * Gets the area of a voronoi cell.
    */
    private float getArea(VoronoiCell cell) {
        float area = 0.0f;
        Vertex v1, v2;

        for (int i = cell.halfEdges.Count - 1; i >= 0; i--) {
            HalfEdge he = cell.halfEdges[i];
            v1 = he.getStartVertex();
            v2 = he.getEndVertex();
            area += v1.x * v2.y;
            area -= v1.y * v2.x;
        }
        area /= 2.0f;
        return area;
    }

    /* Function: OnDrawGizmos
     * ----------------------
     * Display cells/sites.
     */
    void OnDrawGizmos() {
        if (!gizmos) return;

        if (diagram != null) {
            foreach (VoronoiCell cell in diagram.cells) {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(new Vector3(cell.site.x, 0, cell.site.y), Vector3.one);

                if (cell.halfEdges.Count > 0) {
                    foreach (HalfEdge halfEdge in cell.halfEdges) {
                        Edge edge = halfEdge.edge;

                        if (edge.v1 && edge.v2) {
                            Gizmos.color = Color.red;
                            Gizmos.DrawLine(new Vector3(edge.v1.x, 0, edge.v1.y),
                                            new Vector3(edge.v2.x, 0, edge.v2.y));
                        }
                    }
                }
            }
        }
    }
}
