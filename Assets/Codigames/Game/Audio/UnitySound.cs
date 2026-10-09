using System;
using Codigames.Modules.Audio;
using UnityEngine;

namespace Codigames.Game.Audio
{
    // A sound as Unity plays it: one or more takes (a random one each time, so repeats don't sound identical), its
    // level in the mix, a pitch wobble, a base pitch, and the track it plays on.
    [Serializable]
    public class UnitySound : ISound
    {
        [SerializeField] private string _id;
        [SerializeField] private AudioClip[] _clips = Array.Empty<AudioClip>();
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;
        [SerializeField, Range(0f, 0.5f), Tooltip("Each play is pitched up or down by up to this fraction.")] private float _pitchJitter;
        [SerializeField, Range(0.25f, 2f), Tooltip("Base pitch: one file can serve two sounds.")] private float _rate = 1f;
        [SerializeField] private SoundTrack _track;

        public string Id => _id;
        public SoundTrack Track => _track;
        public float Volume => _volume;
        public float PitchJitter => _pitchJitter;
        public float Rate => _rate;
        public int Takes => _clips.Length;

        public AudioClip Take(int index) => _clips.Length == 0 ? null : _clips[index % _clips.Length];
    }
}
