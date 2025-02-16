using System;
using UnityEngine;
using System.Collections.Generic;
public class GameAudioManager : Singleton<GameAudioManager>
{
    public float gameVolume = 1;
    [SerializeField] private Sound[] musicSounds, sfxSounds;
    [SerializeField] private AudioSource musicSource, sfxOneShotSource;
    private List<AudioSource> interruptedAudioSources = new();


    public void PlayMusic(string name, float volume = 1f)
    {
        Sound sound = Array.Find(musicSounds, x => x.name == name);
        if (sound == null)
        {
            Debug.LogError($"Music sound '{name}' does not exist!");
            return;
        }
        musicSource.volume = volume * gameVolume;
        musicSource.Play();
    }

    public void PlaySfxOneShot(string name, float volume = 1f)
    {
        Sound sound = Array.Find(sfxSounds, x => x.name == name);
        if (sound == null)
        {
            Debug.LogError($"SFX sound '{name}' does not exist!");
            return;
        }

        sfxOneShotSource.PlayOneShot(sound.audioClip, volume * gameVolume);
    }

    public AudioSource PlaySfxInterruptable(string name, float volume = 1f, bool loop = false)
    {
        Sound sound = Array.Find(sfxSounds, x => x.name == name);
        if (sound == null)
        {
            Debug.LogError($"SFX sound '{name}' does not exist!");
            return null;
        }

        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.clip = sound.audioClip;
        newSource.playOnAwake = false;
        newSource.volume = volume * gameVolume;
        newSource.loop = loop;

        newSource.Play();
        interruptedAudioSources.Add(newSource);
        return newSource;
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
        List<AudioSource> audioSourcesToRemove = new();
        interruptedAudioSources.ForEach(audioSource =>
        {
            audioSourcesToRemove.Add(audioSource);
        });

        audioSourcesToRemove.ForEach(audioSource =>
        {
            StopSfx(audioSource);
        });
    }
}

[Serializable]
public class Sound
{
    public string name;
    public AudioClip audioClip;
}

