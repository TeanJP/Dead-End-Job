using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VacuumEffectManager : MonoBehaviour
{
    [SerializeField]
    private int lineRendererCount = 10;

    [SerializeField]
    private Material lineRendererMaterial = null;

    private List<LineRenderer> lineRenderers = new List<LineRenderer>();

    private static VacuumEffectManager vacuumEffectManagerInstance;

    public static VacuumEffectManager Instance
    {
        get
        {
            return vacuumEffectManagerInstance;
        }
    }

    void Awake()
    {
        if (vacuumEffectManagerInstance != null && vacuumEffectManagerInstance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            vacuumEffectManagerInstance = this;
        }
    }

    void Start()
    {
        for (int i = 0; i < lineRendererCount; i++)
        {
            CreateVacuumEffect();
        }
    }

    public List<LineRenderer> GetVacuumEffects(int count)
    {
        if (count > lineRenderers.Count)
        {
            for (int i = 0; i <= (count - lineRenderers.Count); i++)
            {
                CreateVacuumEffect();
            }
        }

        List<LineRenderer> activeEffects = new List<LineRenderer>();

        for (int i = 0; i < count; i++)
        {
            activeEffects.Add(lineRenderers[i]);
            activeEffects[i].gameObject.SetActive(true);
        }

        return activeEffects;
    }

    private void CreateVacuumEffect()
    {
        GameObject temp = new GameObject();
        temp.name = "Vacuum Effect (" + lineRenderers.Count + ")";
        LineRenderer lineRenderer = temp.AddComponent<LineRenderer>();
        lineRenderer.material = lineRendererMaterial;
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.startWidth = 0.15f;
        lineRenderer.sortingOrder = -1;
        lineRenderers.Add(lineRenderer);
        temp.transform.parent = gameObject.transform;
        temp.SetActive(false);
    }
}
