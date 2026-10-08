using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Modules.Cameras.Tests
{
    public class CameraControllerTests
    {
        private sealed class FakeRig : ICameraRig
        {
            public Vector2 Position { get; set; }
            public float OrthographicSize { get; set; } = 8f;
            public float Aspect => 0.5f;
            public float ScreenHeight => 1600f;
        }

        private sealed class FakeSettings : ICameraSettings
        {
            public float ZoomSensitivity => 1f;
            public float MinZoom => 3f;
            public float MaxZoom => 20f;
            public float ZoomElasticFactor => 0.2f;
            public bool UseInertia { get; set; } = true;
            public float Damping => 5f;
            public Vector2 MinLimit => new(-10f, -10f);
            public Vector2 MaxLimit => new(10f, 10f);
            public float ElasticFactor => 0.4f;
            public float SnapBackSpeed => 10f;
            public float CenterDuration => 0.4f;
        }

        private FakeRig _rig;
        private FakeSettings _settings;
        private CameraController _camera;

        [SetUp]
        public void SetUp()
        {
            _rig = new FakeRig();
            _settings = new FakeSettings();
            _camera = new CameraController(_rig, _settings);
        }

        [Test]
        public void Drag_ShouldMoveTheViewAgainstTheFinger()
        {
            _camera.BeginDrag(new Vector2(500f, 500f));
            _camera.Drag(new Vector2(400f, 500f), 0.016f);

            // 100 px left at 16 world units over 1,600 px: the view moves 1 unit right.
            Assert.That(_rig.Position.X, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Drag_ShouldResistPastTheBounds()
        {
            _rig.Position = new Vector2(10f, 0f);
            _camera.BeginDrag(new Vector2(500f, 500f));
            _camera.Drag(new Vector2(400f, 500f), 0.016f);

            Assert.That(_rig.Position.X, Is.EqualTo(10.4f).Within(0.0001f));
        }

        [Test]
        public void Tick_ShouldSpringBackInsideTheBounds()
        {
            _rig.Position = new Vector2(12f, 0f);

            for (var i = 0; i < 120; i++) _camera.Tick(1f / 60f);

            Assert.That(_rig.Position.X, Is.EqualTo(10f).Within(0.02f));
        }

        [Test]
        public void Scroll_ShouldStopHardAtTheMaximumZoom()
        {
            _rig.OrthographicSize = 19.9f;

            _camera.Scroll(-1200f);

            Assert.That(_rig.OrthographicSize, Is.EqualTo(20f));
        }

        [Test]
        public void Pinch_ShouldBeElasticPastTheMinimumAndSpringBack()
        {
            _rig.OrthographicSize = 3f;

            _camera.Pinch(1.5f);
            var stretched = _rig.OrthographicSize;
            _camera.EndPinch();
            for (var i = 0; i < 120; i++) _camera.Tick(1f / 60f);

            Assert.That(stretched, Is.LessThan(3f).And.GreaterThan(2.5f));
            Assert.That(_rig.OrthographicSize, Is.EqualTo(3f).Within(0.02f));
        }

        [Test]
        public void CenterOn_ShouldGlideToTheClampedPoint()
        {
            _camera.CenterOn(new Vector2(50f, 4f));

            for (var i = 0; i < 60; i++) _camera.Tick(1f / 60f);

            Assert.That(_rig.Position, Is.EqualTo(new Vector2(10f, 4f)));
        }

        [Test]
        public void Inertia_ShouldCarryTheViewAfterADrag()
        {
            _camera.BeginDrag(new Vector2(500f, 500f));
            _camera.Drag(new Vector2(480f, 500f), 0.016f);
            _camera.EndDrag();
            var released = _rig.Position.X;

            _camera.Tick(0.016f);

            Assert.That(_rig.Position.X, Is.GreaterThan(released));
        }
    }
}
