/*
* File:        Jump Flood Texture
* Author:      Robert Neff
* Date:        11/29/17
* Description: Creates texture with jump flood colored Voronoi cells
*              and applies it to object.
*/

using UnityEngine;

public class JumpFloodTexture : MonoBehaviour {
    // Jump Flood shader
    [SerializeField] private Shader jfShader;
    // Sites to compute
    [SerializeField] private int nSites = 10;

    // Algorithm script
    private JumpFloodFracture jf;

    /* Set texture. */
	void Start () {
        jf = GetComponent<JumpFloodFracture>();
        jf.init(jfShader, null, null, null, nSites);
        GetComponent<Renderer>().material.mainTexture = jf.generateTexture();
	}

    /* Method: renderNewTexture
     * ------------------------
     * Re-renders random Voronoi texture with new site count.
     */
    public void renderNewTexture(int numSites) {
        jf.updateNumSites(numSites);
        GetComponent<Renderer>().material.mainTexture = jf.generateTexture();
    }
}
