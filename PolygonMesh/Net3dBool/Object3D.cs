﻿/*
The MIT License (MIT)

Copyright (c) 2014 Sebastian Loncar

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

See:
D. H. Laidlaw, W. B. Trumbore, and J. F. Hughes.
"Constructive Solid Geometry for Polyhedral Objects"
SIGGRAPH Proceedings, 1986, p.161.

original author: Danilo Balby Silva Castanheira (danbalby@yahoo.com)

Ported from Java to C# by Sebastian Loncar, Web: http://loncar.de
Optimized and refactored by: Lars Brubaker (larsbrubaker@matterhackers.com)
Project: https://github.com/MatterHackers/agg-sharp (an included library)
*/

using MatterHackers.VectorMath;
using System;
using System.Collections.Generic;

namespace Net3dBool
{
	/// <summary>
	/// Data structure about a 3d solid to apply boolean operations in it.
	/// Tipically, two 'Object3d' objects are created to apply boolean operation. The
	/// methods splitFaces() and classifyFaces() are called in this sequence for both objects,
	/// always using the other one as parameter.Then the faces from both objects are collected
	/// according their status.
	/// </summary>
	public class Object3D
	{
		/// <summary>
		/// tolerance value to test equalities
		/// </summary>
		private readonly static double EqualityTolerance = 1e-10f;
		/// <summary>
		/// object representing the solid extremes
		/// </summary>
		private Bound bound;
		/// <summary>
		/// solid faces
		/// </summary>
		private List<Face> faces;
		/// <summary>
		/// solid vertices
		/// </summary>
		private List<Vertex> vertices;

		/// <summary>
		/// Constructs a Object3d object based on a solid file.
		/// </summary>
		/// <param name="solid">solid used to construct the Object3d object</param>
		public Object3D(Solid solid)
		{
			Vertex v1, v2, v3, vertex;
			Vector3[] verticesPoints = solid.getVertices();
			int[] indices = solid.getIndices();
			var verticesTemp = new List<Vertex>();

			//create vertices
			vertices = new List<Vertex>();
			for (int i = 0; i < verticesPoints.Length; i++)
			{
				vertex = AddVertex(verticesPoints[i], Status.UNKNOWN);
				verticesTemp.Add(vertex);
			}

			//create faces
			faces = new List<Face>();
			for (int i = 0; i < indices.Length; i = i + 3)
			{
				v1 = verticesTemp[indices[i]];
				v2 = verticesTemp[indices[i + 1]];
				v3 = verticesTemp[indices[i + 2]];
				AddFace(v1, v2, v3);
			}

			//create bound
			bound = new Bound(verticesPoints);
		}

		/// <summary>
		/// Default constructor
		/// </summary>
		private Object3D()
		{
		}

		/// <summary>
		/// Classify faces as being inside, outside or on boundary of other object
		/// </summary>
		/// <param name="otherObject">object 3d used for the comparison</param>
		public void ClassifyFaces(Object3D otherObject)
		{
			//calculate adjacency information
			Face face;
			for (int i = 0; i < this.GetNumFaces(); i++)
			{
				face = this.GetFace(i);
				face.v1.AddAdjacentVertex(face.v2);
				face.v1.AddAdjacentVertex(face.v3);
				face.v2.AddAdjacentVertex(face.v1);
				face.v2.AddAdjacentVertex(face.v3);
				face.v3.AddAdjacentVertex(face.v1);
				face.v3.AddAdjacentVertex(face.v2);
			}

			//for each face
			for (int i = 0; i < GetNumFaces(); i++)
			{
				face = GetFace(i);

				//if the face vertices aren't classified to make the simple classify
				if (face.SimpleClassify() == false)
				{
					//makes the ray trace classification
					face.RayTraceClassify(otherObject);

					//mark the vertices
					if (face.v1.GetStatus() == Status.UNKNOWN)
					{
						face.v1.Mark(face.GetStatus());
					}
					if (face.v2.GetStatus() == Status.UNKNOWN)
					{
						face.v2.Mark(face.GetStatus());
					}
					if (face.v3.GetStatus() == Status.UNKNOWN)
					{
						face.v3.Mark(face.GetStatus());
					}
				}
			}
		}

