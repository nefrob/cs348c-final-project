/*
* File:        Voronoi Cell
* Author:      Robert Neff
* Date:        11/06/17
* Description: Implements a Voronoi cell.
* 
* Note: Modelled after Jeremie St-Amand's Cell.cs:
* https://github.com/jesta88/Unity-Voronoi/blob/master/Assets/Voronoi/Graph/Cell.cs
*/

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fracture {
    public class VoronoiCell {
        public Site site; // site in cell
        public List<HalfEdge> halfEdges; // half-edges comprising border
        public bool needToClose;

        /* 
        * Constructor 
        */
        public VoronoiCell(Site site) {
            this.site = site;
            halfEdges = new List<HalfEdge>();
            needToClose = false;
        }

        /* Method: initialize
         * ------------------
         * Initializes the cell.
         */
        public void initialize(Site site) {
            this.site = site;
            halfEdges = new List<HalfEdge>();
			needToClose = false;
        }

        /* Method: pruneSortEdges
         * ----------------------
         * Finds half-edges used in cell, sorts them by CCW angle order and
         * returns the count.
         */
        public int pruneSortEdges() {
            // Get rid of half-edges not in cell
            for (int i = halfEdges.Count - 1; i >= 0; i--) 
                if (!halfEdges[i].edge.v2 || !halfEdges[i].edge.v1) halfEdges.Remove(halfEdges[i]);

            // Sort half-edges CCW
            halfEdges = halfEdges.OrderByDescending(h => h.angle).ToList();
            return halfEdges.Count;
        }

        /* Method: getNeighborIds
         * ----------------------
         * Loops over half-edges to find neighbor sites.
         * Returns a list of the neighbor site id tags.
         */
        public List<int> getNeighborIds() {
            List<int> neighbors = new List<int>();
            Edge edge = null;
            for (int i = halfEdges.Count - 1; i >= 0; i--) {
                edge = halfEdges[i].edge;
                if (edge.s1 != null && edge.s1.id != site.id) neighbors.Add(edge.s1.id);
                else if (edge.s2 != null && edge.s2.id != site.id)neighbors.Add(edge.s2.id); 
            }
            return neighbors;
        }

        /* Method: getCellBounds
         * ---------------------
         * Compute and return bounding box of cell.
         */
        public Bounds getCellBounds() {
            float xMin, yMin;
            xMin = yMin = Mathf.Infinity;
            float xMax, yMax;
            xMax = yMax = -Mathf.Infinity;
            Vertex v = null;
            // Loop over start vertex to get bounding box (since cycle don't check end vertex)
            for (int i = halfEdges.Count - 1; i >= 0; i--) {
                v = halfEdges[i].getStartVertex();
                if (v.x < xMin) xMin = v.x; 
                if (v.y < yMin) yMin = v.y; 
                if (v.x > xMax) xMax = v.x; 
                if (v.y > yMax) yMax = v.y; 
            }

            // Verify y
            Bounds bounds = new Bounds();
            bounds.SetMinMax(new Vector3(xMin, 0, xMax), new Vector3(yMin, 0, yMax));
            return bounds;
        }

        /* Method: doesPointIntersect
         * --------------------------
         * Returns whether a point is inside, on the boundary or outside the cell.
         * If -1 point is outside, 0 point is on boundary, 1 point is inside the cell.
         */
        public int pointIntersection(float x, float y) {
            HalfEdge halfEdge;
            Vertex v1, v2;
            float status;
            // Voronoi polygon is convex and 2D so check if point is always on same 
            // side of directed "path" of perimeter half-edges 
            // Loop over half-edges to check if outside or not
            for (int i = halfEdges.Count - 1; i >= 0; i--) {
                halfEdge = halfEdges[i];
                v1 = halfEdge.getStartVertex();
                v2 = halfEdge.getEndVertex();
                status = (y - v1.y) * (v2.x - v1.x) - (x - v1.x) * (v2.y - v1.y); // 
                if (status == 0) return 0; // on cell boundary
                else if (status > 0) return -1; // outside cell
            }
            return 1; // inside cell
        }
    }
}
