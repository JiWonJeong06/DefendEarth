using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BouncyUFO
{
    /// <summary>
    /// 어떤 화면 비율이든 플레이 영역 전체가 보이게 카메라를 맞춘다.
    /// Pixel Perfect Camera가 있으면 그쪽이 정수 배율로 맞추고(도트가 고르게 보임),
    /// 없으면 여기서 화면에 들어가는 가장 큰 크기로 맞춘다.
    /// 얇은 폰은 위아래, 넓은 화면(태블릿·폴더블·가로 창)은 양옆에 여백이 생긴다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-900)]
    public sealed class PlayAreaCamera : MonoBehaviour
    {
        public static PlayAreaCamera Main { get; private set; }

        private Camera cam;
        private PixelPerfectCamera pixelPerfect;

        public Camera Camera => cam;

        /// <summary>화면 1픽셀이 월드 몇 유닛인지.</summary>
        public float UnitsPerScreenPixel
        {
            get
            {
                float ortho = (pixelPerfect != null && pixelPerfect.isActiveAndEnabled && pixelPerfect.orthographicSize > 0f)
                    ? pixelPerfect.orthographicSize
                    : cam.orthographicSize;
                return ortho * 2f / Mathf.Max(1, cam.pixelHeight);
            }
        }

        /// <summary>
        /// 지금 화면에서 플레이 영역 너비가 몇 화면 픽셀인지.
        /// 드래그 감도와 탭 판정을 이 값의 비율로 계산해서 기기마다 손맛이 같게 한다.
        /// </summary>
        public float PlayAreaWidthInScreenPixels => PlayArea.Size.x / UnitsPerScreenPixel;

        private void OnEnable() => Main = this;

        private void OnDisable()
        {
            if (Main == this) Main = null;
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            pixelPerfect = GetComponent<PixelPerfectCamera>();
            cam.orthographic = true;
            transform.position = new Vector3(0f, 0f, transform.position.z);
            if (pixelPerfect != null && PlayArea.Config != null)
            {
                pixelPerfect.assetsPPU = PlayArea.Config.pixelsPerUnit;
                pixelPerfect.refResolutionX = PlayArea.Config.playAreaPixels.x;
                pixelPerfect.refResolutionY = PlayArea.Config.playAreaPixels.y;
                pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.None;
            }
            Fit();
        }

        private void LateUpdate() => Fit();

        private void Fit()
        {
            if (pixelPerfect != null && pixelPerfect.isActiveAndEnabled) return;
            float aspect = cam.aspect > 0f ? cam.aspect : 9f / 16f;
            Vector2 size = PlayArea.Size;
            cam.orthographicSize = Mathf.Max(size.y * 0.5f, size.x * 0.5f / aspect);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Rect r = PlayArea.WorldRect;
            Gizmos.DrawWireCube(r.center, r.size);
        }
    }
}
