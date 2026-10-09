using TMPro;
using UnityEngine;

namespace Codigames.Game.UI.Kit
{
    // The call to action (the web's k-cta): a red enamel stud on its host's corner that gives a small nudge every few
    // seconds — each badge at its own phase, so no two nudge in step — with the count on it when more than one thing
    // waits ("2" … "9", then "9+").
    public class CtaBadge : MonoBehaviour
    {
        private const float LOOP = 6.4f;

        [SerializeField] private TMP_Text _count;

        private float _phase;

        public void Show(int count)
        {
            gameObject.SetActive(count > 0);
            _count.text = count > 9 ? "9+" : count > 1 ? count.ToString() : string.Empty;
        }

        private void OnEnable() => _phase = Random.value * LOOP;

        // The web's k-cta-nudge: still until 82% of the loop, then a swell, a bounce and a settle.
        private void Update()
        {
            var t = Mathf.Repeat(Time.unscaledTime + _phase, LOOP) / LOOP;
            float scale, angle;
            if (t < 0.82f) (scale, angle) = (1f, 0f);
            else if (t < 0.86f) (scale, angle) = (Mathf.Lerp(1f, 1.2f, (t - 0.82f) / 0.04f), Mathf.Lerp(0f, 6f, (t - 0.82f) / 0.04f));
            else if (t < 0.90f) (scale, angle) = (Mathf.Lerp(1.2f, 0.94f, (t - 0.86f) / 0.04f), Mathf.Lerp(6f, -3f, (t - 0.86f) / 0.04f));
            else if (t < 0.94f) (scale, angle) = (Mathf.Lerp(0.94f, 1.06f, (t - 0.90f) / 0.04f), Mathf.Lerp(-3f, 0f, (t - 0.90f) / 0.04f));
            else (scale, angle) = (Mathf.Lerp(1.06f, 1f, (t - 0.94f) / 0.06f), 0f);

            transform.localScale = new Vector3(scale, scale, 1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
