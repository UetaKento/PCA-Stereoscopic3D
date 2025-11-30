using Meta.XR;
using System.Collections;
using UnityEngine;

public class CreateSBSTexture : MonoBehaviour
{
    [SerializeField] private PassthroughCameraAccess _leftCameraAccess;
    [SerializeField] private PassthroughCameraAccess _rightCameraAccess;
    [SerializeField] private Material _stereoTargetMaterial;

    private RenderTexture _stereoTexture;
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    private void OnEnable()
    {
        StartCoroutine(EnsureStreams());
    }

    private IEnumerator EnsureStreams()
    {
        while (!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess))
        {
            yield return null;
        }

        StartCoroutine(StreamEye(_leftCameraAccess, _rightCameraAccess));
    }

    private IEnumerator StreamEye(PassthroughCameraAccess accessL, PassthroughCameraAccess accessR)
    {
        if (!accessL || !accessR || !_stereoTargetMaterial)
        {
            yield break;
        }

        while (isActiveAndEnabled)
        {
            if (!accessL.IsPlaying || !accessR.IsPlaying)
            {
                yield return null;
                continue;
            }

            var leftTexture = accessL.GetTexture();
            var rightTexture = accessR.GetTexture();
            if (!leftTexture || !rightTexture)
            {
                yield return null;
                continue;
            }

            EnsureStereoTarget(leftTexture.width, leftTexture.height, rightTexture.width, rightTexture.height);

            Graphics.SetRenderTarget(_stereoTexture);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, _stereoTexture.width, 0, _stereoTexture.height);
            GL.Clear(false, true, Color.clear);
            Graphics.DrawTexture(new Rect(0, 0, leftTexture.width, leftTexture.height), leftTexture);
            Graphics.DrawTexture(new Rect(leftTexture.width, 0, rightTexture.width, rightTexture.height), rightTexture);
            GL.PopMatrix();
            Graphics.SetRenderTarget(null);

            _stereoTargetMaterial.SetTexture(MainTexId, _stereoTexture);
            yield return null;
        }
    }

    private void EnsureStereoTarget(int leftW, int leftH, int rightW, int rightH)
    {
        int targetWidth = leftW + rightW;
        int targetHeight = Mathf.Max(leftH, rightH);

        if (_stereoTexture && (_stereoTexture.width != targetWidth || _stereoTexture.height != targetHeight))
        {
            _stereoTexture.Release();
            Destroy(_stereoTexture);
            _stereoTexture = null;
        }

        if (!_stereoTexture)
        {
            _stereoTexture = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            _stereoTexture.Create();
        }
    }

    private void OnDisable()
    {
        if (_stereoTexture)
        {
            _stereoTexture.Release();
            Destroy(_stereoTexture);
            _stereoTexture = null;
        }
    }
}
