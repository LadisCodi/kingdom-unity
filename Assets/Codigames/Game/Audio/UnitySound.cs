using System;
using Codigames.Modules.Audio;
using UnityEngine;

namespace Codigames.Game.Audio
{
    // A sound as Unity plays it: a clip on a track.
    [Serializable]
    public class UnitySound : ISound
    {
        [SerializeField] private string _id;
        [SerializeField] private AudioClip _clip;
        [SerializeField] private SoundTrack _track;

        public string Id => _id;
        public AudioClip Clip => _clip;
        public SoundTrack Track => _track;
    }
}
