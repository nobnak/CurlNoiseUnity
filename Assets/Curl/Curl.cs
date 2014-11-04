using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class Curl : MonoBehaviour {

	public const int N_THREADS_IN_GROUP = 64;
	public const int KERNEL = 0;
	public const string SHADER_POS_IN = "PosIn";
	public const string SHADER_POS_OUT = "PosOut";
	public const string SHADER_DT = "Dt";
	public const string SHADER_DX = "Dx";
	public const string SHADER_SCALE = "Scale";
	public const string SHADER_ID = "Id";

	public int nGroups = 1;
	public ComputeShader curl;
	public GameObject particleFab;

	private int _nThreads = -1;
	private Vector2[] _poses;
	private ComputeBuffer _posBuf0, _posBuf1;
	private GameObject[] _particles;
	private GameObject _parent;

	void OnDestroy() { Release(); }
	void Update () {
		CheckInit();

		var dt = Time.deltaTime;

		curl.SetFloat(SHADER_DT, dt);
		curl.SetBuffer(KERNEL, SHADER_POS_IN, _posBuf0);
		curl.SetBuffer(KERNEL, SHADER_POS_OUT, _posBuf1);
		curl.Dispatch(KERNEL, nGroups, 1, 1);
		Swap();

		Shader.SetGlobalFloat(SHADER_DT, dt);
		Shader.SetGlobalBuffer(SHADER_POS_IN, _posBuf0);
	}

	void CheckInit() {
		_nThreads = nGroups * N_THREADS_IN_GROUP;
		if (_poses != null && _poses.Length == _nThreads)
			return;

		Release();

		_poses = new Vector2[_nThreads];
		_posBuf0 = new ComputeBuffer(_poses.Length, Marshal.SizeOf(_poses[0]));
		_posBuf1 = new ComputeBuffer(_poses.Length, Marshal.SizeOf(_poses[0]));

		for (var i = 0; i < _nThreads; i++)
			_poses[i] = new Vector2(5f * (Random.value - 0.5f), 5f * (Random.value - 0.5f));
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
	void Release() {
		if (_posBuf0 != null)
			_posBuf0.Release();
		if (_posBuf1 != null)
			_posBuf1.Release();

		if (_particles != null)
			for (var i = 0; i < _particles.Length; i++)
				Destroy(_particles[i]);
		Destroy(_parent);
	}
	void Swap() {
		var tmpPos = _posBuf0; _posBuf0 = _posBuf1; _posBuf1 = tmpPos;
	}
}
