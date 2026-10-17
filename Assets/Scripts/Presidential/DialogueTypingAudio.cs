using System;
using TMPro;
using UnityEngine;

// Optional presentation sound shared by the two existing dialogue views.
[Serializable]
public sealed class DialogueTypingAudio
{
    public AudioClip clip;
    [Range(2,4)] public int characterInterval = 3;
    [Range(0,.2f)] public float volume = .045f;
    [NonSerialized] private AudioSource source;
    [NonSerialized] private int lastCharacter;
    [NonSerialized] private float nextSound;

    public void BeginLine() { lastCharacter=0;nextSound=0;Stop(); }
    public void Reveal(TMP_Text text, int visible, GameObject owner)
    {
        if(!clip || visible-lastCharacter<characterInterval || Time.unscaledTime<nextSound)return;
        int index=Mathf.Min(visible,text.textInfo.characterCount)-1;
        if(index<0 || char.IsWhiteSpace(text.textInfo.characterInfo[index].character))return;
        if(!source)
        {
            source=owner.AddComponent<AudioSource>();source.playOnAwake=false;
            source.loop=false;source.spatialBlend=0;source.volume=1;
        }
        source.Stop();source.PlayOneShot(clip,volume);lastCharacter=visible;nextSound=Time.unscaledTime+.09f;
    }
    public void Stop() { if(source)source.Stop(); }
}
