using UnityEngine;

public class CardRenderer : MonoBehaviour {
    
    [SerializeField] private MeshRenderer content;
    [SerializeField] private MeshRenderer outline;
    [SerializeField] private ParticleSystemRenderer particles;

    private MaterialPropertyBlock contentMaterialProperties;
    private MaterialPropertyBlock outlineMaterialProperties;
    private MaterialPropertyBlock particleMaterialProperties;
    private ParticleSystem outlineParticles;

    void Awake() {
        if (particles != null) outlineParticles = particles.GetComponent<ParticleSystem>();
        contentMaterialProperties = new MaterialPropertyBlock();
        outlineMaterialProperties = new MaterialPropertyBlock();
        particleMaterialProperties = new MaterialPropertyBlock();
    }

    public void SetOutlineEnabled(bool enabled) {
        outline.enabled = enabled;
        if (outlineParticles == null) return;
        if (enabled) outlineParticles.Play();
        else outlineParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void SetOutlineColor(Color color) {
        outlineMaterialProperties.SetColor("_EmissionColor", color);
        particleMaterialProperties.SetColor("_EmissionColor", color);
        
        outline.SetPropertyBlock(outlineMaterialProperties);
        if (particles != null) particles.SetPropertyBlock(particleMaterialProperties);
    }
}
