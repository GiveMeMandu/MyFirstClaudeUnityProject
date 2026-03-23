using UnityEngine;
using UnityEngine.Rendering;

public class DayNightController : MonoBehaviour
{
    [Header("Scene References")]
    public Transform sunSphere;
    public Light sunLight;
    public Camera mainCamera;

    [Header("Timing")]
    public float dayDuration = 20f;

    [Header("Arc Settings")]
    public float arcRadius = 15f;

    private Transform _pivot;

    private static readonly Color SkyDawn  = new Color(0.95f, 0.45f, 0.15f);
    private static readonly Color SkyDay   = new Color(0.40f, 0.65f, 0.95f);
    private static readonly Color SkyDusk  = new Color(0.90f, 0.35f, 0.10f);
    private static readonly Color SkyNight = new Color(0.02f, 0.02f, 0.06f);

    private static readonly Color LightDawn  = new Color(1.0f, 0.60f, 0.30f);
    private static readonly Color LightDay   = new Color(1.0f, 0.98f, 0.90f);
    private static readonly Color LightDusk  = new Color(1.0f, 0.55f, 0.20f);
    private static readonly Color LightNight = new Color(0.05f, 0.05f, 0.10f);

    private static readonly Color AmbDawn  = new Color(0.30f, 0.18f, 0.12f);
    private static readonly Color AmbDay   = new Color(0.22f, 0.28f, 0.35f);
    private static readonly Color AmbDusk  = new Color(0.28f, 0.15f, 0.10f);
    private static readonly Color AmbNight = new Color(0.01f, 0.01f, 0.03f);

    private static readonly Color SunColDawn  = new Color(1.0f, 0.55f, 0.05f);
    private static readonly Color SunColDay   = new Color(1.0f, 0.95f, 0.60f);
    private static readonly Color SunColDusk  = new Color(1.0f, 0.40f, 0.05f);
    private static readonly Color SunColNight = Color.black;

    private Material _sunMat;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;

        var pivotGO = new GameObject("SunPivot");
        _pivot = pivotGO.transform;

        Vector3 pivotPos = mainCamera.transform.position
                         + mainCamera.transform.forward * arcRadius * 2f;
        pivotPos.y = 0f;
        _pivot.position = pivotPos;
        _pivot.rotation = Quaternion.LookRotation(mainCamera.transform.forward, Vector3.up);

        if (sunSphere != null)
        {
            sunSphere.SetParent(_pivot);
            sunSphere.localPosition = new Vector3(0f, arcRadius, 0f);
            sunSphere.localRotation = Quaternion.identity;
        }

        var rend = sunSphere != null ? sunSphere.GetComponent<Renderer>() : null;
        _sunMat = rend != null ? rend.material : null;

        RenderSettings.ambientMode = AmbientMode.Flat;
    }

    void Update()
    {
        float t = (Time.time % dayDuration) / dayDuration;

        // Z축 회전: t=0 → 동쪽(왼쪽) 지평선, t=0.25 → 정점, t=0.5 → 서쪽(오른쪽) 지평선
        // angle=90 → sun localPos(0,R,0)이 Z회전으로 (-R,0,0) = 동쪽
        float angle = 90f - t * 360f;
        _pivot.localRotation = Quaternion.Euler(0f, 0f, angle);

        Color skyCol, lightCol, ambCol, sunCol;
        float intensity;
        bool aboveHorizon;
        EvaluateDayState(t, out skyCol, out lightCol, out ambCol, out sunCol, out intensity, out aboveHorizon);

        mainCamera.backgroundColor = skyCol;

        if (sunLight != null)
        {
            sunLight.color = lightCol;
            sunLight.intensity = intensity;
            if (sunSphere != null && aboveHorizon)
            {
                Vector3 toSun = (sunSphere.position - mainCamera.transform.position).normalized;
                sunLight.transform.rotation = Quaternion.LookRotation(-toSun);
            }
        }

        RenderSettings.ambientLight = ambCol;

        if (_sunMat != null)
            _sunMat.SetColor("_BaseColor", sunCol);

        if (sunSphere != null)
            sunSphere.gameObject.SetActive(aboveHorizon);
    }

    private void EvaluateDayState(
        float t,
        out Color skyCol, out Color lightCol,
        out Color ambCol, out Color sunCol,
        out float intensity, out bool aboveHorizon)
    {
        if (t < 0.04f)
        {
            float s = t / 0.04f;
            skyCol = Color.Lerp(SkyNight, SkyDawn, s);
            lightCol = Color.Lerp(LightNight, LightDawn, s);
            ambCol = Color.Lerp(AmbNight, AmbDawn, s);
            sunCol = Color.Lerp(SunColNight, SunColDawn, s);
            intensity = Mathf.Lerp(0f, 0.5f, s);
            aboveHorizon = false;
        }
        else if (t < 0.12f)
        {
            float s = (t - 0.04f) / 0.08f;
            skyCol = Color.Lerp(SkyDawn, SkyDay, s);
            lightCol = Color.Lerp(LightDawn, LightDay, s);
            ambCol = Color.Lerp(AmbDawn, AmbDay, s);
            sunCol = Color.Lerp(SunColDawn, SunColDay, s);
            intensity = Mathf.Lerp(0.5f, 1.0f, s);
            aboveHorizon = true;
        }
        else if (t < 0.42f)
        {
            skyCol = SkyDay;
            lightCol = LightDay;
            ambCol = AmbDay;
            sunCol = SunColDay;
            intensity = 1.0f;
            aboveHorizon = true;
        }
        else if (t < 0.50f)
        {
            float s = (t - 0.42f) / 0.08f;
            skyCol = Color.Lerp(SkyDay, SkyDusk, s);
            lightCol = Color.Lerp(LightDay, LightDusk, s);
            ambCol = Color.Lerp(AmbDay, AmbDusk, s);
            sunCol = Color.Lerp(SunColDay, SunColDusk, s);
            intensity = Mathf.Lerp(1.0f, 0.5f, s);
            aboveHorizon = true;
        }
        else if (t < 0.58f)
        {
            float s = (t - 0.50f) / 0.08f;
            skyCol = Color.Lerp(SkyDusk, SkyNight, s);
            lightCol = Color.Lerp(LightDusk, LightNight, s);
            ambCol = Color.Lerp(AmbDusk, AmbNight, s);
            sunCol = Color.Lerp(SunColDusk, SunColNight, s);
            intensity = Mathf.Lerp(0.5f, 0f, s);
            aboveHorizon = s < 0.6f;
        }
        else
        {
            skyCol = SkyNight;
            lightCol = LightNight;
            ambCol = AmbNight;
            sunCol = SunColNight;
            intensity = 0f;
            aboveHorizon = false;
        }
    }
}