		/// <summary>
		/// Clones the Object3D object
		/// </summary>
		/// <returns>cloned object</returns>
		public Object3D Clone()
		{
			Object3D clone = new Object3D();
			clone.vertices = new List<Vertex>();
			for (int i = 0; i < vertices.Count; i++)
			{
				clone.vertices.Add(vertices[i].Clone());
			}
			clone.faces = new List<Face>();
			for (int i = 0; i < vertices.Count; i++)
			{
				clone.faces.Add(faces[i].Clone());
			}
			clone.bound = bound;

			return clone;
		}

		/// <summary>
		/// Gets the solid bound
		/// </summary>
		/// <returns>solid bound</returns>
		public Bound GetBound()
		{
			return bound;
		}

		/// <summary>
		/// Gets a face reference for a given position
		/// </summary>
		/// <param name="index">required face position</param>
		/// <returns>face reference , null if the position is invalid</returns>
		public Face GetFace(int index)
		{
			if (index < 0 || index >= faces.Count)
			{
				return null;
			}
			else
			{
				return faces[index];
			}
		}

		/// <summary>
		/// Gets the number of faces
		/// </summary>
		/// <returns>number of faces</returns>
		public int GetNumFaces()
		{
			return faces.Count;
		}

		/// <summary>
		/// Inverts faces classified as INSIDE, making its normals point outside. Usually used into the second solid when the difference is applied.
		/// </summary>
		public void InvertInsideFaces()
		{
			Face face;
			for (int i = 0; i < GetNumFaces(); i++)
			{
				face = GetFace(i);
				if (face.GetStatus() == Status.INSIDE)
				{
					face.Invert();
				}
			}
		}

