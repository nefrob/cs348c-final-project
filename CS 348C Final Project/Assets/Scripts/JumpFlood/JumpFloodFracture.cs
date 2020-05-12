/*
* File:        Jump Flood Fracture
* Author:      Robert Neff
* Date:        11/29/17
* Description: Allows for Voronoi fracture using the Jump Flood + 2 algorithm.
*/

using System.Collections.Generic;
using UnityEngine;

public class JumpFloodFracture : MonoBehaviour {
    // Private variables
    private GameObject fracPiece; // prefab
    private GameObject startObj; // positioning
    private GameObject piecesContainer;
    // Shader to compute with
    private Shader jumpFloodShader;  

    private static int x; // texture dimensions
    private static int y;
    private int numSites;
    private List<GameObject> fracturedPieces; // pieces generated for reset
    private Dictionary<Color, List<Vector3>> verticesByColor; // vertex info

    /* 
     * Constructor (can't set up normally since extending MonoBehaviour)
     */
    public void init(Shader shader, GameObject fracPiece, GameObject startObj, GameObject piecesContainer,
        int numSites = 10, int width = 512, int height = 512) {

        this.fracPiece = fracPiece;
        this.startObj = startObj;
        this.piecesContainer = piecesContainer;
        jumpFloodShader = shader;
        x = width;
        y = height;
        this.numSites = numSites;

        fracturedPieces = new List<GameObject>();
        verticesByColor = new Dictionary<Color, List<Vector3>>();
    }

    /* Method: updateNumSites
     * ----------------------
     * Just updates number of sites.
     */
    public void updateNumSites(int numSites) {
        this.numSites = numSites;
    }

    /* Method: reset
     * -------------
     * Resets the start object to start unfractured state.
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
    public void fractureObject() {
        startObj.SetActive(false);
        verticesByColor.Clear();
        RenderTexture rend = generateTexture();
        generateDiagramFromTexture(rend);
        generateFracturePieces();
    }

    /* Function: getVertices
     * ---------------------
     * Get Voronoi cell vertices from rasterized diagram (texture).
     */
    private void generateDiagramFromTexture(RenderTexture rTex) {
        Texture2D tex = null;
        fillTexFromMaterial(ref rTex, ref tex);

        // Handle top row
        handleEdgeRow(ref tex, 0);

        HashSet<Color> found = new HashSet<Color>();
        // Loop over rest of image adding vertices
        for (int j = 1; j < y - 2; j += 2) {
            for (int i = x - 2; i >= 1; i -= 2) {
                // Check surrounding pixels for vertex
                for (int di = -1; di <= 1; di++)
                    for (int dj = -1; dj <= 1; dj++)
                        found.Add(tex.GetPixel(i + di, j + dj));

                // Be less strict on edges when finding colors
                int requiredCount = isCloseToEdge(i, j) ? 2 : 3;

                // Check if valid vertex found, add to all colors
                if (found.Count >= requiredCount)
                    foreach (Color c in found)
                        addVertex(c, i, j);
                found.Clear();
            }
        }
        // Handle bottom row
        handleEdgeRow(ref tex, y - 1);
    }

    /* Function: fillTexFromMaterial
     * -----------------------------
     * Fills texture from the renderer.
     */
    private void fillTexFromMaterial(ref RenderTexture rTex, ref Texture2D tex) {
        tex = new Texture2D(x, y, TextureFormat.ARGB32, true);
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
    }

    /* Function: isCloseToEdge
     * -----------------------
     * Check if current pixel position is 'close' to edge.
     */
    private bool isCloseToEdge(int i, int j) {
        return (i >= x - 3 || i <= 2 || j <= 1 || j >= y - 3);
    }

    /* Function: handleEdgeRow
     * -----------------------
     * Add vertices in the top or bottom row.
     */
    private void handleEdgeRow(ref Texture2D tex, int row) {
        Color last = tex.GetPixel(x - 1, row);
        addVertex(last, x - 1, row);
        for (int i = x - 2; i >= 1; i--) {
            Color currPixel = tex.GetPixel(i, row);
            if (currPixel != last) {
                addVertex(last, i, row);
                last = currPixel;
            }
        }
        addVertex(tex.GetPixel(0, row), 0, row);
    }

    /* Function: generateFracturePieces
     * --------------------------------
     * Generates fracture parts from vertices.
     */
    private void generateFracturePieces() {
        Dictionary<float, Vector3> anglesToPos = new Dictionary<float, Vector3>();
        int id = 0;
        foreach (Color c in verticesByColor.Keys) {
            // Average position to find middle (fake site)
            Vector3 site = centroidOfVertices(c);

            // Loop over colors (cells)
            List<float> anglesToSort = new List<float>();
            foreach (Vector3 v in verticesByColor[c]) {
                float angle = Mathf.Atan2(v.y - site.y, v.x - site.x);
                anglesToSort.Add(angle);
                anglesToPos[angle] = v;
            }
            anglesToSort.Sort(); // CW sort

            Vector3[] verts = new Vector3[verticesByColor[c].Count];
            for (int i = 0; i < verts.Length; i++)
                verts[i] = anglesToPos[anglesToSort[i]];
            anglesToPos.Clear();

            // Generate mesh 
            GameObject piece = Instantiate(fracPiece, Vector3.zero, Quaternion.identity) as GameObject;
            piece.transform.parent = piecesContainer.transform;
            piece.name = "Piece " + id.ToString();
            fracturedPieces.Add(piece);
            piece.GetComponent<MeshGenerator>().createMeshVertsSpecified(verts, site);
            id++;
        }
    }

