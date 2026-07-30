using TheBadge.Greybox.Sim;
using UnityEngine;

namespace TheBadge.Greybox.View
{
    /// <summary>
    /// Portre kamera: dikey sahayı ekrana sığdırır (plan kararı: portre/dikey saha).
    /// Gol vurgusunun titreme yarısı burada (Brif K2: yavaşlatma + titreme).
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        Camera cam;
        float shakeTimer, shakeDuration, shakeAmplitude;
        GreyboxBalance bal;

        public Camera Cam => cam;

        public static CameraRig Create(GreyboxBalance bal)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var rig = go.AddComponent<CameraRig>();
            rig.bal = bal;
            rig.cam = go.AddComponent<Camera>();
            rig.cam.orthographic = true;
            rig.cam.clearFlags = CameraClearFlags.SolidColor;
            rig.cam.backgroundColor = SpriteFactory.Hex(bal.renkler.arkaplan, new Color(0.08f, 0.2f, 0.12f));
            rig.cam.nearClipPlane = -10f;
            rig.cam.farClipPlane = 100f;
            go.transform.position = new Vector3(0f, 0f, -10f);
            return rig;
        }

        public void Shake()
        {
            shakeAmplitude = bal.vurgu.shakeGenlikM;
            shakeDuration = shakeTimer = bal.vurgu.shakeSureSn;
        }

        void LateUpdate()
        {
            // Saha 68x105; kenarlarda küçük pay bırak. Dar kenar (genişlik) portrede belirleyicidir.
            float aspect = cam.aspect;
            float needHalfH = FlowSim.PitchL * 0.5f * 1.04f;
            float needHalfHForW = (FlowSim.PitchW * 0.5f * 1.06f) / Mathf.Max(0.2f, aspect);
            cam.orthographicSize = Mathf.Max(needHalfH, needHalfHForW);

            Vector3 pos = new Vector3(0f, 0f, -10f);
            if (shakeTimer > 0f)
            {
                shakeTimer -= Time.deltaTime;
                float fade = Mathf.Clamp01(shakeTimer / Mathf.Max(0.01f, shakeDuration));
                float t = Time.time * 34f;
                pos.x += (Mathf.PerlinNoise(t, 0.3f) - 0.5f) * 2f * shakeAmplitude * fade;
                pos.y += (Mathf.PerlinNoise(0.7f, t) - 0.5f) * 2f * shakeAmplitude * fade;
            }
            transform.position = pos;
        }
    }
}
