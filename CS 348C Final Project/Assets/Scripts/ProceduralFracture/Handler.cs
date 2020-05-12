/*
* File:        Handler
* Author:      Robert Neff
* Date:        12/04/17
* Description: Handles input and collision to trigger events.
*/

using UnityEngine;

public class Handler : MonoBehaviour {
    // Texture
    [SerializeField] private JumpFloodTexture jfTex;
    private int textureNumSites = 200;

    // Fortune Voronoi Fracture
    public Bounds bounds;
    public static Bounds voronoiBounds;
    [SerializeField] private int vfSites = 10;
    [SerializeField] private GameObject startObjVF;
    [SerializeField] private GameObject piecesContainerVF;

    // Jump Flood
    [SerializeField] private Shader jfShader;
    [SerializeField] private int jfSites = 10;
    [SerializeField] private GameObject startObjJF;
    [SerializeField] private GameObject piecesContainerJF;

    [SerializeField] private GameObject fracPiece;
    private int tweenPosCount;

    public VoronoiFracture vf;
    public JumpFloodFracture jf;

	/* Initialize fracture handlers. */
	void Start () {
        tweenPosCount = 0;

        jfTex = FindObjectOfType<JumpFloodTexture>();

        voronoiBounds = bounds;
        vf = GetComponent<VoronoiFracture>();
        vf.init(voronoiBounds, fracPiece, startObjVF, piecesContainerVF, vfSites);

        jf = GetComponent<JumpFloodFracture>();
        jf.init(jfShader, fracPiece, startObjJF, piecesContainerJF, jfSites);
	}

    // Update is called once per frame
    void Update() {
        // Movement input
        if (Input.GetKeyDown(KeyCode.RightArrow) && tweenPosCount <= 2) {
            // Move camera right
            tweenPosCount++;
            iTween.MoveBy(gameObject, iTween.Hash("x", -75, "easeType", "easeInOutExpo"));
        } else if (Input.GetKeyDown(KeyCode.LeftArrow) && tweenPosCount >= 0)  {
            // Move camera left
            tweenPosCount--;
            iTween.MoveBy(gameObject, iTween.Hash("x", 75, "easeType", "easeInOutExpo"));
        }

        // Update texture
        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            if (textureNumSites <= 1000) textureNumSites += 50;
            jfTex.renderNewTexture(textureNumSites);
        } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
            if (textureNumSites > 50) textureNumSites -= 50;
            jfTex.renderNewTexture(textureNumSites);
        }

        // VF
        if (tweenPosCount == 0) {
            // Toggle random full random sites or concentrated around impact
            if (Input.GetKeyDown(KeyCode.Y)) vf.invertFullRandomSites();

            // Generate diagram
            if (Input.GetKeyDown(KeyCode.E)) vf.normalInputFracture();

            // Relax diagram sites and regenerate
            if (Input.GetKeyDown(KeyCode.R)) vf.relaxInputFracture();

            // Reset fracture state
            if (Input.GetKeyDown(KeyCode.T)) vf.reset();
        } else if (tweenPosCount == 1) {
            // Generate diagram
            if (Input.GetKeyDown(KeyCode.E)) {
                jf.reset();
                jf.fractureObject();
            }

            // Reset fracture state
            if (Input.GetKeyDown(KeyCode.T)) jf.reset();
        }
    }
}
