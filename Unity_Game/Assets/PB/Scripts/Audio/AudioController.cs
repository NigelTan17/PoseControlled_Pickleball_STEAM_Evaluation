using UnityEngine;
using System.Collections.Generic;

[DefaultExecutionOrder(-50)]
public class AudioController : MonoBehaviour
{
    [System.Serializable] public class NamedClip { public string key; public AudioClip clip; }

    public static AudioController Instance { get; private set; }

    [Header("SFX Library")] public List<NamedClip> sfx = new();   // keys: BallHit, Score, UIClick
    [Header("Music Library")] public List<NamedClip> music = new(); // key: Match
    [Header("Volumes")] [Range(0,1)] public float sfxVolume = 0.9f; [Range(0,1)] public float musicVolume = 0.5f;

    AudioSource _sfx, _musicA, _musicB;
    Dictionary<string, AudioClip> _sfxMap, _musicMap;
    bool _aActive = true;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);

        _sfx = gameObject.AddComponent<AudioSource>(); _sfx.playOnAwake = false; _sfx.spatialBlend = 0f;

        _musicA = gameObject.AddComponent<AudioSource>(); _musicA.loop = true; _musicA.spatialBlend = 0f;
        _musicB = gameObject.AddComponent<AudioSource>(); _musicB.loop = true; _musicB.spatialBlend = 0f;

        _sfxMap = new(); foreach (var nc in sfx) if (!string.IsNullOrEmpty(nc.key) && nc.clip) _sfxMap[nc.key] = nc.clip;
        _musicMap = new(); foreach (var nc in music) if (!string.IsNullOrEmpty(nc.key) && nc.clip) _musicMap[nc.key] = nc.clip;
    }

    void OnValidate()
    {
        if (_sfx) _sfx.volume = sfxVolume;
        var cur = _aActive ? _musicA : _musicB; if (cur) cur.volume = musicVolume;
    }

    // --------- SFX ----------
    public void PlaySFX2D(string key, float vol=1f, float pitch=1f)
    {
        if (_sfxMap.TryGetValue(key, out var c) && c)
        { _sfx.pitch = Mathf.Clamp(pitch, 0.1f, 3f); _sfx.volume = sfxVolume * Mathf.Clamp01(vol); _sfx.PlayOneShot(c); }
    }

    // “Clamped” one-shot: stops after maxDuration seconds (quick way to “trim” long beeps)
    public void PlaySFX2DClamped(string key, float maxDuration, float vol=1f, float pitch=1.0f)
    {
        if (!_sfxMap.TryGetValue(key, out var clip) || !clip) return;
        var go = new GameObject("SFX2D_"+key); var a = go.AddComponent<AudioSource>();
        a.spatialBlend = 0f; a.playOnAwake = false; a.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        a.volume = sfxVolume * Mathf.Clamp01(vol); a.clip = clip; a.Play();
        Destroy(go, Mathf.Min(maxDuration, clip.length / Mathf.Max(0.1f, a.pitch)));
    }

    public void PlaySFX3D(string key, Vector3 pos, float vol=1f, float pitch=1f)
    {
        if (!_sfxMap.TryGetValue(key, out var c) || !c) return;
        var go = new GameObject("SFX3D_"+key); go.transform.position = pos;
        var a = go.AddComponent<AudioSource>(); a.spatialBlend = 1f; a.rolloffMode = AudioRolloffMode.Linear; a.minDistance = 2f; a.maxDistance = 18f;
        a.pitch = Mathf.Clamp(pitch, 0.1f, 3f); a.volume = sfxVolume * Mathf.Clamp01(vol); a.clip = c; a.Play();
        Destroy(go, c.length / Mathf.Max(0.1f, a.pitch));
    }

    // --------- MUSIC ----------
    public void PlayMusic(string key, float fade=0.8f)
    {
        if (!_musicMap.TryGetValue(key, out var clip) || !clip) return;
        StopAllCoroutines();
        var from = _aActive ? _musicA : _musicB; var to = _aActive ? _musicB : _musicA; _aActive = !_aActive;
        to.clip = clip; to.volume = 0f; to.Play(); StartCoroutine(Fade(from, to, fade));
    }
    public void StopMusic(float fade=0.4f)
    {
        StopAllCoroutines(); var cur = _aActive ? _musicA : _musicB; if (!cur || !cur.isPlaying) return;
        var dummy = gameObject.AddComponent<AudioSource>(); dummy.volume = 0f;
        StartCoroutine(Fade(cur, dummy, fade, stopFrom:true));
    }
    System.Collections.IEnumerator Fade(AudioSource from, AudioSource to, float t, bool stopFrom=false)
    {
        float el=0f; while (el<t){ el+=Time.unscaledDeltaTime; float k=Mathf.Clamp01(el/t);
            if (from) from.volume = Mathf.Lerp(musicVolume, 0f, k);
            if (to)   to.volume   = Mathf.Lerp(0f, musicVolume, k); yield return null; }
        if (from && stopFrom) from.Stop(); if (from) from.volume = 0f; if (to) to.volume = musicVolume;
    }
}
