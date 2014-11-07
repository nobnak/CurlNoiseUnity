using UnityEngine;
using System.Collections;

public class TickKeeper {
	private int _fps;
	private float _invFps;
	private float _t;

	public TickKeeper(int fps) {
		Fps = fps;
		_t = Time.timeSinceLevelLoad;
	}

	public int Count() {
		var t = Time.timeSinceLevelLoad;
		if (_fps > 0) {
			var dt = t - _t;
			var n = Mathf.FloorToInt(_fps * dt);
			_t += n * _invFps;
			return n;
		}
		_t = t;
		return 0;
	}

	public int Fps {
		get { return _fps; }
		set {
			_fps = (value >= 0 ? value : 0);
			_invFps = 1f / (_fps + 1e-3f);
		}
	}
}
