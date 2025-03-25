using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameAudioManager : Singleton<GameAudioManager>
{
    public float gameVolume = 1;
    [SerializeField] private Sound[] musicSounds, sfxSounds;
    [SerializeField] private AudioSource musicSource, sfxOneShotSource;
    private List<AudioSource> interruptedAudioSources = new List<AudioSource>();

    private Dictionary<string, Sound> musicSoundDict;
    private Dictionary<string, Sound> sfxSoundDict;

    private void Awake()
    {
        musicSoundDict = new Dictionary<string, Sound>(musicSounds.Length);
        foreach (var sound in musicSounds)
        {
            if (!musicSoundDict.ContainsKey(sound.name))
                musicSoundDict.Add(sound.name, sound);
        }

        sfxSoundDict = new Dictionary<string, Sound>(sfxSounds.Length);
        foreach (var sound in sfxSounds)
        {
            if (!sfxSoundDict.ContainsKey(sound.name))
                sfxSoundDict.Add(sound.name, sound);
        }
    }

    public AudioClip GetAudioClip(string name)
    {
        if (!sfxSoundDict.TryGetValue(name, out Sound sound))
        {
            return null;
        }

        return sound.audioClip;
    }

    public void PlayMusic(string name, float volume = 1f)
    {
        if (!musicSoundDict.TryGetValue(name, out Sound sound))
        {
            Debug.LogError($"Music sound '{name}' does not exist!");
            return;
        }

        musicSource.clip = sound.audioClip;
        musicSource.volume = volume * gameVolume;
        musicSource.Play();
    }

    public void PlaySfxOneShot(string name, float volume = 1f)
    {
        if (!sfxSoundDict.TryGetValue(name, out Sound sound))
        {
            Debug.LogError($"SFX sound '{name}' does not exist!");
            return;
        }
        sfxOneShotSource.PlayOneShot(sound.audioClip, volume * gameVolume);
    }

    public AudioSource PlaySfxInterruptable(string name, float volume = 1f, bool loop = false)
    {
        if (!sfxSoundDict.TryGetValue(name, out Sound sound))
        {
            Debug.LogError($"SFX sound '{name}' does not exist!");
            return null;
        }

        // Create a new AudioSource component (consider object pooling for a more scalable solution)
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.clip = sound.audioClip;
        newSource.playOnAwake = false;
        newSource.volume = volume * gameVolume;
        newSource.loop = loop;
        newSource.Play();

        interruptedAudioSources.Add(newSource);

        // Automatically remove non-looping sources after the clip finishes playing.
        if (!loop)
        {
            StartCoroutine(RemoveAfterFinish(newSource, sound.audioClip.length));
        }

        return newSource;
    }

    private IEnumerator RemoveAfterFinish(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        StopSfx(source);
    }

    public void StopSfx(AudioSource audioSource)
    {
        if (audioSource == null) return;
        if (!interruptedAudioSources.Contains(audioSource)) return;

        interruptedAudioSources.Remove(audioSource);
        Destroy(audioSource);
    }

    public void StopAllSfx()
    {
        // Iterate backwards to safely remove items.
        for (int i = interruptedAudioSources.Count - 1; i >= 0; i--)
        {
            StopSfx(interruptedAudioSources[i]);
        }
    }
}

[Serializable]
public class Sound
{
    public string name;
    public AudioClip audioClip;
}
