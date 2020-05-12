/*
* File:        Fortune Voronoi
* Author:      Robert Neff
* Date:        11/16/17
* Description: Implements Fortune's algorithm.
* 
* Note: Breakpoint and circle event calculations and conditions for closing cells modelled after:
*       Jeremie St-Amand
*       https://github.com/jesta88/Unity-Voronoi/blob/master/Assets/Voronoi/FortuneVoronoi.cs
*/

using Priority_Queue;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Fracture {
    public class FortuneVoronoi {
        // Private member variables
        private static readonly float EPS = 1e-1f; // sig figs to compare to (floats may not be the same)

        // Tracking current state of beach line
        private RBTree<BeachArc> beachLine;

        // Event Queue of site and circle events to handle
        private SimplePriorityQueue<Event, Event> sweepLine;

        // Junk events to be reused for better performance
        private List<BeachArc> usedBeachArcs;
        private List<CircleEvent> usedCircleEvents;

        // For resultant diagram
        private List<Site> sites;
        private List<Edge> edges;
        private List<VoronoiCell> cells;
        private Bounds boundingBox;
        private bool closingErrors = true;

        /* 
         * Constructor 
         */
        public FortuneVoronoi() {
            sweepLine = new SimplePriorityQueue<Event, Event>(Event.sortEventPriority);

            usedBeachArcs = new List<BeachArc>();
            usedCircleEvents = new List<CircleEvent>();

            sites = null;
            edges = null;
            cells = null;
        }

        /* Method: initialize
         * ------------------
         * Initializes variables for new diagram computation.
         */
        public void initialize(List<Site> sites, Bounds boundingBox) {
             // Event Queue <- all site events
            foreach (Site site in sites) {
                Event e = new Event(site);
                sweepLine.Enqueue(e, e);
            }
            
            // Beach line tree <- empty
            if (!beachLine) beachLine = new RBTree<BeachArc>();

            this.sites = sites;
            edges = new List<Edge>();
            cells = new List<VoronoiCell>();
            this.boundingBox = boundingBox;
        }

        /* Method: reset
         * -------------
         * Resets to compute a new diagram.
         */
        public void reset() {
            // Leftover beach sections to the used list
            if (beachLine.Root) {
                BeachArc arc = beachLine.GetFirst(beachLine.Root);
                while (arc) {
                    usedBeachArcs.Add(arc);
                    arc = arc.Next;
                }
            }
            beachLine.Root = null;
            // Reset sweep line (should be empty)
            sweepLine.Clear();
        }

        /* Method: computeVoronoiDiagram
         * -----------------------------
         * Computes Fortune Voronoi digram and returns it.
         */
        public VoronoiDiagram computeVoronoiDiagram() {
            int currSiteID = 0;
            float lastEventX, lastEventY;
            lastEventX = lastEventY = Mathf.Infinity;
            Event e = null;
            while (sweepLine.Count > 0) {
                e = sweepLine.Dequeue();  // remove event from queue with largest y-coord

                if (e.isSiteEvent && !isDuplicate(e, lastEventX, lastEventY)) {
                    e.site.setId(currSiteID);
                    cells.Insert(e.site.id, new VoronoiCell(e.site)); // add new cell at id

                    // Add site to beach section
                    handleSiteEvent(e.site);

                    // Update for next event
                    currSiteID++;
                    lastEventX = e.site.x;
                    lastEventY = e.site.y;
                } else {
                    CircleEvent ce = (CircleEvent)e;
                    handleCircleEvent(ce.arc);
                }
            }

            // Terminate cell edges after queue empty on a bounding box (they go to infinite)
            clipEdgesOnBounds();
            closeCells();

            // Build Voronoi diagram
            VoronoiDiagram diagram = new VoronoiDiagram();
            diagram.sites = sites;
            diagram.cells = cells;
            diagram.edges = edges;
            reset();
            return diagram;
        }

        /* Function: handleSiteEvent
         * -------------------------
         * Handles site event.
         */
        private void handleSiteEvent(Site site) {
            // Locate arc above new site (binary search tree on x-coord)
            BeachArc leftArc = null;
            BeachArc rightArc = null;
            findNewArcPosition(site.x, site.y, ref leftArc, ref rightArc);

            // Create a new site event for the site and add it to RB-tree
            BeachArc newArc = createNewBeachArc(site);
            beachLine.Insert(leftArc, newArc); // if leftArc null, newArc set as root

            // First element in beach line, do nothing
            if (!leftArc && !rightArc) return;
            // Node falls in middle of existing beach section arc
            else if (leftArc == rightArc) {
                // Delete potential circle event of now split section
                deleteCircleEvent(leftArc);

                // Split beach section in two 
                rightArc = createNewBeachArc(leftArc.site);
                beachLine.Insert(newArc, rightArc);

                // New edge between new site and the one that created split old arc (from new breakpoint)
                newArc.edge = rightArc.edge = createNewEdge(leftArc.site, newArc.site);

                //  Check for potential circle events and add to the event queue
                findPotentialCircleEvent(leftArc);
                findPotentialCircleEvent(rightArc);
            }
            // Node falls last on beach line to the right
            else if (leftArc && rightArc == null) {
                newArc.edge = createNewEdge(leftArc.site, newArc.site);
            }
            // Only occurs if no nodes in tree yet (no left curve yet), handled above
            else if (leftArc == null && rightArc) {
                Debug.LogError("Error inserting first element into beachline tree.");
            }
            // Node falls inbetween two other beach section arcs
            else if (leftArc != rightArc) {
                deleteCircleEvent(leftArc);
                deleteCircleEvent(rightArc);

                // Find new cell vertex
                Vertex v = findNewVertex(site, leftArc.site, rightArc.site);

                // Connect vertex to arcs
                rightArc.edge.setStartVertex(leftArc.site, rightArc.site, v);
                newArc.edge = createNewEdge(leftArc.site, site, null, v);
                rightArc.edge = createNewEdge(site, rightArc.site, null, v);

                //  Check for potential circle events and add to the event queue
                findPotentialCircleEvent(leftArc);
                findPotentialCircleEvent(rightArc);
            }
        }

        /* Function: findNewArcPosition 
         * ----------------------------
         * Finds left and right components of arc above the current one to locate
         * where new site arc should fall and updates the values.
         */
        private void findNewArcPosition(float siteX, float sweepLineY, ref BeachArc leftArc, ref BeachArc rightArc) {
            BeachArc node = beachLine.Root;
            float xLeft, xRight;
            while (node) {
                xLeft = findLeftBreakPoint(node, sweepLineY) - siteX;
                // Left node x-value falls somewhere on the left edge of the beachsection
                if (xLeft > EPS) node = node.Left;
                else {
                    xRight = siteX - findRightBreakPoint(node, sweepLineY);
                    // Right node x-value falls somewhere after the right edge of the beachsection
                    if (xRight > EPS) {
                        if (node.Right == null) {
                            leftArc = node;
                            break;
                        }
                        node = node.Right; // continue right
                    } else {
                        // Left node x-value falls exactly on the left edge of the beachsection
                        if (xLeft > -EPS) {
                            leftArc = node.Prev;
                            rightArc = node;
                        }
                        // Right node x-value falls exactly on the right edge of the beachsection
                        else if (xRight > -EPS) {
                            leftArc = node;
                            rightArc = node.Next;
                        }
                        // Node falls in the middle of the beachsection
                        else leftArc = rightArc = node;
                        break;
                    }
                }
            }
        }

        /* Function: findLeftBreakPoint
        * ----------------------------
        * Finds the breakpoint to the left of the new arc in the beach line.
        */
        private float findLeftBreakPoint(BeachArc node, float sweepLineY) {
            // Right site
            Site site = node.site;
            float rightFocusX = site.x;
            float rightFocusY = site.y;
            float rightFocusSweepDiff = rightFocusY - sweepLineY;

            // Parabola focus on directrix (sweep line)
            if (rightFocusSweepDiff == 0.0f) return rightFocusX;

            // New node, don't do anything
            BeachArc leftArc = node.Prev;
            if (leftArc == null) return -Mathf.Infinity;

            // Left site
            site = leftArc.site;
            float leftFocusX = site.x;
            float leftFocusY = site.y;
            float leftFocusSweepDiff = leftFocusY - sweepLineY;
            // Parabola focus on directrix (sweep line)
            if (leftFocusSweepDiff == 0.0f) return leftFocusX;

            // Find intersection
            float xFocusDiff = leftFocusX - rightFocusX;
            float inverseYDiff = 1 / rightFocusSweepDiff - 1 / leftFocusSweepDiff;
            float b = xFocusDiff / leftFocusSweepDiff;

            // Like quadratic equation centered at rightfocusX and different coefficients
            if (inverseYDiff != 0)
                return (-b + Mathf.Sqrt(b * b - 2 * inverseYDiff * (xFocusDiff * xFocusDiff / (-2 * leftFocusSweepDiff)
                    - leftFocusY + leftFocusSweepDiff / 2 + rightFocusY - rightFocusSweepDiff / 2)))
                    / inverseYDiff + rightFocusX;

            // Both parabolas are the same distance to sweep line, breakpoint is in middle
            return (rightFocusX + leftFocusX) / 2;
        }

        /* Function: findRightBreakPoint
         * ----------------------------
         * Finds the breakpoint to the right of the new arc in the beach line.
         */
        private float findRightBreakPoint(BeachArc node, float sweepLineY) {
            BeachArc rightArc = node.Next;
            if (rightArc) return findLeftBreakPoint(rightArc, sweepLineY);
            return (node.site.y == sweepLineY) ? node.site.x : Mathf.Infinity;
        }

        /* Function: createNewBeachArc
         * ---------------------------
         * Creates new beach arc from provided site. Attempts to find 
         * 'used' one to minimize memory costs.
         */
        private BeachArc createNewBeachArc(Site site) {
            // See if used arc available for repurposing
            BeachArc arc = (usedBeachArcs.Count > 0) ? usedBeachArcs.Last() : null;
            if (arc) {
                usedBeachArcs.Remove(arc);
                arc.site = site;
            }
            // Otherwise create new one
            else arc = new BeachArc(site);
            return arc;
        }

        /* Function: deleteCircleEvent
         * ---------------------------
         * Deletes a potential circle event associated with a site event
         * from the event queue (it is no longer correct).
         */
        private void deleteCircleEvent(BeachArc arc) {
            CircleEvent ce = arc.circleEvent;
            arc.circleEvent = null;
            if (ce != null && sweepLine.Contains(ce)) {
                sweepLine.Remove(ce);
                usedCircleEvents.Add(ce);
            }
        }

       /* Function: createNewEdge
        * -----------------------
        * New breakpoint lead to fusion of two existing breakpoints 
        * resulting in new edge.
        */
        private Edge createNewEdge(Site leftSite, Site rightSite, Vertex v1 = null, Vertex v2 = null) {
            // Add new edge to diagram
            Edge edge = new Edge(leftSite, rightSite);
            edges.Add(edge);

            // Set points
            if (v1) edge.setStartVertex(leftSite, rightSite, v1);
            if (v2) edge.setEndVertex(leftSite, rightSite, v2);

            // Update diagram bi-directionally
            cells[leftSite.id].halfEdges.Add(new HalfEdge(edge, leftSite, rightSite));
            cells[rightSite.id].halfEdges.Add(new HalfEdge(edge, rightSite, leftSite));

            return edge;
        }

       /* Function: findPotentialCircleEvent
        * ----------------------------------
        * Checks for a potential circle event after a site event and adds it
        * to the event queue and references it on the arc if found. Done by checking
        * if triple of consecutive arcs (including new one) have breakpoints that 
        * converge.
        */
        private void findPotentialCircleEvent(BeachArc arc) {
            BeachArc leftArc = arc.Prev;
            BeachArc rightArc = arc.Next;

            // Handle missing edge (shouldn't happen)
            if (leftArc == null || rightArc == null) {
                //Debug.LogError("Error attaching potential circle event, a surrounding arc is null.");
                return;
            }

            // No convergence, arc sides same
            if (leftArc.site == rightArc.site) return;

            // If left, center, right site locations are CW no event, else event returned
            getTripleArcCircleEvent(leftArc.site, ref arc, rightArc.site);
        }

        /* Function: getTripleArcCircleEvent
         * ---------------------------------
         * Checks sites of three arcs for convergence of breakpoints, i.e. beach will 
         * collapse giving rise to a circle event. If true, gets a circle event and
         * updates its fields on the receiving arc and event queue.
         * 
         * Note: Calculation of circle event based on Jeremie St-Amand's:
         * https://github.com/jesta88/Unity-Voronoi/blob/master/Assets/Voronoi/FortuneVoronoi.cs
         */
        private void getTripleArcCircleEvent(Site left, ref BeachArc center, Site right) {
            float cx = center.site.x;
            float cy = center.site.y;
            float lx = left.x - cx;
            float ly = left.y - cy;
            float rx = right.x - cx;
            float ry = right.y - cy;
            float orientation = 2 * (lx * ry - ly * rx);

            // Points are CW, beach section does not collapse so no event
            if (orientation >= -2e-12) return;

            float lLengthSquared = lx * lx + ly * ly;
            float rLengthSquared = rx * rx + ry * ry;
            float x = (ry * lLengthSquared - ly * rLengthSquared) / orientation;
            float y = (lx * rLengthSquared - rx * lLengthSquared) / orientation;
            float yCenter = y + cy; // at or below sweepline

            // Get circle event to setup
            CircleEvent circleEvent = null;
            if (usedCircleEvents.Count > 0) {
                circleEvent = usedCircleEvents.Last();
                usedCircleEvents.Remove(circleEvent);
            }
            else circleEvent = new CircleEvent();

            // Update fields
            circleEvent.site = center.site;
            circleEvent.arc = center;
            circleEvent.setPosition(x + cx, yCenter + Mathf.Sqrt(x * x + y * y), yCenter);
            center.circleEvent = circleEvent;

            // Add to event queue
            sweepLine.Enqueue(circleEvent, circleEvent);
        }

        /* Function: deleteBeachArc
         * ------------------------
         * Deletes an arc and associated circle event from the beach line.
         */
        private void deleteBeachArc(BeachArc arc) {
            deleteCircleEvent(arc);
            beachLine.Remove(arc);
            usedBeachArcs.Add(arc);
        }

        /* Function: findNewVertex
         * -----------------------
         * Find Veronoi vertex at center of circle with left, new and middle beach sections
         * on its perimeter. Vertex arises on fusion of two breakpoints.
         * 
         * Note: Calculation of new vertex based on Jeremie St-Amand's:
         * https://github.com/jesta88/Unity-Voronoi/blob/master/Assets/Voronoi/FortuneVoronoi.cs
         */
        private Vertex findNewVertex(Site newArcSite, Site leftArcSite, Site rightArcSite) {
            float ax = leftArcSite.x;
            float ay = leftArcSite.y;
            float bx = newArcSite.x - ax;
            float by = newArcSite.y - ay;
            float cx = rightArcSite.x - ax;
            float cy = rightArcSite.y - ay;
            float d = 2 * (bx * cy - by * cx);
            float hb = bx * bx + by * by;
            float hc = cx * cx + cy * cy;
            return new Vertex((cy * hb - by * hc) / d + ax, (bx * hc - cx * hb) / d + ay);
        }

        /* Function: handleCircleEvent
         * ---------------------------
         * Handles circle event.
         */
        private void handleCircleEvent(BeachArc arc) {
            CircleEvent circleEvent = arc.circleEvent;
            Vertex center = new Vertex(circleEvent.x, circleEvent.yCircleCenter);
            BeachArc prev = arc.Prev;
            BeachArc next = arc.Next;

            LinkedList<BeachArc> disappearingTransitions = new LinkedList<BeachArc>();
            disappearingTransitions.AddLast(arc);

            deleteBeachArc(arc);

            // Handle collapse left
            BeachArc leftArc = prev;
            while (leftArc.circleEvent != null &&  Mathf.Abs(center.x - leftArc.circleEvent.x) < EPS &&
                Mathf.Abs(center.y - leftArc.circleEvent.yCircleCenter) < EPS) {
                prev = leftArc.Prev;
                disappearingTransitions.AddFirst(leftArc);
                deleteBeachArc(leftArc);
                leftArc = prev;
            }

            // New left arc not dissappearing, but used in edge updates
            disappearingTransitions.AddFirst(leftArc);
            deleteCircleEvent(leftArc);

            // Hanlde collapse right
            BeachArc rightArc = next;
            while (rightArc.circleEvent != null && Mathf.Abs(center.x - rightArc.circleEvent.x) < EPS && 
                Mathf.Abs(center.y - rightArc.circleEvent.yCircleCenter) < EPS) {
                next = rightArc.Next;
                disappearingTransitions.AddLast(rightArc);
                deleteBeachArc(rightArc);
                rightArc = next;
            }

            // New right arc not dissappearing, but used in edge updates
            disappearingTransitions.AddLast(rightArc);
            deleteCircleEvent(rightArc);

            // Link existing edges at start point    
            int nArcs = disappearingTransitions.Count;
            for (int i = 1; i < nArcs; i++) {
                rightArc = disappearingTransitions.ElementAt(i);
                leftArc = disappearingTransitions.ElementAt(i - 1);
                rightArc.edge.setStartVertex(leftArc.site, rightArc.site, center);
            }

            // Create new edge between previously non-adjacent arcs
            // New vertex defines end point relative to site on the left
            leftArc = disappearingTransitions.ElementAt(0);
            rightArc = disappearingTransitions.ElementAt(nArcs - 1);
            rightArc.edge = createNewEdge(leftArc.site, rightArc.site, null, center);

            // Update circle events for arcs
            findPotentialCircleEvent(leftArc);
            findPotentialCircleEvent(rightArc);
        }

        /* Function: clipEdgesOnBounds
         * ---------------------------
         * Clips edges that go to inifinity or invalid edges after 
         * no more events on the diagram bounds.
         */
        private void clipEdgesOnBounds() {
            // Loop bakcwards for safe splicing
            for (int i = edges.Count - 1; i >= 0; i--) {
                Edge e = edges[i];
                // Remove edge if outside bounds or isn't line
                if (!connectEdge(e) || !clipEdge(e) ||
                    ((Mathf.Abs(e.v1.x - e.v2.x) < EPS && Mathf.Abs(e.v1.y - e.v2.y) < EPS))) {
                    e.v1 = e.v2 = null;
                    edges.Remove(e);
                }
            }
        }

        /* Function: connectEdge
         * ---------------------
         * Attempts to connect dangling egdes. Boolean decision on 
         * success of connection.
         */
        private bool connectEdge(Edge edge) {
            // Already connected, done
            Vertex v2 = edge.v2;
            if (v2) return true;

            // Local copy for performance
            Vertex v1 = edge.v1;
            float xBoundsMin = boundingBox.min.x;
            float xBoundsMax = boundingBox.max.x;
            float zBoundsMin = boundingBox.min.z;
            float zBoundsMax = boundingBox.max.z;
            Site leftSite = edge.s1;
            Site rightSite = edge.s2;
            float lx = leftSite.x;
            float ly = leftSite.y;
            float rx = rightSite.x;
            float ry = rightSite.y;
            float middleX = (lx + rx) / 2;
            float middleY = (ly + ry) / 2;
            float middleSlope = float.NaN;
            float middleOffset = 0.0f;

            // Edge removed or connected to bounding box -> close cells
            cells[leftSite.id].needToClose = true;
            cells[rightSite.id].needToClose = true;

            // Get bisector line
            if (ry != ly) {
                middleSlope = (lx - rx) / (ry - ly);
                middleOffset = middleY - middleSlope * middleX;
            }

            // Note: Closing conditions based on Jeremie St-Amand's:
            // https://github.com/jesta88/Unity-Voronoi/blob/master/Assets/Voronoi/FortuneVoronoi.cs
            // Direction of line (relative to left site):
            // upward: left.x < right.x
            // downward: left.x > right.x
            // horizontal: left.x == right.x
            // upward: left.x < right.x
            // rightward: left.y < right.y
            // leftward: left.y > right.y
            // vertical: left.y == right.y

            // Vertical line
            if (float.IsNaN(middleSlope)) {
                if (middleX < xBoundsMin || middleX >= xBoundsMax) return false; // doesn't intersect viewport
                if (lx > rx) { // downward
                    if (v1 == null || v1.y < zBoundsMin) v1 = new Vertex(middleX, zBoundsMin);
                    else if (v1.y >= zBoundsMax) return false;
                    v2 = new Vertex(middleX, zBoundsMax);
                } else { // upward
                    if (v1 == null || v1.y > zBoundsMax) v1 = new Vertex(middleX, zBoundsMax);
                    else if (v1.y < zBoundsMin) return false;
                    v2 = new Vertex(middleX, zBoundsMin);
                }
            }
            // Almost vertical (more than horizontal), connect to top/bottom of bounding box
            else if (middleSlope < -1 || middleSlope > 1) {
                if (lx > rx) { // downward
                    if (v1 == null || v1.y < zBoundsMin) v1 = new Vertex((zBoundsMin - middleOffset) / middleSlope, zBoundsMin);
                    else if (v1.y >= zBoundsMax) return false;
                    v2 = new Vertex((zBoundsMax - middleOffset) / middleSlope, zBoundsMax);
                } else { // upward
                    if (v1 == null || v1.y > zBoundsMax) v1 = new Vertex((zBoundsMax - middleOffset) / middleSlope, zBoundsMax);
                    else if (v1.y < zBoundsMin) return false;
                    v2 = new Vertex((zBoundsMin - middleOffset) / middleSlope, zBoundsMin);
                }
            }
            // Almost horizontal (more than vertical), connect to left/right of bounding box
            else {
                if (ly < ry) { //right
                    if (v1 == null || v1.x < xBoundsMin) v1 = new Vertex(xBoundsMin, middleSlope * xBoundsMin + middleOffset);
                    else if (v1.x >= xBoundsMax) return false;
                    v2 = new Vertex(xBoundsMax, middleSlope * xBoundsMax + middleOffset);
                } else { // left
                    if (v1 == null || v1.x > xBoundsMax) v1 = new Vertex(xBoundsMax, middleSlope * xBoundsMax + middleOffset);
                    else if (v1.x < xBoundsMin) return false;
                    v2 = new Vertex(xBoundsMin, middleSlope * xBoundsMin + middleOffset);
                }
            }
            edge.v1 = v1;
            edge.v2 = v2;
            return true;
        }

        /* Function: clipEdge
         * ------------------
         * Line clip edge.
         * 
         * Note: Clipping conditions based on Jeremie St-Amand's:
         * https://github.com/jesta88/Unity-Voronoi/blob/master/Assets/Voronoi/FortuneVoronoi.cs
         */
        private bool clipEdge(Edge edge) {   
            // Local copy for performance
            float v1x = edge.v1.x;
            float v1y = edge.v1.y;
            float v2x = edge.v2 != null ? edge.v2.x : float.NaN;
            float v2y = edge.v2 != null ? edge.v2.y : float.NaN;
            float t0 = 0;
            float t1 = 1;
            float dx = v2x - v1x;
            float dy = v2y - v1y;

            // Left
            float diff = v1x - boundingBox.min.x;
            if (dx == 0 && diff < 0) return false;
            float r = -diff / dx;
            if (dx < 0) {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            } else if (dx > 0) {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            // Right
            diff = boundingBox.max.x - v1x;
            if (dx == 0 && diff < 0) return false;
            r = diff / dx;
            if (dx < 0) {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            } else if (dx > 0) {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }
            // Top
            diff = v1y - boundingBox.min.z;
            if (dy == 0 && diff < 0) return false;
            r = -diff / dy;
            if (dy < 0) {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }  else if (dy > 0) {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            // Bottom        
            diff = boundingBox.max.z - v1y;
            if (dy == 0 && diff < 0) return false;
            r = diff / dy;
            if (dy < 0) {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            } else if (dy > 0) {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }

            // Edge is within bounding box
            // Update v1
            if (t0 > 0) edge.v1 = new Vertex(v1x + t0 * dx, v1y + t0 * dy);
            // Update v2
            if (t1 < 1) edge.v2 = new Vertex(v1x + t1 * dx, v1y + t1 * dy);

            // Vertex v1/v2 maybe clipped, close cells
            if (t0 > 0 || t1 < 1) {
                cells[edge.s1.id].needToClose = true;
                cells[edge.s2.id].needToClose = true;
            }
            return true;
        }

        /* Function: closeCells
        * --------------------
        * Closes off all cells by pruning edges, ordering them and 
        * adding missing ones as necesaary.
        * 
        * Note: Closing conditions based on Jeremie St-Amand's:
        * https://github.com/jesta88/Unity-Voronoi/blob/master/Assets/Voronoi/FortuneVoronoi.cs
        */
        private void closeCells() {
            float xbMin = boundingBox.min.x;
            float xbMax = boundingBox.max.x;
            float zbMin = boundingBox.min.z;
            float zbMax = boundingBox.max.z;

            // Loop over cells cheking for closure bool
            int numClosingErrors = 0;
            for (int i = cells.Count - 1; i >= 0; i--) {
                VoronoiCell cell = cells[i];

                if (cell.pruneSortEdges() <= 0 || !cell.needToClose) continue;
                int nHalfedges = cell.halfEdges.Count;

                // Find 'unclosed' vertex, i.e. end of half-edge doesn't 
                // match following edge start point 
                int numClosed = 0;
                while (numClosed < nHalfedges) {
                    Vertex vStart = cell.halfEdges[numClosed].getEndVertex();
                    Vertex vEnd = cell.halfEdges[(numClosed + 1) % nHalfedges].getStartVertex();

                    closeCellsHelper(ref cell, ref vStart, ref vEnd, ref numClosed, ref nHalfedges,
                        ref numClosingErrors, xbMin, xbMax, zbMin, zbMax);

                    numClosed++;
                }
                cell.needToClose = false;
            }
            // Set diagram valid status
            if (numClosingErrors == 0) closingErrors = false;
            else closingErrors = true;
        }

        /* Function: createSimpleEdge
         * --------------------------
         * Close cells conditional helper. Finds unclosed vertices or ends of 
         * half-edges that don't match the start point of the next half-edge, 
         * then closes the cell.
         */
        private void closeCellsHelper(ref VoronoiCell cell, ref Vertex vStart, ref Vertex vEnd, ref int numClosed,
            ref int nHalfedges, ref int numClosingErrors, float xbMin, float xbMax, float zbMin, float zbMax) {

            // End != Start? add missing half-edge to close cell
            if ((Mathf.Abs(vStart.x - vEnd.x) >= EPS || Mathf.Abs(vStart.y - vEnd.y) >= EPS)) {
                bool lastBorderSegment = false;
                Vertex v2;
                Edge edge;

                // Check down on left side
                if (equalWithEps(vStart.x, xbMin) && lessThanWithEps(vStart.y, zbMax)) {
                    lastBorderSegment = equalWithEps(vEnd.x, xbMin);
                    v2 = new Vertex(xbMin, lastBorderSegment ? vEnd.y : zbMax);
                    edge = createSimpleEdge(cell.site, vStart, v2);
                    numClosed++;
                    cell.halfEdges.Insert(numClosed, new HalfEdge(edge, cell.site, null));
                    nHalfedges++;
                    if (!lastBorderSegment) vStart = v2;
                }
                // Check right on bottom side
                if (!lastBorderSegment && equalWithEps(vStart.y, zbMax) && lessThanWithEps(vStart.x, xbMax)) {
                    lastBorderSegment = equalWithEps(vEnd.y, zbMax);
                    v2 = new Vertex(lastBorderSegment ? vEnd.x : xbMax, zbMax);
                    edge = createSimpleEdge(cell.site, vStart, v2);
                    numClosed++;
                    cell.halfEdges.Insert(numClosed, new HalfEdge(edge, cell.site, null));
                    nHalfedges++;
                    if (!lastBorderSegment) vStart = v2;
                }
                // Check up on right side
                if (!lastBorderSegment && equalWithEps(vStart.x, xbMax) && greaterThanWithEps(vStart.y, zbMin)) {
                    lastBorderSegment = equalWithEps(vEnd.x, xbMax);
                    v2 = new Vertex(xbMax, lastBorderSegment ? vEnd.y : zbMin);
                    edge = createSimpleEdge(cell.site, vStart, v2);
                    numClosed++;
                    cell.halfEdges.Insert(numClosed, new HalfEdge(edge, cell.site, null));
                    nHalfedges++;
                    if (!lastBorderSegment) vStart = v2;
                }
                // Check left on top side
                if (!lastBorderSegment && equalWithEps(vStart.y, zbMin) && greaterThanWithEps(vStart.x, xbMin)) {
                    lastBorderSegment = equalWithEps(vEnd.y, zbMin);
                    v2 = new Vertex(lastBorderSegment ? vEnd.x : xbMin, zbMin);
                    edge = createSimpleEdge(cell.site, vStart, v2);
                    numClosed++;

                    cell.halfEdges.Insert(numClosed, new HalfEdge(edge, cell.site, null));
                    nHalfedges++;
                    if (!lastBorderSegment) vStart = v2;
                }
                // Check down on left side
                if (!lastBorderSegment) {
                    lastBorderSegment = equalWithEps(vEnd.x, xbMin);
                    v2 = new Vertex(xbMin, lastBorderSegment ? vEnd.y : zbMax);
                    edge = createSimpleEdge(cell.site, vStart, v2);
                    numClosed++;
                    cell.halfEdges.Insert(numClosed, new HalfEdge(edge, cell.site, null));
                    nHalfedges++;
                    if (!lastBorderSegment) vStart = v2;
                }
                // Check right on bottom side
                if (!lastBorderSegment) {
                    lastBorderSegment = equalWithEps(vEnd.y, zbMax);
                    v2 = new Vertex(lastBorderSegment ? vEnd.x : xbMax, zbMax);
                    edge = createSimpleEdge(cell.site, vStart, v2);
                    numClosed++;
                    cell.halfEdges.Insert(numClosed, new HalfEdge(edge, cell.site, null));
                    nHalfedges++;
                    if (!lastBorderSegment) vStart = v2;
                }
                // Check up on right side
                if (!lastBorderSegment) {
                    lastBorderSegment = equalWithEps(vEnd.x, xbMax);
                    v2 = new Vertex(xbMax, lastBorderSegment ? vEnd.y : zbMin);
                    edge = createSimpleEdge(cell.site, vStart, v2);
                    numClosed++;
                    cell.halfEdges.Insert(numClosed, new HalfEdge(edge, cell.site, null));
                    nHalfedges++;
                }
                // Check for errors
                if (!lastBorderSegment) {
                    Debug.LogError("Error closing cell.");
                    numClosingErrors++;
                }
            }
        }

        /* Function: createSimpleEdge
         * --------------------------
         * Returns edge for provided site at given vertices.
         */
        private Edge createSimpleEdge(Site site, Vertex v1, Vertex v2) {
            Edge edge = new Edge(site);
            edge.v1 = v1;
            edge.v2 = v2;
            edges.Add(edge);
            return edge;
        }

        /* Function: isDuplicate
         * ---------------------
         * Returns boolean decision that site has been found already.
         */
        private bool isDuplicate(Event e, float prevX, float prevY) {
            return (e.site.x == prevX && e.site.y == prevY);
        }

        /* Method: getClosingErrors
         * ------------------------
         * Returns whether there were errors closing cells.
         */
        public bool getClosingErrors() {
            return closingErrors;
        }

        // Helpers for float comparison accuracy
        // Sourced from: https://github.com/jesta88/Unity-Voronoi/blob/master/Assets/Voronoi/FortuneVoronoi.cs
        public bool equalWithEps(float a, float b) { return Mathf.Abs(a - b) < EPS; }
        public bool greaterThanWithEps(float a, float b) { return a - b > EPS; }
        public bool lessThanWithEps(float a, float b) { return b - a > EPS; }
    }
}