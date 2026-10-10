using Codigames.Game.City;
using UnityEngine;

namespace Codigames.Game.Feedback
{
    // A building finished — built or raised a level — gives off a burst of gold sparkles from its plot, rising and
    // falling back as they fade. Unity-only polish: the web marks the moment with a sound alone. Each burst is its own
    // short-lived particle system; they are rare.
    public class BuildBurst
    {
        private const int SPARKS = 36;
        private const int ORDER = 560;
        private static readonly Color GOLD = new Color32(0xff, 0xc5, 0x31, 0xff);
        private static readonly Color PALE = new Color32(0xff, 0xf1, 0xb8, 0xff);

        private readonly CityView _city;
        private Material _material;

        public BuildBurst(CityView city) => _city = city;

        public void At(string districtId)
        {
            var view = _city.ViewOf(districtId);
            if (view == null) return;

            var width = Mathf.Max(0.5f, view.PlotWidth);
            // A spark is about as big on a hut as on a hall: its size follows the plot's root.
            var spark = Mathf.Sqrt(width);
            var go = new GameObject("BuildBurst");
            go.transform.position = view.transform.position + new Vector3(0f, width * 0.25f, 0f);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.duration = 0.2f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f * width, 2.6f * width);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f * spark, 0.15f * spark);
            main.startColor = new ParticleSystem.MinMaxGradient(GOLD, PALE);
            main.gravityModifier = 0.55f * width;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = SPARKS;

            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, SPARKS) });

            // Out of its plot and upward, in a fan.
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 32f;
            shape.radius = width * 0.28f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var shrink = system.sizeOverLifetime;
            shrink.enabled = true;
            shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.4f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Material();
            renderer.sortingLayerID = view.SortingLayerId;
            renderer.sortingOrder = ORDER;

            system.Play();
            Object.Destroy(go, 2f);
        }

        // A soft-edged white disc, tinted by each spark's colour.
        private Material Material()
        {
            if (_material != null) return _material;
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Spark", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var d = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f).magnitude / (size / 2f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Pow(Mathf.Clamp01(1f - d), 1.2f) * 255));
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            _material = new Material(Shader.Find("Sprites/Default")) { name = "Build sparks", mainTexture = texture };
            return _material;
        }
    }
}
