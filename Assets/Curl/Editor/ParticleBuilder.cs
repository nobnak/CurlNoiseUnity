using UnityEngine;
using UnityEditor;
using System.Collections;

public static class ParticleBuilder {
	public const int N_PARTICLES_IN_MESH = 10000;
	public readonly static Vector3[] QUAD = new Vector3[]{ 
		new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
		new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
	public readonly static int[] TRIS = new int[]{ 0, 3, 1, 0, 2, 3 };
	public readonly static Vector2[] UVS = new Vector2[]{
		new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
	public readonly static Bounds BOUNDS = new Bounds(Vector3.zero, 1000f * Vector3.one);

	[MenuItem("Custom/BuildParticle")]
	public static void BuildParticle() {
		var mesh = new Mesh();
		mesh.vertices = QUAD;
		mesh.triangles = TRIS;
		mesh.uv = UVS;
		mesh.bounds = BOUNDS;
		mesh.RecalculateNormals();
		AssetDatabase.CreateAsset(mesh, "Assets/Particle.asset");
	}
	[MenuItem("Custom/BuildCombinedParticle")]
	public static void BuildCombinedParticle() {
		var mesh = new Mesh();
		var vertices = new Vector3[4 * N_PARTICLES_IN_MESH];
		var uv = new Vector2[vertices.Length];
		var triangles = new int[6 * N_PARTICLES_IN_MESH];
		for (var i = 0; i < vertices.Length; i+=4) {
			for (var j = 0; j < 4; j++) {
				vertices[i + j] = QUAD[j]; uv[i + j] = UVS[j];
			}
		}
		for (var i = 0; i < N_PARTICLES_IN_MESH; i++) {
			for (var j = 0; j < TRIS.Length; j++)
				triangles[6 * i + j] = 4 * i + TRIS[j];
		}
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		mesh.bounds = BOUNDS;
		mesh.RecalculateNormals();
		AssetDatabase.CreateAsset(mesh, "Assets/CombinedParticles.asset");
	}
}
