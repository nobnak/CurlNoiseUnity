using UnityEngine;
using UnityEditor;
using System.Collections;

public static class ParticleBuilder {
	[MenuItem("Custom/BuildParticle")]
	public static void BuildParticle() {
		var mesh = new Mesh();
		mesh.vertices = new Vector3[]{ 
			new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
			new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
		mesh.triangles = new int[]{ 0, 3, 1, 0, 2, 3 };
		mesh.uv = new Vector2[]{
			new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
		mesh.bounds = new Bounds(Vector3.zero, 1000f * Vector3.one);
		mesh.RecalculateNormals();
		AssetDatabase.CreateAsset(mesh, "Assets/Particle.asset");
	}
}
