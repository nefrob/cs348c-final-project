/*
* File:        Half-Edge
* Author:      Robert Neff
* Date:        11/06/17
* Description: Implements half-edge, an edge dividing points closer to one site or another.
*              The half-edge between two sites is perpendicular to the line connecting them.
* 
* Note: Angle to find CCW half-edges modelled after Jeremie St-Amand half-edge class:
* https://github.com/jesta88/Unity-Voronoi/blob/master/Assets/Voronoi/Graph/HalfEdge.cs
*/

using UnityEngine;

namespace Fracture {
    public class HalfEdge {
        public Site site;
        public Edge edge;
        public float angle; // for sorting half-edges CCW, angle from s1 (left) to s2 (right)

        /* 
         * Constructor 
         */
        public HalfEdge(Edge edge, Site s1, Site s2) {
            site = s1;
            this.edge = edge;

            if (s2) angle = Mathf.Atan2(s2.y - s1.y, s2.x - s1.x);
            else { // border edge, no s2, use line perpendicualr to half-edge
                Vertex v1 = edge.v1;
                Vertex v2 = edge.v2;
                angle = (edge.s1 == s1) ? Mathf.Atan2(v2.x - v1.x, v1.y - v2.y): Mathf.Atan2(v1.x - v2.x, v2.y - v1.y);
            }
        }

        /* Method: getStartVertex
         * ----------------------
         * Returns the starting vertex of an edge from the current site.
         */
        public Vertex getStartVertex() {
            return edge.s1 == site ? edge.v1 : edge.v2;
        }

        /* Method: getEndVertex
         * --------------------
         * Returns the ending vertex of an edge from the current site.
         */
        public Vertex getEndVertex() {
            return edge.s1 == site ? edge.v2 : edge.v1;
        }

        /* Basic overload. */
        public static implicit operator bool(HalfEdge he) {
            return he != null;
        }
    }
}