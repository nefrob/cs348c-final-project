Details on use of fracture demo:
To play the demo hit the play icon in the top middle of Unity editor.
Once in play mode toggle between demo items via the LEFT ARROW and RIGHT ARROW keys.

The first mode uses the Fortune Voronoi algorithm to procedurally generate Voronoi diagrams for 
fracture. The diagram is then used to generate a fracture mesh for the glass. To demonstrate this, 
the options are as follows:
- Hit SPACE to fire projectiles at the glass.
- Hit E to fracture the glass without requiring a collision.
- Hit R to relax the site position and re-fracture the glass.
- Hit T to reset the glass to an unfractured state.
- Hit Y to toggle between collision point site concentration and full site position randomization.

The second mode uses the Jump Flood algorithm to procedurally generate a Voronoi diagram texture on 
the GPU. Rudimentary feature detection is then performed on the rasterized diagram to extract cells/vertices. 
Finally, the resulting diagram is used to generate a fracture mesh for the glass as before. To demonstrate 
this, the options are as follows:
- Hit SPACE to fire projectiles at the glass.
- Hit E to fracture the glass without requiring a collision.
- Hit T to reset the glass to an unfractured state.


The third and final mode uses the Jump Flood algorithm to generate a Voronoi diagram texture on the GPU 
and displays it. The options to change site counts are as follows:
- Hit UP ARROW to increase site count by 50 (up to 1000).
- Hit DOWN ARROW to decrease site count by 50 (down to 50).