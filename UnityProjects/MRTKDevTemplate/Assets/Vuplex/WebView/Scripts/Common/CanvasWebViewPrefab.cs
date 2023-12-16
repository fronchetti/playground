/**
* Copyright (c) 2021 Vuplex Inc. All rights reserved.
*
* Licensed under the Vuplex Commercial Software Library License, you may
* not use this file except in compliance with the License. You may obtain
* a copy of the License at
*
*     https://vuplex.com/commercial-library-license
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
using System;
using UnityEngine;
using UnityEngine.UI;
#if NET_4_6 || NET_STANDARD_2_0
    using System.Threading.Tasks;
#endif

namespace Vuplex.WebView {

    /// <summary>
    /// `CanvasWebViewPrefab` is a prefab that makes it easy to view and interact with web content in a Canvas.
    /// It takes care of creating an `IWebView`, displaying its texture, and handling pointer interactions
    /// from the user (i.e. clicking, dragging, and scrolling). So, all you need to do is specify a URL or HTML to load,
    /// and then the user can view and interact with it. For use outside of a Canvas, see `WebViewPrefab` instead.
    /// </summary>
    /// <remarks>
    /// There are two ways to create a `CanvasWebViewPrefab`:
    /// 1. By dragging CanvasWebViewPrefab.prefab into your scene via the editor and then setting its "Initial URL" property.
    /// 2. Or by creating an instance programmatically with `CanvasWebViewPrefab.Instantiate()`, waiting for
    ///    it to initialize, and then calling methods on its `WebView` property (like `canvasWebViewPrefab.WebView.LoadUrl("https://vuplex.com")`).
    ///
    /// `CanvasWebViewPrefab` handles standard events from Unity's input event system
    /// (like `IPointerDownHandler` and `IScrollHandler`), so it works with input modules that plug into the event system,
    /// like Unity's `StandaloneInputModule` and the Oculus `OVRInputModule`.
    ///
    /// If your use case requires a high degree of customization, you can instead create an `IWebView`
    /// outside of the prefab with `Web.CreateWebView()`.
    /// </remarks>
    [HelpURL("https://developer.vuplex.com/webview/CanvasWebViewPrefab")]
    public class CanvasWebViewPrefab : BaseWebViewPrefab {

        /// <summary>
        /// Sets the webview's initial resolution in pixels per Unity unit.
        /// You can change the resolution to make web content appear larger or smaller.
        /// For more information on scaling web content, see
        /// [this support article](https://support.vuplex.com/articles/how-to-scale-web-content).
        /// </summary>
        [Label("Initial Resolution (px / Unity unit)")]
        [Tooltip("You can change this to make web content appear larger or smaller.")]
        [HideInInspector]
        public float InitialResolution = 1;

        /// <summary>
        /// Allows the scroll sensitivity to be adjusted.
        /// The default sensitivity is 15.
        /// </summary>
        [HideInInspector]
        public float ScrollingSensitivity = 15;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <remarks>
        /// The `WebView` property is available after initialization completes,
        /// which is indicated by the `Initialized` event or `WaitUntilInitialized()` method.
        /// </remarks>
        /// <example>
        /// var canvas = GameObject.Find("Canvas");
        /// canvasWebViewPrefab = CanvasWebViewPrefab.Instantiate();
        /// canvasWebViewPrefab.transform.parent = canvas.transform;
        /// var rectTransform = canvasWebViewPrefab.transform as RectTransform;
        /// rectTransform.anchoredPosition3D = Vector3.zero;
        /// rectTransform.offsetMin = Vector2.zero;
        /// rectTransform.offsetMax = Vector2.zero;
        /// canvasWebViewPrefab.transform.localScale = Vector3.one;
        /// canvasWebViewPrefab.Initialized += (sender, e) => {
        ///     canvasWebViewPrefab.WebView.LoadUrl("https://vuplex.com");
        /// };
        /// </example>
        public static CanvasWebViewPrefab Instantiate() {

            return Instantiate(new WebViewOptions());
        }

        /// <summary>
        /// Like `Instantiate()`, except it also accepts an object
        /// of options flags that can be used to alter the generated webview's behavior.
        /// </summary>
        public static CanvasWebViewPrefab Instantiate(WebViewOptions options) {

            var prefabPrototype = (GameObject) Resources.Load("CanvasWebViewPrefab");
            var gameObject = (GameObject) Instantiate(prefabPrototype);
            var canvasWebViewPrefab = gameObject.GetComponent<CanvasWebViewPrefab>();
            canvasWebViewPrefab._optionsForInitialization = options;
            return canvasWebViewPrefab;
        }

        /// <summary>
        /// Like `Instantiate()`, except it initializes the instance with an existing, initialized
        /// `IWebView` instance. This causes the `CanvasWebViewPrefab` to use the existing
        /// `IWebView` instance instead of creating a new one.
        /// </summary>
        public static CanvasWebViewPrefab Instantiate(IWebView webView) {

            if (!webView.IsInitialized) {
                throw new ArgumentException("CanvasWebViewPrefab.Init(IWebView) was called with an uninitialized webview, but an initialized webview is required.");
            }
            var prefabPrototype = (GameObject) Resources.Load("CanvasWebViewPrefab");
            var gameObject = (GameObject) Instantiate(prefabPrototype);
            var canvasWebViewPrefab = gameObject.GetComponent<CanvasWebViewPrefab>();
            canvasWebViewPrefab._webViewForInitialization = webView;
            return canvasWebViewPrefab;
        }

        [Obsolete("CanvasWebViewPrefab.Init() has been removed. The CanvasWebViewPrefab script now initializes itself automatically, so Init() no longer needs to be called.", true)]
        public void Init() {}

        [Obsolete("CanvasWebViewPrefab.Init() has been removed. The CanvasWebViewPrefab script now initializes itself automatically, so Init() no longer needs to be called.", true)]
        public void Init(WebViewOptions options) {}

        [Obsolete("CanvasWebViewPrefab.Init() has been removed. The CanvasWebViewPrefab script now initializes itself automatically, so Init() no longer needs to be called.", true)]
        public void Init(IWebView webView) {}

        RectTransform _cachedRectTransform;
        WebViewOptions _optionsForInitialization;
        RectTransform _rectTransform {
            get {
                if (_cachedRectTransform == null) {
                    _cachedRectTransform = GetComponent<RectTransform>();
                }
                return _cachedRectTransform;
            }
        }
        bool _setCustomPointerInputDetector;
        IWebView _webViewForInitialization;

        protected override Vector2 _convertRatioPointToUnityUnits(Vector2 point) {

            // Use Vector2.Scale() because Vector2 * Vector2 isn't supported in Unity 2017.
            return Vector2.Scale(point, _rectTransform.rect.size);
        }

        protected override float _getInitialResolution() {

            return InitialResolution;
        }

        protected override float _getScrollingSensitivity() {

            return ScrollingSensitivity;
        }

        protected override ViewportMaterialView _getVideoLayer() {

            return transform.Find("VideoLayer").GetComponent<ViewportMaterialView>();
        }

        protected override ViewportMaterialView _getView() {

            return transform.Find("CanvasWebViewPrefabView").GetComponent<ViewportMaterialView>();
        }

        void _initCanvasPrefab() {

            _logMacWarningIfNeeded();
            var rect = _rectTransform.rect;
            _init(rect.width, rect.height, _optionsForInitialization, _webViewForInitialization);
        }

        void _logMacWarningIfNeeded() {

            if (Web.DefaultPluginType != WebPluginType.Mac) {
                return;
            }
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) {
                return;
            }
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) {
                WebViewLogger.LogWarning("Unity's macOS player currently has a bug that sometimes prevents 3D WebView's external textures from appearing properly in a \"Screen Space - Overlay\" Canvas (https://issuetracker.unity3d.com/issues/external-texture-is-not-visible-in-player-slash-build-when-canvas-render-mode-is-set-to-screen-space-overlay). If you encounter this issue, please either switch the Canvas's render mode to \"Screen Space - Camera\" or add a script to temporarily resize the player's window with Screen.SetResolution().");
            }
        }

        protected override void _setVideoLayerPosition(Rect videoRect) {

            var videoRectTransform = _videoLayer.transform as RectTransform;
            // Use Vector2.Scale() because Vector2 * Vector2 isn't supported in Unity 2017.
            videoRectTransform.anchoredPosition = Vector2.Scale(Vector2.Scale(videoRect.position, _rectTransform.rect.size), new Vector2(1, -1));
            videoRectTransform.sizeDelta = Vector2.Scale(videoRect.size, _rectTransform.rect.size);
        }

        void Start() {

            _initCanvasPrefab();
        }

        void Update() {

            var rect = _rectTransform.rect;
            var size = _webView.Size;
            if (!(rect.width == size.x && rect.height == size.y)) {
                _webView.Resize(rect.width, rect.height);
            }
        }
    }
}