    /* Function: addVertex
     * -------------------
     * Adds a Voronoi vertex to cell with color provided
     * at the specified location.
     */
    private void addVertex(Color c, int i, int j) {
        // Init new
        if (!verticesByColor.ContainsKey(c))
            verticesByColor[c] = new List<Vector3>();
        verticesByColor[c].Add(pixelPosToVector(i, j));
    }

    /* Function: pixelPosToVector
     * --------------------------
     * Converts pixel i/j coordinate to physical position 
     * on a plane.
     */
    private Vector3 pixelPosToVector(int i, int j) {
        float xPos = startObj.transform.localScale.x / 2f - (startObj.transform.localScale.x / x * i)
           + startObj.transform.position.x;
        float yPos = startObj.transform.localScale.y / 2f - (startObj.transform.localScale.y / y * j)
           + startObj.transform.position.y;
        return new Vector3(xPos, yPos, 0);
    }

    /* Function: centroidOfVertices
     * ----------------------------
     * Returns centroid of vertices with associated color.
     */
    private Vector3 centroidOfVertices(Color c) {
        float xPos, yPos;
        xPos = yPos = 0;
        foreach (Vector3 v in verticesByColor[c]) {
            xPos += v.x;
            yPos += v.y;
        }
        return new Vector3(xPos / verticesByColor[c].Count, yPos / verticesByColor[c].Count, 0);
    }

    /* Function: generateTexture
     * -------------------------
     * Generates Voronoi cell texture.
     */
    public RenderTexture generateTexture() {
        Texture2D sitesOnlyTexture = new Texture2D(x, y);
        sitesOnlyTexture.wrapMode = TextureWrapMode.Repeat;

        // Fill texture with random sites/colors for sites
        for (int site = 0; site < numSites; site++) {
            float i = Random.value;
            float j = Random.value;
            sitesOnlyTexture.SetPixel((int)(x * i), (int)(y * j), new Color(i, j, 1, (i + j) * 0.5f));
        }
        sitesOnlyTexture.Apply();

        // Fill texture via shader
        RenderTexture finalTex = null;
        Material mat = new Material(jumpFloodShader);
        mat.SetFloat("_blue", Random.value);
        mat.mainTexture = sitesOnlyTexture;
        int step = 2;
        while (step <= Mathf.Max(x, y)) {
            jfPass(ref mat, ref finalTex, step);
            step *= 2; // double every time for log_2(n) iterations
        }

        // JF + 2 extra passes to reduce error
        jfPass(ref mat, ref finalTex, 2);
        jfPass(ref mat, ref finalTex, 1);

        DestroyImmediate(mat);
        return finalTex;
    }

    /* Function: jfPass
     * ----------------
     * Execute a single JF pass.
     */
    private static void jfPass(ref Material mat, ref RenderTexture tex, int step) {
        mat.SetVector("_k", new Vector4(1f / step, 1f / step, 0, 0));
        tex = renderCurr(mat);
        DestroyImmediate(mat.mainTexture);
        mat.mainTexture = tex;
    }

    /* Function: renderCurr
     * --------------------
     * Renders the current round texture to the screen.
     */
    private static RenderTexture renderCurr(Material mat) {
        RenderTexture newTex = new RenderTexture(x, y, 16);
        RenderTexture old = RenderTexture.active;
        RenderTexture.active = newTex;

        newTex.wrapMode = TextureWrapMode.Repeat;
        if (isPowerOfTwo(x) > 0 && isPowerOfTwo(y) > 0) newTex.isPowerOfTwo = true;

        Shader.SetGlobalVector("_TexelSize", new Vector4(1f / x, 1f / y, 0, 0));
        mat.SetPass(0);
        drawFullScreenQuad();

        RenderTexture.active = old;
        return newTex;
    }

    /* Function: drawFullScreenQuad
     * ----------------------------
     * Draws full screen quad of texture.
     * 
     * Note: Sourced from - https://forum.unity.com/threads/interesting-algorithm.11678/
     */
    private static void drawFullScreenQuad() {
        GL.Clear(true, true, Color.clear);
        GL.PushMatrix();
        GL.LoadOrtho();
        GL.Begin(GL.QUADS);
            GL.TexCoord2(0, 0);
            GL.Vertex3(0, 0, 0);
            GL.TexCoord2(0, 1);
            GL.Vertex3(0, 1, 0);
            GL.TexCoord2(1, 1);
            GL.Vertex3(1, 1, 0);
            GL.TexCoord2(1, 0);
            GL.Vertex3(1, 0, 0);
        GL.End();
        GL.PopMatrix();
    }

    /* Function: isPowerOfTwo
     * ----------------------
     * Checks if the provided integer is a power of two.
     */
    private static int isPowerOfTwo(int n) {
        float log_2_n = Mathf.Log(n, 2);
        return (log_2_n % 1 == 0) ? (int) log_2_n : -1;
    }
}