		/// <summary>
		/// Split faces so that none face is intercepted by a face of other object
		/// </summary>
		/// <param name="compareObject">the other object 3d used to make the split</param>
		public void SplitFaces(Object3D compareObject)
		{
			Line line;
			Face thisFace, compareFace;
			Segment segment1;
			Segment segment2;
			double v1DistToCompareFace, distFace1Vert2, distFace1Vert3, distFace2Vert1, distFace2Vert2, distFace2Vert3;
			int signFace1Vert1, signFace1Vert2, signFace1Vert3, signFace2Vert1, signFace2Vert2, signFace2Vert3;
			int numFacesBefore = this.GetNumFaces();
			int numFacesStart = this.GetNumFaces();

			//if the objects bounds overlap...
			if (this.GetBound().Overlap(compareObject.GetBound()))
			{
				//for each object1 face...
				for (int thisFaceIndex = 0; thisFaceIndex < this.GetNumFaces(); thisFaceIndex++)
				{
					//if object1 face bound and object2 bound overlap ...
					thisFace = GetFace(thisFaceIndex);

					if (thisFace.GetBound().Overlap(compareObject.GetBound()))
					{
						//for each object2 face...
						for (int compareFaceIndex = 0; compareFaceIndex < compareObject.GetNumFaces(); compareFaceIndex++)
						{
							//if object1 face bound and object2 face bound overlap...
							compareFace = compareObject.GetFace(compareFaceIndex);
							if (thisFace.GetBound().Overlap(compareFace.GetBound()))
							{
								//PART I - DO TWO POLIGONS INTERSECT?
								//POSSIBLE RESULTS: INTERSECT, NOT_INTERSECT, COPLANAR

								//distance from the face1 vertices to the face2 plane
								v1DistToCompareFace = ComputeDistance(thisFace.v1, compareFace);
								distFace1Vert2 = ComputeDistance(thisFace.v2, compareFace);
								distFace1Vert3 = ComputeDistance(thisFace.v3, compareFace);

								//distances signs from the face1 vertices to the face2 plane
								signFace1Vert1 = (v1DistToCompareFace > EqualityTolerance ? 1 : (v1DistToCompareFace < -EqualityTolerance ? -1 : 0));
								signFace1Vert2 = (distFace1Vert2 > EqualityTolerance ? 1 : (distFace1Vert2 < -EqualityTolerance ? -1 : 0));
								signFace1Vert3 = (distFace1Vert3 > EqualityTolerance ? 1 : (distFace1Vert3 < -EqualityTolerance ? -1 : 0));

								//if all the signs are zero, the planes are coplanar
								//if all the signs are positive or negative, the planes do not intersect
								//if the signs are not equal...
								if (!(signFace1Vert1 == signFace1Vert2 && signFace1Vert2 == signFace1Vert3))
								{
									//distance from the face2 vertices to the face1 plane
									distFace2Vert1 = ComputeDistance(compareFace.v1, thisFace);
									distFace2Vert2 = ComputeDistance(compareFace.v2, thisFace);
									distFace2Vert3 = ComputeDistance(compareFace.v3, thisFace);

									//distances signs from the face2 vertices to the face1 plane
									signFace2Vert1 = (distFace2Vert1 > EqualityTolerance ? 1 : (distFace2Vert1 < -EqualityTolerance ? -1 : 0));
									signFace2Vert2 = (distFace2Vert2 > EqualityTolerance ? 1 : (distFace2Vert2 < -EqualityTolerance ? -1 : 0));
									signFace2Vert3 = (distFace2Vert3 > EqualityTolerance ? 1 : (distFace2Vert3 < -EqualityTolerance ? -1 : 0));

									//if the signs are not equal...
									if (!(signFace2Vert1 == signFace2Vert2 && signFace2Vert2 == signFace2Vert3))
									{
										line = new Line(thisFace, compareFace);

										//intersection of the face1 and the plane of face2
										segment1 = new Segment(line, thisFace, signFace1Vert1, signFace1Vert2, signFace1Vert3);

										//intersection of the face2 and the plane of face1
										segment2 = new Segment(line, compareFace, signFace2Vert1, signFace2Vert2, signFace2Vert3);

										//if the two segments intersect...
										if (segment1.Intersect(segment2))
										{
											//PART II - SUBDIVIDING NON-COPLANAR POLYGONS
											int lastNumFaces = GetNumFaces();
											bool splitOccured = this.SplitFace(thisFaceIndex, segment1, segment2);

											//prevent from infinite loop (with a loss of faces...)
											if (GetNumFaces() > numFacesStart * 100)
											{
												//System.out.println("possible infinite loop situation: terminating faces split");
												//return;
												int a = 0;
											}

											//if the face in the position isn't the same, there was a break
											if (splitOccured
												&& thisFace != GetFace(thisFaceIndex))
											{
												//if the generated solid is equal the origin...
												if (thisFace.Equals(GetFace(GetNumFaces() - 1)))
												{
													//return it to its position and jump it
													if (thisFaceIndex != (GetNumFaces() - 1))
													{
														faces.RemoveAt(GetNumFaces() - 1);
														faces.Insert(thisFaceIndex, thisFace);
													}
													else
													{
														continue;
													}
												}
												//else: test next face
												else
												{
													thisFaceIndex--;
													break;
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Method used to add a face properly for internal methods
		/// </summary>
		/// <param name="v1">a face vertex</param>
		/// <param name="v2">a face vertex</param>
		/// <param name="v3">a face vertex</param>
		/// <returns></returns>
		private Face AddFace(Vertex v1, Vertex v2, Vertex v3)
		{
			if (!(v1.Equals(v2) || v1.Equals(v3) || v2.Equals(v3)))
			{
				Face face = new Face(v1, v2, v3);
				if (face.GetArea() > EqualityTolerance)
				{
					faces.Add(face);
					return face;
				}
				else
				{
					return null;
				}
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Method used to add a vertex properly for internal methods
		/// </summary>
		/// <param name="pos">vertex position</param>
		/// <param name="status">vertex status</param>
		/// <returns>The vertex inserted (if a similar vertex already exists, this is returned).</returns>
		private Vertex AddVertex(Vector3 pos, Status status)
		{
			Vertex vertex = new Vertex(pos, status);
			for (int i = 0; i < vertices.Count; i++)
			{
				if (vertex.Equals(vertices[i]))
				{
					// if the vertex is already there, it is not inserted
					vertex = vertices[i];
					vertex.SetStatus(status);
					return vertex;
				}
			}

			vertices.Add(vertex);
			return vertex;
		}

		/// <summary>
		/// Face breaker for FACE-FACE-FACE
		/// </summary>
		/// <param name="faceIndex">face index in the faces array</param>
		/// <param name="facePos1">new vertex position</param>
		/// <param name="facePos2">new vertex position</param>
		/// <param name="linedVertex">linedVertex what vertex is more lined with the interersection found</param>
		private bool BreakFaceInFive(int faceIndex, Vector3 facePos1, Vector3 facePos2, int linedVertex)
		{
			//       O
			//      - -
			//     -   -
			//    -     -
			//   -    X  -
			//  -  X      -
			// O-----------O
			Face face = faces[faceIndex];
			faces.RemoveAt(faceIndex);

			Vertex faceVertex1 = AddVertex(facePos1, Status.BOUNDARY);
			bool faceVertex1Exists = faceVertex1 != vertices[vertices.Count - 1];

			Vertex faceVertex2 = AddVertex(facePos2, Status.BOUNDARY);
			bool faceVertex2Exists = faceVertex2 != vertices[vertices.Count - 1];

			if (faceVertex1Exists && faceVertex2Exists)
			{
				vertices.RemoveAt(vertices.Count - 1);
				vertices.RemoveAt(vertices.Count - 1);
				return false;
			}

			if (linedVertex == 1)
			{
				AddFace(face.v2, face.v3, faceVertex1);
				AddFace(face.v2, faceVertex1, faceVertex2);
				AddFace(face.v3, faceVertex2, faceVertex1);
				AddFace(face.v2, faceVertex2, face.v1);
				AddFace(face.v3, face.v1, faceVertex2);
			}
			else if (linedVertex == 2)
			{
				AddFace(face.v3, face.v1, faceVertex1);
				AddFace(face.v3, faceVertex1, faceVertex2);
				AddFace(face.v1, faceVertex2, faceVertex1);
				AddFace(face.v3, faceVertex2, face.v2);
				AddFace(face.v1, face.v2, faceVertex2);
			}
			else
			{
				AddFace(face.v1, face.v2, faceVertex1);
				AddFace(face.v1, faceVertex1, faceVertex2);
				AddFace(face.v2, faceVertex2, faceVertex1);
				AddFace(face.v1, faceVertex2, face.v3);
				AddFace(face.v2, face.v3, faceVertex2);
			}

			return true;
		}

		/// <summary>
		/// Face breaker for EDGE-FACE-FACE / FACE-FACE-EDGE
		/// </summary>
		/// <param name="faceIndex">face index in the faces array</param>
		/// <param name="edgePos">new vertex position</param>
		/// <param name="facePos">new vertex position</param>
		/// <param name="endVertex">vertex used for the split</param>
		private bool BreakFaceInFour(int faceIndex, Vector3 edgePos, Vector3 facePos, Vertex endVertex)
		{
			//         2
			//        -*-
			//       - * -
			//      -  *  E
			//     -   * * -
			//    -    F    -
			//   -   *   *   -
			//  - *         * -
			// 3---------------1
			Face face = faces[faceIndex];

			Vertex edgeVertex = AddVertex(edgePos, Status.BOUNDARY);
			bool edgeExists = edgeVertex != vertices[vertices.Count - 1];
			Vertex faceVertex = AddVertex(facePos, Status.BOUNDARY);
			bool faceExists = faceVertex != vertices[vertices.Count - 1];

			if (faceExists && edgeExists)
			{
				vertices.RemoveAt(vertices.Count - 1);
				vertices.RemoveAt(vertices.Count - 1);
				return false;
			}

			faces.RemoveAt(faceIndex);
			// check that we are not adding back in the same face we are removing
			if (endVertex.Equals(face.v1))
			{
				AddFace(face.v1, edgeVertex, faceVertex);
				AddFace(edgeVertex, face.v2, faceVertex);
				AddFace(face.v2, face.v3, faceVertex);
				AddFace(face.v3, face.v1, faceVertex);
			}
			else if (endVertex.Equals(face.v2))
			{
				AddFace(face.v2, edgeVertex, faceVertex);
				AddFace(edgeVertex, face.v3, faceVertex);
				AddFace(face.v3, face.v1, faceVertex);
				AddFace(face.v1, face.v2, faceVertex);
			}
			else
			{
				AddFace(face.v3, edgeVertex, faceVertex);
				AddFace(edgeVertex, face.v1, faceVertex);
				AddFace(face.v1, face.v2, faceVertex);
				AddFace(face.v2, face.v3, faceVertex);
			}

			return true;
		}

		/// <summary>
		/// Face breaker for EDGE-EDGE-EDGE
		/// </summary>
		/// <param name="faceIndex">face index in the faces array</param>
		/// <param name="newPos1">new vertex position</param>
		/// <param name="newPos2">new vertex position</param>
		/// <param name="splitEdge">edge that will be split</param>
		private bool BreakFaceInThree(int faceIndex, Vector3 newPos1, Vector3 newPos2, int splitEdge)
		{
			//       O
			//      - -
			//     -   X
			//    -  *  -
			//   - *   **X
			//  -* ****   -
			// X-----------O

			Vertex vertex1 = AddVertex(newPos1, Status.BOUNDARY);
			Vertex vertex2 = AddVertex(newPos2, Status.BOUNDARY);

			if (splitEdge == 1) // vertex 3
			{
				Face face = faces[faceIndex];
				bool willMakeExistingFace = (vertex1 == face.v1 && vertex2 == face.v2) || (vertex1 == face.v2 || vertex2 == face.v1);
				if (!willMakeExistingFace)
				{
					faces.RemoveAt(faceIndex);
					AddFace(face.v1, vertex1, face.v3);
					AddFace(vertex1, vertex2, face.v3);
					AddFace(vertex2, face.v2, face.v3);
					return true;
				}
			}
			else if (splitEdge == 2) // vertex 1
			{
				Face face = faces[faceIndex];
				bool willMakeExistingFace = (vertex1 == face.v2 && vertex2 == face.v3) || (vertex1 == face.v3 || vertex2 == face.v2);
				if (!willMakeExistingFace)
				{
					faces.RemoveAt(faceIndex);
					AddFace(face.v2, vertex1, face.v1);
					AddFace(vertex1, vertex2, face.v1);
					AddFace(vertex2, face.v3, face.v1);
					return true;
				}
			}
			else // vertex 2
			{
				Face face = faces[faceIndex];
				bool willMakeExistingFace = (vertex1 == face.v1 && vertex2 == face.v3) || (vertex1 == face.v3 || vertex2 == face.v1);
				if (!willMakeExistingFace)
				{
					faces.RemoveAt(faceIndex);
					AddFace(face.v3, vertex1, face.v2);
					AddFace(vertex1, vertex2, face.v2);
					AddFace(vertex2, face.v1, face.v2);
					return true;
				}
			}
			
			if(vertex2 == vertices[vertices.Count-1])
				vertices.RemoveAt(vertices.Count - 1);
			if (vertex1 == vertices[vertices.Count - 1])
				vertices.RemoveAt(vertices.Count - 1);
			return false;
		}

		/// <summary>
		/// Face breaker for VERTEX-FACE-FACE / FACE-FACE-VERTEX
		/// </summary>
		/// <param name="faceIndex">face index in the faces array</param>
		/// <param name="newPos">new vertex position</param>
		/// <param name="endVertex">vertex used for the split</param>
		private bool BreakFaceInThree(int faceIndex, Vector3 newPos)
		{
			//       O
			//      -*-
			//     - * -
			//    -  *  -
			//   -   X   -
			//  -  *   *  -
			// O-*--------*O
			Vertex vertex = AddVertex(newPos, Status.BOUNDARY);
			if (vertex != vertices[vertices.Count - 1]) // it is not new
			{
				// if the vertex we are adding is any of the existing vertices then don't add any
				return false;
			}

			Face face = faces[faceIndex];
			faces.RemoveAt(faceIndex);

			AddFace(face.v1, face.v2, vertex);
			AddFace(face.v2, face.v3, vertex);
			AddFace(face.v3, face.v1, vertex);

			return true;
		}

		/// <summary>
		/// Face breaker for EDGE-FACE-EDGE
		/// </summary>
		/// <param name="faceIndex">face index in the faces array</param>
		/// <param name="newPos1">new vertex position</param>
		/// <param name="newPos2">new vertex position</param>
		/// <param name="startVertex">vertex used for the new faces creation</param>
		/// <param name="endVertex">vertex used for the new faces creation</param>
		private bool BreakFaceInThree(int faceIndex, Vector3 newPos1, Vector3 newPos2, Vertex startVertex, Vertex endVertex)
		{
			//       O
			//      - -
			//     -   -
			//    -     -
			//   -       -
			//  -         -
			// O-----------O
			Face face = faces[faceIndex];
			faces.RemoveAt(faceIndex);

			Vertex vertex1 = AddVertex(newPos1, Status.BOUNDARY);
			Vertex vertex2 = AddVertex(newPos2, Status.BOUNDARY);

			if (startVertex.Equals(face.v1) && endVertex.Equals(face.v2))
			{
				AddFace(face.v1, vertex1, vertex2);
				AddFace(face.v1, vertex2, face.v3);
				AddFace(vertex1, face.v2, vertex2);
			}
			else if (startVertex.Equals(face.v2) && endVertex.Equals(face.v1))
			{
				AddFace(face.v1, vertex2, vertex1);
				AddFace(face.v1, vertex1, face.v3);
				AddFace(vertex2, face.v2, vertex1);
			}
			else if (startVertex.Equals(face.v2) && endVertex.Equals(face.v3))
			{
				AddFace(face.v2, vertex1, vertex2);
				AddFace(face.v2, vertex2, face.v1);
				AddFace(vertex1, face.v3, vertex2);
			}
			else if (startVertex.Equals(face.v3) && endVertex.Equals(face.v2))
			{
				AddFace(face.v2, vertex2, vertex1);
				AddFace(face.v2, vertex1, face.v1);
				AddFace(vertex2, face.v3, vertex1);
			}
			else if (startVertex.Equals(face.v3) && endVertex.Equals(face.v1))
			{
				AddFace(face.v3, vertex1, vertex2);
				AddFace(face.v3, vertex2, face.v2);
				AddFace(vertex1, face.v1, vertex2);
			}
			else
			{
				AddFace(face.v3, vertex2, vertex1);
				AddFace(face.v3, vertex1, face.v2);
				AddFace(vertex2, face.v1, vertex1);
			}

			return true;
		}

		/// <summary>
		/// Face breaker for VERTEX-EDGE-EDGE / EDGE-EDGE-VERTEX
		/// </summary>
		/// <param name="faceIndex">face index in the faces array</param>
		/// <param name="newPos">new vertex position</param>
		/// <param name="splitEdge">edge that will be split</param>
		private bool BreakFaceInTwo(int faceIndex, Vector3 newPos, int splitEdge)
		{
			//       O
			//      -*-
			//     - * -
			//    -  *  -
			//   -   *   -
			//  -    *    -
			// O-----X-----O
			Vertex vertex = AddVertex(newPos, Status.BOUNDARY);

			if (vertex != vertices[vertices.Count - 1])
			{
				// The added vertex is one of the existing vertices. So we would only add the same face we are removing.
				return false;
			}

			Face face = faces[faceIndex];
			faces.RemoveAt(faceIndex);

			if (splitEdge == 1)
			{
				AddFace(face.v1, vertex, face.v3);
				AddFace(vertex, face.v2, face.v3);
			}
			else if (splitEdge == 2)
			{
				if (!face.v3.Equals(vertex))
				{
					AddFace(face.v2, vertex, face.v1);
				}
				AddFace(vertex, face.v3, face.v1);
			}
			else
			{
				AddFace(face.v3, vertex, face.v2);
				AddFace(vertex, face.v1, face.v2);
			}

			return true;
		}

		/// <summary>
		/// Face breaker for VERTEX-FACE-EDGE / EDGE-FACE-VERTEX
		/// </summary>
		/// <param name="faceIndex">face index in the faces array</param>
		/// <param name="newPos">new vertex position</param>
		/// <param name="endVertex">vertex used for splitting</param>
		private bool BreakFaceInTwo(int faceIndex, Vector3 newPos, Vertex endVertex)
		{
			//       O
			//      - -
			//     -   -
			//    -     -
			//   -       -
			//  -         -
			// O-----------O

			// TODO: make sure we are not creating extra Vertices and not cleaning them up
			Face face = faces[faceIndex];
			Vertex vertex = AddVertex(newPos, Status.BOUNDARY);

			if (endVertex.Equals(face.v1))
			{
				// don't add it if it is the same as the face we have
				if (!face.v1.Equals(vertex)
					&& !face.v2.Equals(vertex))
				{
					AddFace(face.v1, vertex, face.v3);
					AddFace(vertex, face.v2, face.v3);
					faces.RemoveAt(faceIndex);
					return true;
				}
			}
			else if (endVertex.Equals(face.v2))
			{
				// don't add it if it is the same as the face we have
				if (!face.v2.Equals(vertex)
					&& !face.v3.Equals(vertex))
				{
					AddFace(face.v2, vertex, face.v1);
					AddFace(vertex, face.v3, face.v1);
					faces.RemoveAt(faceIndex);
					return true;
				}
			}
			else
			{
				// don't add it if it is the same as the face we have
				if (!face.v1.Equals(vertex)
					&& !face.v3.Equals(vertex))
				{
					AddFace(face.v3, vertex, face.v2);
					AddFace(vertex, face.v1, face.v2);
					faces.RemoveAt(faceIndex);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Computes closest distance from a vertex to a plane
		/// </summary>
		/// <param name="vertex">vertex used to compute the distance</param>
		/// <param name="face">face representing the plane where it is contained</param>
		/// <returns>the closest distance from the vertex to the plane</returns>
		private double ComputeDistance(Vertex vertex, Face face)
		{
			Vector3 normal = face.GetNormal();
			double distToV1 = face.GetPlane().distanceToPlaneFromOrigin;
			double distToVertex = Vector3.Dot(normal, vertex.Position);
			double distFromFacePlane = distToVertex - distToV1;
			return distFromFacePlane;
		}

		/// <summary>
		/// Split an individual face
		/// </summary>
		/// <param name="faceIndex">face index in the array of faces</param>
		/// <param name="segment1">segment representing the intersection of the face with the plane</param>
		/// <param name="segment2">segment representing the intersection of other face with the plane of the current face plane</param>
		private bool SplitFace(int faceIndex, Segment segment1, Segment segment2)
		{
			Vector3 startPos, endPos;
			int startType, endType, middleType;
			double startDist, endDist;

			Face face = GetFace(faceIndex);
			Vertex startVertex = segment1.GetStartVertex();
			Vertex endVertex = segment1.GetEndVertex();

			//starting point: deeper starting point
			if (segment2.StartDist > segment1.StartDist + EqualityTolerance)
			{
				startDist = segment2.StartDist;
				startType = segment1.GetIntermediateType();
				startPos = segment2.GetStartPosition();
			}
			else
			{
				startDist = segment1.StartDist;
				startType = segment1.GetStartType();
				startPos = segment1.GetStartPosition();
			}

			//ending point: deepest ending point
			if (segment2.GetEndDistance() < segment1.GetEndDistance() - EqualityTolerance)
			{
				endDist = segment2.GetEndDistance();
				endType = segment1.GetIntermediateType();
				endPos = segment2.GetEndPosition();
			}
			else
			{
				endDist = segment1.GetEndDistance();
				endType = segment1.GetEndType();
				endPos = segment1.GetEndPosition();
			}
			middleType = segment1.GetIntermediateType();

			if (startType == Segment.VERTEX)
			{
				//set vertex to BOUNDARY if it is start type
				startVertex.SetStatus(Status.BOUNDARY);
			}

			if (endType == Segment.VERTEX)
			{
				//set vertex to BOUNDARY if it is end type
				endVertex.SetStatus(Status.BOUNDARY);
			}

			if (startType == Segment.VERTEX && endType == Segment.VERTEX)
			{
				//VERTEX-_______-VERTEX
				return false;
			}
			else if (middleType == Segment.EDGE)
			{
				//______-EDGE-______
				//gets the edge
				int splitEdge;
				if ((startVertex == face.v1 && endVertex == face.v2) || (startVertex == face.v2 && endVertex == face.v1))
				{
					splitEdge = 1;
				}
				else if ((startVertex == face.v2 && endVertex == face.v3) || (startVertex == face.v3 && endVertex == face.v2))
				{
					splitEdge = 2;
				}
				else
				{
					splitEdge = 3;
				}

				if (startType == Segment.VERTEX)
				{
					//VERTEX-EDGE-EDGE
					return BreakFaceInTwo(faceIndex, endPos, splitEdge);
				}
				else if (endType == Segment.VERTEX)
				{
					//EDGE-EDGE-VERTEX
					return BreakFaceInTwo(faceIndex, startPos, splitEdge);
				}
				else if (startDist == endDist)
				{
					// EDGE-EDGE-EDGE
					return BreakFaceInTwo(faceIndex, endPos, splitEdge);
				}
				else
				{
					if ((startVertex == face.v1 && endVertex == face.v2) || (startVertex == face.v2 && endVertex == face.v3) || (startVertex == face.v3 && endVertex == face.v1))
					{
						return BreakFaceInThree(faceIndex, startPos, endPos, splitEdge);
					}
					else
					{
						return BreakFaceInThree(faceIndex, endPos, startPos, splitEdge);
					}
				}
			}
			//______-FACE-______
			else if (startType == Segment.VERTEX && endType == Segment.EDGE)
			{
				//VERTEX-FACE-EDGE
				return BreakFaceInTwo(faceIndex, endPos, endVertex);
			}
			else if (startType == Segment.EDGE && endType == Segment.VERTEX)
			{
				//EDGE-FACE-VERTEX
				return BreakFaceInTwo(faceIndex, startPos, startVertex);
			}
			else if (startType == Segment.VERTEX && endType == Segment.FACE)
			{
				//VERTEX-FACE-FACE
				return BreakFaceInThree(faceIndex, endPos);
			}
			else if (startType == Segment.FACE && endType == Segment.VERTEX)
			{
				//FACE-FACE-VERTEX
				return BreakFaceInThree(faceIndex, startPos);
			}
			else if (startType == Segment.EDGE && endType == Segment.EDGE)
			{
				//EDGE-FACE-EDGE
				return BreakFaceInThree(faceIndex, startPos, endPos, startVertex, endVertex);
			}
			else if (startType == Segment.EDGE && endType == Segment.FACE)
			{
				//EDGE-FACE-FACE
				return BreakFaceInFour(faceIndex, startPos, endPos, startVertex);
			}
			else if (startType == Segment.FACE && endType == Segment.EDGE)
			{
				//FACE-FACE-EDGE
				return BreakFaceInFour(faceIndex, endPos, startPos, endVertex);
			}
			else if (startType == Segment.FACE && endType == Segment.FACE)
			{
				//FACE-FACE-FACE
				Vector3 segmentVector = new Vector3(startPos.x - endPos.x, startPos.y - endPos.y, startPos.z - endPos.z);

				//if the intersection segment is a point only...
				if (Math.Abs(segmentVector.x) < EqualityTolerance && Math.Abs(segmentVector.y) < EqualityTolerance && Math.Abs(segmentVector.z) < EqualityTolerance)
				{
					return BreakFaceInThree(faceIndex, startPos);
				}

				//gets the vertex more lined with the intersection segment
				int linedVertex;
				Vector3 linedVertexPos;
				Vector3 vertexVector = new Vector3(endPos.x - face.v1.Position.x, endPos.y - face.v1.Position.y, endPos.z - face.v1.Position.z);
				vertexVector.Normalize();
				double dot1 = Math.Abs(Vector3.Dot(segmentVector, vertexVector));
				vertexVector = new Vector3(endPos.x - face.v2.Position.x, endPos.y - face.v2.Position.y, endPos.z - face.v2.Position.z);
				vertexVector.Normalize();
				double dot2 = Math.Abs(Vector3.Dot(segmentVector, vertexVector));
				vertexVector = new Vector3(endPos.x - face.v3.Position.x, endPos.y - face.v3.Position.y, endPos.z - face.v3.Position.z);
				vertexVector.Normalize();
				double dot3 = Math.Abs(Vector3.Dot(segmentVector, vertexVector));
				if (dot1 > dot2 && dot1 > dot3)
				{
					linedVertex = 1;
					linedVertexPos = face.v1.GetPosition();
				}
				else if (dot2 > dot3 && dot2 > dot1)
				{
					linedVertex = 2;
					linedVertexPos = face.v2.GetPosition();
				}
				else
				{
					linedVertex = 3;
					linedVertexPos = face.v3.GetPosition();
				}

				// Now find which of the intersection endpoints is nearest to that vertex.
				if ((linedVertexPos - startPos).Length > (linedVertexPos - endPos).Length)
				{
					return BreakFaceInFive(faceIndex, startPos, endPos, linedVertex);
				}
				else
				{
					return BreakFaceInFive(faceIndex, endPos, startPos, linedVertex);
				}
			}

			return false;
		}
	}
}