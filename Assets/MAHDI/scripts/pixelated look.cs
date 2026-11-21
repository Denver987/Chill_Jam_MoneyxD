using UnityEngine;
using UnityEngine.UI;

public class pixelatedlook : MonoBehaviour
{

    [SerializeField] private Camera _camera;
    [SerializeField] private RawImage _rawImage;

    [SerializeField] private int _cameraHeight;
    [SerializeField, Range(1f, 1.3f)] private float clarityScale = 1.18f;

    private RenderTexture _renderTexture;
    private int screenWidth;
    private int screenHeight;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateRenderTexture();
    }

    public void UpdateRenderTexture()
    {
        if (_camera == null || _rawImage == null || Screen.height <= 0) return;
        if(_renderTexture != null )
        {
            _camera.targetTexture = null;
            _renderTexture.Release( );  
            Destroy(_renderTexture);
        }

        float aspectRatio = (float)Screen.width / Screen.height;
        int height = Mathf.Clamp(Mathf.RoundToInt(_cameraHeight * clarityScale), 64, Screen.height);
        int cameraWidth = Mathf.Max(1, Mathf.RoundToInt(aspectRatio * height));

        _renderTexture = new RenderTexture(cameraWidth, height, 16, RenderTextureFormat.ARGB32);
        _renderTexture.filterMode= FilterMode.Point;

        _renderTexture.Create();
        _camera.targetTexture= _renderTexture;
        _rawImage.texture= _renderTexture;
        screenWidth = Screen.width;
        screenHeight = Screen.height;
    }

    private void Update()
    {
        if (Screen.width != screenWidth || Screen.height != screenHeight) UpdateRenderTexture();
    }

    private void OnDestroy()
    {
        if (_renderTexture == null) return;
        if (_camera != null && _camera.targetTexture == _renderTexture) _camera.targetTexture = null;
        _renderTexture.Release();
        Destroy(_renderTexture);
    }
}
