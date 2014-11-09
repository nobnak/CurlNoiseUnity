using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class Curl : MonoBehaviour {

	public const int THREAD_WIDTH = 8;
	public const int THREAD_N = THREAD_WIDTH * THREAD_WIDTH;
	public const int TEX_WIDTH = 512;
	public const int KERNEL_SIMULATE = 0;
	public const int KERNEL_EMIT = 1;
	public const int KERNEL_PRECOMPUTE = 2;
	public const string SHADER_POT_TEX_IN = "PotTexIn";
	public const string SHADER_POT_TEX_OUT = "PotTexOut";
	public const string SHADER_PIX_2_NOISE = "Pix2Noise";
	public const string SHADER_GEO_2_NOISE = "Geo2Noise";
	public const string SHADER_GEO_2_UV = "Geo2UV";
	public const string SHADER_TIME = "Time";
	public const string SHADER_SPEED = "Speed";
	public const string SHADER_DT = "Dt";
	public const string SHADER_GEO_SIZE = "GeoSize";
	public const string SHADER_PARTICLE_BUF_IN = "ParticleIn";
	public const string SHADER_PARTICLE_BUF_OUT = "ParticleOut";
	public const string SHADER_EMIT_INDEX_BUF = "EmitIndices";
	public const string SHADER_EMIT_PARTICLE_BUF = "EmitParticles";

	public int nParticleGroup = 100;
	public int nEmitGroup = 2;
	public int nEmitPerSec = 100;
	public Vector2 size = new Vector2(100f, 100f);
	public float particleSpeed = 10f;
	public float timeScale = 0.1f;
	public float noiseScale = 0.1f;
	public ComputeShader curl;
	public KeyCode debugKey;
	public Material debugMat;
	public float particleLife = 30f;
	public GameObject particleFab;
	public Transform emitter;

	private Vector4 _noiseSize;
	private RenderTexture _potTex;
	private int _debugMode = 0;
	private GameObject _debugGO;
	private Mesh _debugMesh;

	private GameObject[] _particleGOs;
	private Particle[] _particles;
	private ComputeBuffer _particleBuf0, _particleBuf1;

	private int[] _emitIndices;
	private Particle[] _emitParticles;
	private ComputeBuffer _emitIndexBuf, _emitParticleBuf;
	private TickKeeper _ticker;
	private int _nPrevEmit = 0;

	void Update() {
		var dt = Time.deltaTime;

		CheckInit();
		
		curl.SetFloat(SHADER_GEO_2_NOISE, noiseScale);
		curl.SetVector(SHADER_GEO_SIZE, new Vector4(size.x, size.y, 0f, 0f));
		curl.SetVector(SHADER_PIX_2_NOISE, 
		               new Vector4(size.x * noiseScale / _potTex.width, size.y * noiseScale / _potTex.height, 0f, 0f));
		curl.SetVector(SHADER_GEO_2_UV, new Vector4(1f / size.x, 1f / size.y, 0f, 0f));

		curl.SetFloat(SHADER_TIME, timeScale * Time.timeSinceLevelLoad);
		curl.SetTexture(KERNEL_PRECOMPUTE, SHADER_POT_TEX_OUT, _potTex);
		curl.Dispatch(KERNEL_PRECOMPUTE, _potTex.width / THREAD_WIDTH, _potTex.height / THREAD_WIDTH, 1);

		UpdateEmitter();
		curl.SetBuffer(KERNEL_EMIT, SHADER_EMIT_INDEX_BUF, _emitIndexBuf);
		curl.SetBuffer(KERNEL_EMIT, SHADER_EMIT_PARTICLE_BUF, _emitParticleBuf);
		curl.SetBuffer(KERNEL_EMIT, SHADER_PARTICLE_BUF_OUT, _particleBuf0);
		curl.Dispatch(KERNEL_EMIT, nEmitGroup, 1, 1);

		UpdateParticle(dt);
		curl.SetFloat(SHADER_DT, dt);
		curl.SetFloat(SHADER_SPEED, particleSpeed);
		curl.SetBuffer(KERNEL_SIMULATE, SHADER_PARTICLE_BUF_IN, _particleBuf0);
		curl.SetBuffer(KERNEL_SIMULATE, SHADER_PARTICLE_BUF_OUT, _particleBuf1);
		curl.SetTexture(KERNEL_SIMULATE, SHADER_POT_TEX_IN, _potTex);
		curl.Dispatch(KERNEL_SIMULATE, nParticleGroup, 1, 1);
		SwapParticle();

		Shader.SetGlobalBuffer(SHADER_PARTICLE_BUF_IN, _particleBuf0);
	}

	void CheckInit() {
		var nParticles = nParticleGroup * THREAD_N;
		if (_particleGOs == null || _particleGOs.Length != nParticles) {
			ReleaseParticle ();
			_particleGOs = new GameObject[nParticles];
			for (var i = 0; i < _particleGOs.Length; i++) {
				var go = _particleGOs [i] = (GameObject)Instantiate (particleFab);
				go.transform.parent = transform;
				var mesh = go.GetComponent<MeshFilter>().mesh;
				var uv2 = new Vector2[mesh.vertexCount];
				for (var j = 0; j < uv2.Length; j++)
					uv2 [j].x = i + 0.5f;
				mesh.uv2 = uv2;
			}
			_particles = new Particle[nParticles];
			for (var i = 0; i < _particles.Length; i++)
				_particles [i] = Particle.Init;
			_particleBuf0 = new ComputeBuffer (nParticles, Marshal.SizeOf (_particles [0]));
			_particleBuf1 = new ComputeBuffer (nParticles, Marshal.SizeOf (_particles [0]));
			_particleBuf0.SetData (_particles);
			_particleBuf1.SetData (_particles);
		}
		if (_potTex == null || _potTex.width != TEX_WIDTH) {
			ReleasePotTex ();
			_potTex = new RenderTexture (TEX_WIDTH, TEX_WIDTH, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
			_potTex.filterMode = FilterMode.Bilinear;
			_potTex.enableRandomWrite = true;
			_potTex.Create ();
		}

		if (_emitIndices == null) {
			ReleaseEmitter();
			_emitIndices = new int[nEmitGroup * THREAD_N];
			_emitParticles = new Particle[_emitIndices.Length];
			for (var i = 0; i < _emitIndices.Length; i++) {
				_emitIndices[i] = -1;
				_emitParticles[i] = Particle.Init;
			}
			_emitIndexBuf = new ComputeBuffer(_emitIndices.Length, Marshal.SizeOf(_emitIndices[0]));
			_emitParticleBuf = new ComputeBuffer(_emitParticles.Length, Marshal.SizeOf(_emitParticles[0]));
			_emitIndexBuf.SetData(_emitIndices);

			_ticker = new TickKeeper(nEmitPerSec);
		}
		_ticker.Fps = nEmitPerSec;
	}
	void SwapParticle() {
		var tmp = _particleBuf0; _particleBuf0 = _particleBuf1; _particleBuf1 = tmp;
	}

	void UpdateParticle(float dt) {
		for (var i = 0; i < _particles.Length; i++) {
			var p = _particles[i];
			if (p.t < p.life) {
				p.t += dt;
				_particles[i] = p;
			}
		}
	}

	void UpdateEmitter() {
		var nEmit = _ticker.Count();
		nEmit = (nEmit > _emitIndices.Length ? _emitIndices.Length : nEmit);
		var emitCount = 0;
		for (var i = 0; i < _particles.Length && emitCount < nEmit; i++) {
			var p = _particles[i];
			if (p.life <= p.t) {
				var pnew = Particle.From(emitter, particleLife);
				_emitIndices[emitCount] = i;
				_emitParticles[emitCount] = _particles[i] = pnew;
				emitCount++;
			}
		}
		for (var i = nEmit; i < _nPrevEmit; i++)
			_emitIndices[i] = -1;
		_nPrevEmit = nEmit;
		_emitIndexBuf.SetData(_emitIndices);
		_emitParticleBuf.SetData(_emitParticles);
	}

	void OnDestroy() {
		Destroy(_debugMesh);
		ReleasePotTex();
		ReleaseParticle();
		ReleaseEmitter();
	}
	void ReleasePotTex() {
		if (_potTex != null)
			_potTex.Release ();
	}

	void ReleaseParticle () {
		if (_particleGOs != null) {
			foreach (var go in _particleGOs) {
				Destroy (go.GetComponent<MeshFilter> ().sharedMesh);
				Destroy (go);
			}
		}
		if (_particleBuf0 != null)
			_particleBuf0.Release();
		if (_particleBuf1 != null)
			_particleBuf1.Release();
	}

	void ReleaseEmitter() {
		if (_emitIndexBuf != null)
			_emitIndexBuf.Release ();
		if (_emitParticleBuf != null)
			_emitParticleBuf.Release ();
	}

	void OnGUI() {
		if (Event.current.type.Equals(EventType.KeyDown)) {
			if (Input.GetKeyDown(debugKey))
				_debugMode = (++_debugMode) % 3;
		}

		if (Event.current.type.Equals(EventType.Repaint)) {
			var active = false;
			switch (_debugMode) {
			case 1:
				if (_debugMesh == null) {
					_debugGO = new GameObject("Debug Mesh");
					_debugGO.hideFlags = HideFlags.DontSave;
					_debugGO.transform.parent = transform;
					_debugGO.transform.localPosition = new Vector3(0f, 0f, 1f);
					_debugGO.AddComponent<MeshRenderer>();
					_debugGO.renderer.sharedMaterial = debugMat;
					_debugGO.AddComponent<MeshFilter>().sharedMesh = _debugMesh = new Mesh();
					_debugMesh.vertices = new Vector3[]{ Vector3.zero, new Vector3(size.x, 0f, 0f), new Vector3(0f, size.y, 0f), new Vector3(size.x, size.y, 0f) };
					_debugMesh.triangles = new int[]{ 0, 3, 1, 0, 2, 3 };
					_debugMesh.uv = new Vector2[]{ new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
				}
				active = true;
				debugMat.mainTexture = _potTex;
				break;
			}
			if (_debugGO != null)
				_debugGO.SetActive(active);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct Particle {
		public Vector2 x;
		public float t;
		public float life;

		public Particle(Vector2 x, float t, float life) {
			this.x = x;
			this.t = t;
			this.life = life;
		}
		public static readonly Particle Init = new Particle(Vector2.zero, 0f, -1f);
		public static Particle From(Transform emitter, float life) {
			var pos = new Vector2(emitter.localPosition.x + emitter.localScale.x * (Random.value - 0.5f),
			                      emitter.localPosition.y + emitter.localScale.y * (Random.value - 0.5f));
			return new Particle(pos, 0f, life);
		}
	}
}
