using UnityEngine;

public class EtherLine : MonoBehaviour
{
    public Transform target;             // The object you connect to
    public int surroundingLineCount = 4; // Number of lines around the center (3-4 recommended)
    public int segments = 20;            // How many points per line
    public float waveAmplitude = 0.3f;   // How big the "wiggle" is
    public float startRadius = 1.5f;     // Radius at the start point (spread out more)
    public float endRadius = 0.2f;       // Radius at the target (converge)
    public float rotationSpeed = 30f;    // Degrees per second rotation
    public float wiggleSpeed = 1f;       // Speed of the slow wiggle
    
    [Header("Materials")]
    public Material centerLineMaterial;  // High intensity material for center line
    public Material surroundingLineMaterial; // Lower intensity material for outer lines
    
    [Header("Line Properties")]
    public float centerLineWidth = 0.08f;      // Thicker center line
    public float surroundingLineWidth = 0.03f; // Thinner outer lines
    public float centerIntensityMultiplier = 2f;  // Brightness multiplier for center
    public float surroundingIntensityMultiplier = 0.4f; // Dimmer outer lines

    [Header("Particle Effects")]
    public ParticleSystem sparkleParticles;  // assign in inspector
    public ParticleSystem beamGlow;         // optional beam glow effect

    private LineRenderer centerLineRenderer;
    private LineRenderer[] surroundingLineRenderers;
    private float rotationOffset = 0f;

    void Start()
    {
        CreateLines();
    }

    void CreateLines()
    {
        // Create center line
        GameObject centerLineObj = new GameObject("CenterEtherLine");
        centerLineObj.transform.SetParent(transform);
        centerLineObj.transform.localPosition = Vector3.zero;
        
        centerLineRenderer = centerLineObj.AddComponent<LineRenderer>();
        centerLineRenderer.positionCount = segments;
        centerLineRenderer.startWidth = centerLineWidth;
        centerLineRenderer.endWidth = centerLineWidth * 0.6f; // slightly taper
        centerLineRenderer.useWorldSpace = true;
        centerLineRenderer.material = centerLineMaterial;
        
        // Set center line color (bright white/cyan with full intensity)
        Color centerColor = Color.white;
        centerColor.a = 1f;
        centerLineRenderer.startColor = centerColor;
        centerLineRenderer.endColor = centerColor;

        // Create surrounding lines
        surroundingLineRenderers = new LineRenderer[surroundingLineCount];
        
        for (int i = 0; i < surroundingLineCount; i++)
        {
            GameObject lineObj = new GameObject($"SurroundingEtherLine_{i}");
            lineObj.transform.SetParent(transform);
            lineObj.transform.localPosition = Vector3.zero;
            
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = segments;
            lr.startWidth = surroundingLineWidth;
            lr.endWidth = surroundingLineWidth * 0.5f;
            lr.useWorldSpace = true;
            lr.material = surroundingLineMaterial;
            
            // Set surrounding line color (dimmer with slight color variation)
            Color baseColor = Color.cyan;
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            h = (h + (i / (float)surroundingLineCount) * 0.2f) % 1f; // slight hue variation
            Color lineColor = Color.HSVToRGB(h, s * 0.7f, v * surroundingIntensityMultiplier);
            lineColor.a = 0.6f;
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            
            surroundingLineRenderers[i] = lr;
        }
    }

    void Update()
    {
        if (target == null) return;

        // Update rotation
        rotationOffset += rotationSpeed * Time.deltaTime;

        Vector3 start = transform.position;
        Vector3 end = target.position;
        Vector3 direction = (end - start).normalized;
        Vector3 perpendicular1 = Vector3.Cross(direction, Vector3.up).normalized;
        Vector3 perpendicular2 = Vector3.Cross(direction, perpendicular1).normalized;

        // Update center line (straight line with minimal wiggle and subtle rotation)
        if (centerLineRenderer != null)
        {
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                Vector3 pos = Vector3.Lerp(start, end, t);
                
                // Add subtle spiral rotation to center line
                float spiralAngle = rotationOffset * 2f + t * 45f; // slow rotation + slight twist along length
                float spiralRadius = 0.1f * (1 - t); // very small radius that fades toward target
                Vector3 spiralOffset = (perpendicular1 * Mathf.Cos(spiralAngle * Mathf.Deg2Rad) + 
                                       perpendicular2 * Mathf.Sin(spiralAngle * Mathf.Deg2Rad)) * spiralRadius;
                
                // Very subtle wiggle for the center line
                float centerWigglePhase = Time.time * wiggleSpeed * 0.3f + i * 0.1f;
                Vector3 centerWiggle = (perpendicular1 * Mathf.Sin(centerWigglePhase) + 
                                       perpendicular2 * Mathf.Cos(centerWigglePhase * 0.8f)) * 
                                       waveAmplitude * 0.1f; // much smaller wiggle
                
                pos += spiralOffset + centerWiggle;
                centerLineRenderer.SetPosition(i, pos);
            }
        }

        // Update surrounding lines
        for (int lineIndex = 0; lineIndex < surroundingLineRenderers.Length; lineIndex++)
        {
            LineRenderer lr = surroundingLineRenderers[lineIndex];
            
            // Calculate this line's position in the circle
            float lineAngle = (lineIndex / (float)surroundingLineCount) * 360f + rotationOffset;
            Vector3 circleDirection = perpendicular1 * Mathf.Cos(lineAngle * Mathf.Deg2Rad) + 
                                     perpendicular2 * Mathf.Sin(lineAngle * Mathf.Deg2Rad);

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                Vector3 pos = Vector3.Lerp(start, end, t);
                
                // Interpolate radius from wide start to narrow end
                float currentRadius = Mathf.Lerp(startRadius, endRadius, t * t);
                Vector3 circleOffset = circleDirection * currentRadius;
                pos += circleOffset;
                
                // More pronounced wiggle for surrounding lines
                float wigglePhase = Time.time * wiggleSpeed + lineIndex * 0.8f + i * 0.15f;
                float wiggleStrength = waveAmplitude * (1 - t * 0.7f);
                
                Vector3 wiggleOffset = (perpendicular1 * Mathf.Sin(wigglePhase) + 
                                       perpendicular2 * Mathf.Cos(wigglePhase * 1.3f)) * wiggleStrength;
                
                pos += wiggleOffset;
                lr.SetPosition(i, pos);
            }
        }
    }

    public void StartEffect(Transform targetObject)
    {
        target = targetObject;
        gameObject.SetActive(true);
        
        // Start particle effects
        if (sparkleParticles != null) sparkleParticles.Play();
        if (beamGlow != null) beamGlow.Play();
    }

    public void StopEffect()
    {
        // Stop particle effects
        if (sparkleParticles != null) sparkleParticles.Stop();
        if (beamGlow != null) beamGlow.Stop();
        
        gameObject.SetActive(false);
        target = null;
    }

    void OnDestroy()
    {
        if (centerLineRenderer != null && centerLineRenderer.gameObject != null)
            DestroyImmediate(centerLineRenderer.gameObject);
            
        if (surroundingLineRenderers != null)
        {
            for (int i = 0; i < surroundingLineRenderers.Length; i++)
            {
                if (surroundingLineRenderers[i] != null && surroundingLineRenderers[i].gameObject != null)
                    DestroyImmediate(surroundingLineRenderers[i].gameObject);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, target.position); // center line
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, startRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target.position, endRadius);
    }
}
