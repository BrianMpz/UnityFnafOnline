using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameAudioManager : Singleton<GameAudioManager>
{
    public float gameVolume = 1;
    [SerializeField] private Sound[] musicSounds, sfxSounds;
    [SerializeField] private AudioSource musicSource, sfxOneShotSource;
    private List<AudioSource> interruptableAudioSources = new();

    private Dictionary<string, Sound> musicSoundDict;
    private Dictionary<string, Sound> sfxSoundDict;

    private protected override void OnEnable()
    {
        SoundArrayToDict();

        if (Instance != null && Instance != this) Destroy(Instance.gameObject);
        base.OnEnable();
    }

    private void SoundArrayToDict()
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
            if (!musicSoundDict.TryGetValue(name, out Sound music))
            {
                return null;
            }
            else return music.audioClip;
        }
        else return sound.audioClip;
    }

    public void PlayMusic(string name, float volume = 1f, bool loop = true) // only one music clip can play at a time
    {
        StopMusic();

        if (!musicSoundDict.TryGetValue(name, out Sound sound))
        {
            Debug.LogError($"Music sound '{name}' does not exist!");
            return;
        }

        musicSource.clip = sound.audioClip;
        musicSource.volume = volume * gameVolume;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public AudioSource GetCurrentMusic() => musicSource;

    public void StopMusic()
    {
        if (musicSource.isPlaying) musicSource.Stop();
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

        interruptableAudioSources.Add(newSource);

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
        if (!interruptableAudioSources.Contains(audioSource)) return;

        interruptableAudioSources.Remove(audioSource);
        Destroy(audioSource);
    }

    public void StopAllSfx()
    {
        // Iterate backwards to safely remove items.
        for (int i = interruptableAudioSources.Count - 1; i >= 0; i--)
        {
            StopSfx(interruptableAudioSources[i]);
        }
    }

    public void PlayButtonSelect() // called in unity events
    {
        PlaySfxOneShot("select 1");
    }
}

[Serializable]
public class Sound
{
    public string name;
    public AudioClip audioClip;
}
