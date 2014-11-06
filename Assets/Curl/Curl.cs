using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class Curl : MonoBehaviour {

	public const int N_THREADS_IN_GROUP = 64;
	public const int KERNEL = 0;
	public const string SHADER_BUF_POS_IN = "PosIn";
	public const string SHADER_BUF_POS_OUT = "PosOut";
	public const string SHADER_BUF_SPHERE = "Spheres";
	public const string SHADER_TIME = "Time";
	public const string SHADER_DT = "Dt";
	public const string SHADER_NOISE_SCALE = "NoiseScale";
	public const string SHADER_SPEED = "Speed";
	public const string SHADER_ID = "Id";
	public const string SHADER_L = "L";

	public int nGroups = 1;
	public float speed = 1;
	public float timeScale = 1;
	public float noiseScale = 0.1f;
	public Vector4 l = new Vector4(10f, 10f, 10f, 0f);
	public ComputeShader curl;
	public GameObject particleFab;
	public Transform[] Spheres;

	private int _nThreads = -1;
	private Vector3[] _poses;
	private ComputeBuffer _posBuf0, _posBuf1;
	private Vector4[] _spheres;
	private ComputeBuffer _sphereBuf;
	private GameObject[] _particles;
	private GameObject _parent;

	void OnDestroy() { Release(); }
	void Update () {
		CheckInit();

		var time = Time.timeSinceLevelLoad;
		var dt = Time.deltaTime;

		curl.SetFloat(SHADER_DT, dt);
		curl.SetFloat(SHADER_SPEED, speed);
		curl.SetFloat(SHADER_TIME, time * timeScale);
		curl.SetFloat(SHADER_NOISE_SCALE, noiseScale);
		if (Spheres.Length > 0) {
			UpdateSphereBuf();
			curl.SetBuffer(KERNEL, SHADER_BUF_SPHERE, _sphereBuf);
		}
		curl.SetBuffer(KERNEL, SHADER_BUF_POS_IN, _posBuf0);
		curl.SetBuffer(KERNEL, SHADER_BUF_POS_OUT, _posBuf1);
		curl.Dispatch(KERNEL, nGroups, 1, 1);
		Swap();

		Shader.SetGlobalFloat(SHADER_DT, dt);
		Shader.SetGlobalBuffer(SHADER_BUF_POS_IN, _posBuf0);
	}

	void CheckInit() {
		_nThreads = nGroups * N_THREADS_IN_GROUP;
		if (_poses == null || _poses.Length != _nThreads) {
			ReleasePosBufs();
			_poses = new Vector3[_nThreads];
			_posBuf0 = new ComputeBuffer(_poses.Length, Marshal.SizeOf(_poses[0]));
			_posBuf1 = new ComputeBuffer(_poses.Length, Marshal.SizeOf(_poses[0]));

			for (var i = 0; i < _nThreads; i++)
				_poses[i] = new Vector3(l.x * Random.value, l.y * Random.value, l.z * Random.value);
			_posBuf0.SetData(_poses);
			_posBuf1.SetData(_poses);

			_parent = new GameObject("Root Particle");
			_particles = new GameObject[_nThreads];
			for (var i = 0;  i < _nThreads; i++) {
				var go = _particles[i] = (GameObject)Instantiate(particleFab, Vector3.zero, Quaternion.identity);
				go.name = "Particle";
				go.transform.parent = _parent.transform;
				var mat = go.renderer.material;
				mat.SetInt(SHADER_ID, i);
			}
		}
		if (_spheres == null || _spheres.Length != Spheres.Length) {
			ReleaseSphereBufs();
			if (Spheres.Length > 0) {
				_spheres = new Vector4[Spheres.Length];
				_sphereBuf = new ComputeBuffer(_spheres.Length, Marshal.SizeOf(typeof(Vector4)));
			}
		}
	}
	void UpdateSphereBuf() {
		for (var i = 0; i < Spheres.Length; i++) {
			var s = Spheres[i];
			var p = s.position;
			_spheres [i] = new Vector4(p.x, p.y, p.z, 0.5f * s.localScale.x);
		}
		_sphereBuf.SetData(_spheres);
	}

	void Release() {
		ReleasePosBufs();
		ReleaseSphereBufs();
	}
	void ReleasePosBufs() {
		if (_posBuf0 != null)
			_posBuf0.Release ();
		if (_posBuf1 != null)
			_posBuf1.Release ();
		if (_particles != null)
			for (var i = 0; i < _particles.Length; i++)
				Destroy (_particles [i]);
		Destroy (_parent);
	}
	void ReleaseSphereBufs() {
		if (_sphereBuf != null)
			_sphereBuf.Release();
	}
	void Swap() {
		var tmpPos = _posBuf0; _posBuf0 = _posBuf1; _posBuf1 = tmpPos;
	}
}
