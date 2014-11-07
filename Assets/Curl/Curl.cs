using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class Curl : MonoBehaviour {

	public const int N_THREADS_IN_GROUP = 64;
	public const int KERNEL_SIMULATE = 0;
	public const int KERNEL_EMIT = 1;
	public const string SHADER_BUF_PARTICLE_IN = "ParticleIn";
	public const string SHADER_BUF_PARTICLE_OUT = "ParticleOut";
	public const string SHADER_BUF_SPHERE = "Spheres";
	public const string SHADER_BUF_EMIT_INDEX = "EmitIndices";
	public const string SHADER_BUF_EMIT_PARTICLE = "EmitParticles";
	public const string SHADER_TIME = "Time";
	public const string SHADER_DT = "Dt";
	public const string SHADER_NOISE_SCALE = "NoiseScale";
	public const string SHADER_SPEED = "Speed";
	public const string SHADER_ID = "Id";

	public int nGroups = 1;
	public int nGroupsOfEmit = 1;
	public float speed = 1;
	public float timeScale = 1;
	public float noiseScale = 0.1f;
	public ComputeShader curl;
	public GameObject particleFab;
	public Transform[] spheres;
	public Transform emitter;

	private int _nThreadsOfSimulation = -1;
	private Particle[] _particles;
	private ComputeBuffer _particleBuf0, _particleBuf1;
	private Vector4[] _spheres;
	private ComputeBuffer _sphereBuf;
	private GameObject[] _particleGOs;
	private GameObject _parent;
	private int _nPrevEmit = 0;
	private int[] _emitIndices;
	private ComputeBuffer _emitIndexBuf;
	private Particle[] _emitParticles;
	private ComputeBuffer _emitParticleBuf;

	void OnDestroy() { Release(); }
	void Update () {
		CheckInit();

		var time = Time.timeSinceLevelLoad;
		var dt = Time.deltaTime;

		UpdateEmitter();
		curl.SetBuffer(KERNEL_EMIT, SHADER_BUF_EMIT_INDEX, _emitIndexBuf);
		curl.SetBuffer(KERNEL_EMIT, SHADER_BUF_EMIT_PARTICLE, _emitParticleBuf);
		curl.SetBuffer(KERNEL_EMIT, SHADER_BUF_PARTICLE_OUT, _particleBuf0);
		curl.Dispatch(KERNEL_EMIT, nGroupsOfEmit, 1, 1);

		UpdateParticles(dt);
		curl.SetFloat(SHADER_DT, dt);
		curl.SetFloat(SHADER_SPEED, speed);
		curl.SetFloat(SHADER_TIME, time * timeScale);
		curl.SetFloat(SHADER_NOISE_SCALE, noiseScale);
		if (spheres.Length > 0) {
			UpdateSphereBuf();
			curl.SetBuffer(KERNEL_SIMULATE, SHADER_BUF_SPHERE, _sphereBuf);
		}
		curl.SetBuffer(KERNEL_SIMULATE, SHADER_BUF_PARTICLE_IN, _particleBuf0);
		curl.SetBuffer(KERNEL_SIMULATE, SHADER_BUF_PARTICLE_OUT, _particleBuf1);
		curl.Dispatch(KERNEL_SIMULATE, nGroups, 1, 1);
		Swap();

		Shader.SetGlobalFloat(SHADER_DT, dt);
		Shader.SetGlobalBuffer(SHADER_BUF_PARTICLE_IN, _particleBuf0);
	}
	void UpdateParticles(float dt) {
		for (var i = 0; i < _particles.Length; i++) {
			var p = _particles[i];
			if (p.t < p.life)
				p.t += dt;
			_particles[i] = p;
		}
	}
	void UpdateEmitter() {
		var nReqEmit = 10;
		var nCurrEmit = 0;
		for (var i = 0; i < _particles.Length && nCurrEmit < nReqEmit; i++) {
			var p = _particles[i];
			if (p.life <= p.t) {
				var pEmit = new Particle(RandomInEmitter(), 0f, 30f);
				_emitIndices[nCurrEmit] = i;
				_emitParticles[nCurrEmit] = _particles[i] = pEmit;
				nCurrEmit++;
			}
		}
		if (nCurrEmit < _nPrevEmit)
			for (var i = nCurrEmit; i < _emitIndices.Length && i <= _nPrevEmit; i++)
				_emitIndices[i] = -1;
		if (nCurrEmit > 0 || nCurrEmit != _nPrevEmit) {
			_nPrevEmit = nCurrEmit;
			_emitIndexBuf.SetData(_emitIndices);
			_emitParticleBuf.SetData(_emitParticles);
		}
	}
	void UpdateSphereBuf() {
		for (var i = 0; i < spheres.Length; i++) {
			var s = spheres[i];
			var p = s.position;
			_spheres [i] = new Vector4(p.x, p.y, p.z, 0.5f * s.localScale.x);
		}
		_sphereBuf.SetData(_spheres);
	}

	void CheckInit() {
		_nThreadsOfSimulation = nGroups * N_THREADS_IN_GROUP;
		if (_particles == null || _particles.Length != _nThreadsOfSimulation) {
			ReleaseParticleBufs();
			_particles = new Particle[_nThreadsOfSimulation];
			_particleBuf0 = new ComputeBuffer(_particles.Length, Marshal.SizeOf(_particles[0]));
			_particleBuf1 = new ComputeBuffer(_particles.Length, Marshal.SizeOf(_particles[0]));
			
			for (var i = 0; i < _nThreadsOfSimulation; i++)
				_particles[i] = Particle.Init; //new Particle(new Vector3(10f * RandomCenter(), 10f * RandomCenter(), 10f * RandomCenter()), 0f, 30f);
			_particleBuf0.SetData(_particles);
			_particleBuf1.SetData(_particles);

			_parent = new GameObject("Root Particle");
			_particleGOs = new GameObject[_nThreadsOfSimulation];
			for (var i = 0;  i < _nThreadsOfSimulation; i++) {
				var go = _particleGOs[i] = (GameObject)Instantiate(particleFab, Vector3.zero, Quaternion.identity);
				go.name = "Particle";
				go.transform.parent = _parent.transform;
				var mat = go.renderer.material;
				mat.SetInt(SHADER_ID, i);
			}
		}
		if (_spheres == null || _spheres.Length != spheres.Length) {
			ReleaseSphereBufs();
			if (spheres.Length > 0) {
				_spheres = new Vector4[spheres.Length];
				_sphereBuf = new ComputeBuffer(_spheres.Length, Marshal.SizeOf(typeof(Vector4)));
			}
		}
		if (_emitParticles == null) {
			ReleaseEmitBufs();
			_nPrevEmit = -1;
			_emitIndices = new int[nGroupsOfEmit * N_THREADS_IN_GROUP];
			for (var i = 0; i < _emitIndices.Length; i++)
				_emitIndices[i] = -1;
			_emitIndexBuf = new ComputeBuffer(_emitIndices.Length, Marshal.SizeOf(_emitIndices[0]));
			_emitIndexBuf.SetData(_emitIndices);
			_emitParticles = new Particle[_emitIndices.Length];
			_emitParticleBuf = new ComputeBuffer(_emitParticles.Length, Marshal.SizeOf(_emitParticles[0]));
		}
	}

	void Release() {
		ReleaseParticleBufs();
		ReleaseSphereBufs();
		ReleaseEmitBufs();
	}
	void ReleaseParticleBufs() {
		if (_particleBuf0 != null)
			_particleBuf0.Release ();
		if (_particleBuf1 != null)
			_particleBuf1.Release ();
		if (_particleGOs != null)
			for (var i = 0; i < _particleGOs.Length; i++)
				Destroy (_particleGOs [i]);
		Destroy (_parent);
	}
	void ReleaseEmitBufs() {
		if (_emitIndexBuf != null)
			_emitIndexBuf.Release();
		if (_emitParticleBuf != null)
			_emitParticleBuf.Release();
	}
	void ReleaseSphereBufs() {
		if (_sphereBuf != null)
			_sphereBuf.Release();
	}
	void Swap() {
		var tmpPos = _particleBuf0; _particleBuf0 = _particleBuf1; _particleBuf1 = tmpPos;
	}
	Vector3 RandomInEmitter() {
		var es = emitter.localScale;
		return emitter.position + new Vector3(es.x * RandomCenter(), es.y * RandomCenter(), es.z * RandomCenter());
	}
	float RandomCenter() { return Random.Range(-0.5f, 0.5f); }

	[StructLayout(LayoutKind.Sequential)]
	public struct Particle {
		public Vector3 x;
		public float t;
		public float life;

		public Particle(Vector3 x, float t, float life) {
			this.x = x;
			this.t = t;
			this.life = life;
		}
		public static readonly Particle Init = new Particle(Vector3.zero, 0f, 0f);
	}
}
