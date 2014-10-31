using UnityEngine;
using System.Collections;

public class SNoise : MonoBehaviour {
	public const int N_THREADS = 64;
	public const int KERNEL_SNOISE = 0;

	public const string SHADER_POS_IN = "PosIn";
	public const string SHADER_POS_OUT = "PosOut";

	public int n = 512;
	public Vector4 speed;
	public ComputeShader snoise;

	private int _nGroups;
	private RenderTexture _noiseTex;

	void OnDisable() {
		Release();
	}
	void Update() {
		CheckInit();

		snoise.SetVector(SHADER_SPEED, speed);
		snoise.SetFloat(SHADER_TIME, Time.timeSinceLevelLoad);
		snoise.SetTexture(KERNEL_SNOISE, SHADER_RESULT, _noiseTex);
		_noiseTex.DiscardContents();
		snoise.Dispatch(KERNEL_SNOISE, _nGroups, _nGroups, 1);

		var m = renderer.sharedMaterial;
		m.mainTexture = _noiseTex;
	}

	void CheckInit() {
		if (_noiseTex != null && _noiseTex.width == n)
			return;

		Release();

		_nGroups = n / N_THREADS;

		_noiseTex = new RenderTexture(n, n, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
		_noiseTex.enableRandomWrite = true;
		_noiseTex.Create();
	}
	void Release() {
		if (_noiseTex != null)
			_noiseTex.Release();
	}
}
