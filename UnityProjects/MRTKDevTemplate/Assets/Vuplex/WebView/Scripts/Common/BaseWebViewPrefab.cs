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
using UnityEngine.EventSystems;
#if NET_4_6 || NET_STANDARD_2_0
    using System.Threading.Tasks;
#endif

namespace Vuplex.WebView {

    public abstract class BaseWebViewPrefab : MonoBehaviour {

        /// <summary>
        /// Indicates that the prefab was clicked. Note that the prefab automatically
        /// calls the `IWebView.Click()` method for you.
        /// </summary>
        public event EventHandler<ClickedEventArgs> Clicked;

        /// <summary>
        /// Indicates that the prefab finished initializing,
        /// so its `WebView` property is ready to use.
        /// </summary>
        /// <seealso cref="WaitUntilInitialized"/>
        public event EventHandler Initialized;

        /// <summary>
        /// Indicates that the prefab was scrolled. Note that the prefab automatically
        /// calls the `IWebView.Scroll()` method for you.
        /// </summary>
        public event EventHandler<ScrolledEventArgs> Scrolled;

        /// <summary>
        /// If you drag the prefab into the scene via the editor,
        /// you can set this property to make it so that the instance
        /// automatically initializes itself with the given URL. To load a new URL
        /// after the prefab has been initialized, use `IWebView.LoadUrl()` instead.
        /// </summary>
        [Label("Initial URL to load (optional)")]
        [Tooltip("Or you can leave the Initial URL blank if you want to initialize the prefab programmatically by calling Init().")]
        [HideInInspector]
        public string InitialUrl;

        [Header("Other Settings")]
        /// <summary>
        /// Determines how the prefab handles drag interactions.
        /// </summary>
        [Tooltip("Note: \"Drag Within Page\" is not supported on iOS or UWP.")]
        public DragMode DragMode = DragMode.DragToScroll;

        /// <summary>
        /// Clicking is enabled by default, but can be disabled by
        /// setting this property to `false`.
        /// </summary>
        public bool ClickingEnabled = true;

        /// <summary>
        /// Hover interactions are enabled by default, but can be disabled by
        /// setting this property to `false`.
        /// Note that hovering only works for webview implementations that
        /// support the `IWithMovablePointer` interface (i.e. Android, Windows, and macOS).
        /// </summary>
        [Tooltip("Note: Hovering is not supported on iOS or UWP.")]
        public bool HoveringEnabled = true;

        /// <summary>
        /// Scrolling is enabled by default, but can be disabled by
        /// setting this property to `false`.
        /// </summary>
        public bool ScrollingEnabled = true;

        /// <summary>
        /// Determines the threshold (in web pixels) for triggering a drag. The default is 20.
        /// </summary>
        /// <remarks>
        /// When the `DragMode` is set to `DragToScroll`, this property determines
        /// the distance that the pointer must drag before it's no longer
        /// considered a click.
        /// </remarks>
        /// <remarks>
        /// When the `DragMode` is set to `DragWithinPage`, this property determines
        /// the distance that the pointer must drag before it triggers
        /// a drag within the page.
        /// </remarks>
        [Label("Drag Threshold (px)")]
        [Tooltip("Determines the threshold (in web pixels) for triggering a drag.")]
        public float DragThreshold = 20;

        [Obsolete("The WebViewPrefab.DragToScrollThreshold property is obsolete. Please use DragThreshold instead.")]
        public float DragToScrollThreshold { get; set; }

        [Header("Debugging")]
        /// <summary>
        /// Determines whether remote debugging is enabled with `Web.EnableRemoteDebugging()`.
        /// </summary>
        [Tooltip("Determines whether remote debugging is enabled with Web.EnableRemoteDebugging().")]
        public bool RemoteDebuggingEnabled = false;

        /// <summary>
        /// Determines whether JavaScript console messages from `IWebView.ConsoleMessageLogged`
        /// are printed to the Unity logs.
        /// <summary>
        [Tooltip("Determines whether JavaScript console messages are printed to the Unity logs.")]
        public bool LogConsoleMessages = false;

        /// <summary>
        /// The prefab's material.
        /// </summary>
        public Material Material {
            get {
                return _view.Material;
            }
            set {
                _view.Material = value;
            }
        }

        /// <summary>
        /// Controls whether the instance is visible or hidden.
        /// </summary>
        public virtual bool Visible {
            get {
                return _view.Visible;
            }
            set {
                _view.Visible = value;
                if (_videoLayer != null) {
                    _videoLayer.Visible = value;
                }
            }
        }

        /// <summary>
        /// A reference to the prefab's `IWebView` instance, which
        /// is available after the `Initialized` event is raised.
        /// Before initialization is complete, this property is `null`.
        /// </summary>
        public IWebView WebView { get { return _webView; }}

        /// <summary>
        /// Destroys the instance and its children. Note that you don't have
        /// to call this method if you destroy the instance's parent with
        /// `Object.Destroy()`.
        /// </summary>
        public void Destroy() {

            UnityEngine.Object.Destroy(gameObject);
        }

        public void SetCutoutRect(Rect rect) {

            _view.SetCutoutRect(rect);
        }

        /// <summary>
        /// By default, the prefab detects pointer input events like clicks through
        /// Unity's event system, but you can use this method to override the way that
        /// input events are detected.
        /// </summary>
        public void SetPointerInputDetector(IPointerInputDetector pointerInputDetector) {

            var previousPointerInputDetector = _pointerInputDetector;
            _pointerInputDetector = pointerInputDetector;
            // If _webView hasn't been set yet, then _initPointerInputDetector
            // will get called before it's set to initialize _pointerInputDetector.
            if (_webView != null) {
                _initPointerInputDetector(_webView, previousPointerInputDetector);
            }
        }

    #if NET_4_6 || NET_STANDARD_2_0
        /// <summary>
        /// Returns a task that resolves when the prefab is initialized
        /// (i.e. when its `WebView` property is ready for use).
        /// </summary>
        /// <seealso cref="Initialized"/>
        public Task WaitUntilInitialized() {

            var taskCompletionSource = new TaskCompletionSource<bool>();
            var isInitialized = _webView != null;
            if (isInitialized) {
                taskCompletionSource.SetResult(true);
            } else {
                Initialized += (sender, e) => taskCompletionSource.SetResult(true);
            }
            return taskCompletionSource.Task;
        }
    #endif

        [SerializeField]
        [HideInInspector]
        ViewportMaterialView _cachedVideoLayer;
        [SerializeField]
        [HideInInspector]
        ViewportMaterialView _cachedView;
        IWebView _cachedWebView;
        // Used for DragMode.DragToScroll and DragMode.Disabled
        bool _clickIsPending;
        bool _consoleMessageLoggedHandlerAttached;
        bool _loggedDragWarning;
        WebViewOptions _options;
        [SerializeField]
        [HideInInspector]
        GameObject _pointerInputDetectorGameObject;
        IPointerInputDetector _pointerInputDetector {
            get {
                return _pointerInputDetectorGameObject == null ? null :
                                                                 _pointerInputDetectorGameObject.GetComponent<IPointerInputDetector>();
            }
            set {
                var monoBehaviour = value as MonoBehaviour;
                if (monoBehaviour == null) {
                    throw new ArgumentException("The provided IPointerInputDetector can't be successfully set because it's not a MonoBehaviour");
                }
                _pointerInputDetectorGameObject = monoBehaviour.gameObject;
            }
        }
        bool _pointerIsDown;
        Vector2 _pointerDownRatioPoint;
        Vector2 _previousDragPoint;
        Vector2 _previousMovePointerPoint;
        static bool _remoteDebuggingEnabled;
        protected ViewportMaterialView _videoLayer {
            get {
                if (_cachedVideoLayer == null) {
                    _cachedVideoLayer = _getVideoLayer();
                }
                return _cachedVideoLayer;
            }
        }
        bool _videoLayerDisabled;
        Material _videoMaterial;
        protected ViewportMaterialView _view {
            get {
                if (_cachedView == null) {
                    _cachedView = _getView();
                }
                return _cachedView;
            }
        }
        Material _viewMaterial;
        [SerializeField]
        [HideInInspector]
        GameObject _webViewGameObject;
        protected IWebView _webView {
            get {
                if (_cachedWebView == null) {
                    if (_webViewGameObject == null) {
                        return null;
                    }
                    _cachedWebView = _webViewGameObject.GetComponent<IWebView>();
                }
                return _cachedWebView;
            }
            set {
                var monoBehaviour = value as MonoBehaviour;
                if (monoBehaviour == null) {
                    throw new ArgumentException("The IWebView cannot be set successfully because it's not a MonoBehaviour.");
                }
                _webViewGameObject = monoBehaviour.gameObject;
                _cachedWebView = value;
            }
        }

        void _attachWebViewEventHandlers(IWebView webView) {

            if (!_options.disableVideo) {
                webView.VideoRectChanged += (sender, e) => _setVideoRect(e.Value);
            }
            if (LogConsoleMessages) {
                _consoleMessageLoggedHandlerAttached = true;
                webView.ConsoleMessageLogged += WebView_ConsoleMessageLogged;
            }
        }

        protected abstract Vector2 _convertRatioPointToUnityUnits(Vector2 point);

        protected abstract float _getInitialResolution();

        protected abstract float _getScrollingSensitivity();

        protected abstract ViewportMaterialView _getVideoLayer();

        protected abstract ViewportMaterialView _getView();

        protected void _init(float width, float height, WebViewOptions options = new WebViewOptions(), IWebView initializedWebView = null) {

            _throwExceptionIfInitialized();
            // Remote debugging can only be enabled once, before any webviews are initialized.
            if (RemoteDebuggingEnabled && !_remoteDebuggingEnabled) {
                _remoteDebuggingEnabled = true;
                Web.EnableRemoteDebugging();
            }
            _options = options;

            // Only set _webView *after* the webview has been initialized to guarantee
            // that WebViewPrefab.WebView is ready to use as long as it's not null.
            var webView = initializedWebView == null ? Web.CreateWebView(_options.preferredPlugins) : initializedWebView;

            Web.CreateMaterial(viewMaterial => {
                _viewMaterial = viewMaterial;
                _view.Material = viewMaterial;
                _initWebViewIfReady(webView, width, height);
            });
            if (_options.disableVideo) {
                _videoLayerDisabled = true;
                if (_videoLayer != null) {
                    _videoLayer.Visible = false;
                }
                _initWebViewIfReady(webView, width, height);
            } else {
                Web.CreateVideoMaterial(videoMaterial => {
                    if (videoMaterial == null) {
                        _videoLayerDisabled = true;
                        if (_videoLayer != null) {
                            _videoLayer.Visible = false;
                        }
                    } else {
                        _videoMaterial = videoMaterial;
                        _videoLayer.Material = videoMaterial;
                        _setVideoRect(new Rect(0, 0, 0, 0));
                    }
                    _initWebViewIfReady(webView, width, height);
                });
            }
        }

        void _initPointerInputDetector(IWebView webView, IPointerInputDetector previousPointerInputDetector = null) {

            if (previousPointerInputDetector != null) {
                previousPointerInputDetector.BeganDrag -= InputDetector_BeganDrag;
                previousPointerInputDetector.Dragged -= InputDetector_Dragged;
                previousPointerInputDetector.PointerDown -= InputDetector_PointerDown;
                previousPointerInputDetector.PointerExited -= InputDetector_PointerExited;
                previousPointerInputDetector.PointerMoved -= InputDetector_PointerMoved;
                previousPointerInputDetector.PointerUp -= InputDetector_PointerUp;
                previousPointerInputDetector.Scrolled -= InputDetector_Scrolled;
            }

            if (_pointerInputDetector == null) {
                _pointerInputDetector = GetComponentInChildren<IPointerInputDetector>();
            }

            // Only enable the PointerMoved event if the webview implementation has MovePointer().
            _pointerInputDetector.PointerMovedEnabled = (webView as IWithMovablePointer) != null;
            _pointerInputDetector.BeganDrag += InputDetector_BeganDrag;
            _pointerInputDetector.Dragged += InputDetector_Dragged;
            _pointerInputDetector.PointerDown += InputDetector_PointerDown;
            _pointerInputDetector.PointerExited += InputDetector_PointerExited;
            _pointerInputDetector.PointerMoved += InputDetector_PointerMoved;
            _pointerInputDetector.PointerUp += InputDetector_PointerUp;
            _pointerInputDetector.Scrolled += InputDetector_Scrolled;
        }

        void _initWebViewIfReady(IWebView webView, float width, float height) {

            if (_view.Texture == null || (!_videoLayerDisabled && _videoLayer.Texture == null)) {
                // Wait until both views' textures are ready.
                return;
            }
            var initializedWebViewWasProvided = webView.IsInitialized;
            if (initializedWebViewWasProvided) {
                // An initialized webview was provided via WebViewPrefab.Init(IWebView),
                // so just hook up its existing textures.
                _view.Texture = webView.Texture;
                if (_videoLayer != null) {
                    _videoLayer.Texture = webView.VideoTexture;
                }
            } else {
                // Set the resolution prior to initializing the webview
                // so the initial size is correct.
                var initialResolution = _getInitialResolution();
                if (initialResolution <= 0) {
                    WebViewLogger.LogWarningFormat("Invalid value for InitialResolution ({0}) will be ignored.", initialResolution);
                } else {
                    webView.SetResolution(initialResolution);
                }
                var videoTexture = _videoLayer == null ? null : _videoLayer.Texture;
                webView.Init(_view.Texture, width, height, videoTexture);
            }

            _attachWebViewEventHandlers(webView);

            // Init the pointer input detector just before setting _webView so that
            // SetPointerInputDetector() will behave correctly if it's called before _webView is set.
            _initPointerInputDetector(webView);
            // _webView can be set now that the webview is initialized.
            _webView = webView;
            var handler = Initialized;
            if (handler != null) {
                handler(this, EventArgs.Empty);
            }
            if (!String.IsNullOrEmpty(InitialUrl)) {
                if (initializedWebViewWasProvided) {
                    WebViewLogger.LogWarning("Custom InitialUrl value will be ignored because an initialized webview was provided.");
                } else {
                    var url = InitialUrl.Trim();
                    if (!url.Contains(":")) {
                        url = "http://" + url;
                    }
                    webView.LoadUrl(url);
                }
            }
        }

        void InputDetector_BeganDrag(object sender, EventArgs<Vector2> eventArgs) {

            _previousDragPoint = _convertRatioPointToUnityUnits(_pointerDownRatioPoint);
        }

        void InputDetector_Dragged(object sender, EventArgs<Vector2> eventArgs) {

            if (DragMode == DragMode.Disabled || _webView == null) {
                return;
            }
            var point = eventArgs.Value;
            var previousDragPoint = _previousDragPoint;
            var newDragPoint = _convertRatioPointToUnityUnits(point);
            _previousDragPoint = newDragPoint;
            var totalDragDelta = _convertRatioPointToUnityUnits(_pointerDownRatioPoint) - newDragPoint;

            if (DragMode == DragMode.DragWithinPage) {
                var dragThresholdReached = totalDragDelta.magnitude * _webView.Resolution > DragThreshold;
                if (dragThresholdReached) {
                    _movePointerIfNeeded(point);
                }
                return;
            }

            // DragMode == DragMode.DragToScroll
            var dragDelta = previousDragPoint - newDragPoint;
            _scrollIfNeeded(dragDelta, _pointerDownRatioPoint);

            // Check whether to cancel a pending viewport click so that drag-to-scroll
            // doesn't unintentionally trigger a click.
            if (_clickIsPending) {
                var dragThresholdReached = totalDragDelta.magnitude * _webView.Resolution > DragThreshold;
                if (dragThresholdReached) {
                    _clickIsPending = false;
                }
            }
        }

        protected virtual void InputDetector_PointerDown(object sender, PointerEventArgs eventArgs) {

            _pointerIsDown = true;
            _pointerDownRatioPoint = eventArgs.Point;

            if (!ClickingEnabled || _webView == null) {
                return;
            }
            if (DragMode == DragMode.DragWithinPage) {
                var webViewWithPointerDown = _webView as IWithPointerDownAndUp;
                if (webViewWithPointerDown != null) {
                    webViewWithPointerDown.PointerDown(eventArgs.Point, eventArgs.ToPointerOptions());
                    return;
                } else if (!_loggedDragWarning) {
                    _loggedDragWarning = true;
                    WebViewLogger.LogWarningFormat("The WebViewPrefab's DragMode is set to DragWithinPage, but the webview implementation for this platform ({0}) doesn't support the PointerDown and PointerUp methods needed for dragging within a page. For more info, see <em>https://developer.vuplex.com/webview/IWithPointerDownAndUp</em>.", _webView.PluginType);
                    // Fallback to setting _clickIsPending so Click() can be called.
                }
            }
            // Defer calling PointerDown() for DragToScroll so that the click can
            // be cancelled if drag exceeds the threshold needed to become a scroll.
            _clickIsPending = true;
        }

        void InputDetector_PointerExited(object sender, EventArgs eventArgs) {

            if (HoveringEnabled) {
                // Remove the hover state when the pointer exits.
                _movePointerIfNeeded(Vector2.zero);
            }
        }

        void InputDetector_PointerMoved(object sender, EventArgs<Vector2> eventArgs) {

            // InputDetector_Dragged handles calling MovePointer while dragging.
            if (_pointerIsDown || !HoveringEnabled) {
                return;
            }
            _movePointerIfNeeded(eventArgs.Value);
        }

        protected virtual void InputDetector_PointerUp(object sender, PointerEventArgs eventArgs) {

            _pointerIsDown = false;
            if (!ClickingEnabled || _webView == null) {
                return;
            }
            var webViewWithPointerDownAndUp = _webView as IWithPointerDownAndUp;
            if (DragMode == DragMode.DragWithinPage && webViewWithPointerDownAndUp != null) {
                var totalDragDelta = _convertRatioPointToUnityUnits(_pointerDownRatioPoint) - _convertRatioPointToUnityUnits(eventArgs.Point);
                var dragThresholdReached = totalDragDelta.magnitude * _webView.Resolution > DragThreshold;
                var pointerUpPoint = dragThresholdReached ? eventArgs.Point : _pointerDownRatioPoint;
                webViewWithPointerDownAndUp.PointerUp(pointerUpPoint, eventArgs.ToPointerOptions());
            } else {
                if (!_clickIsPending) {
                    return;
                }
                _clickIsPending = false;
                // PointerDown() and PointerUp() don't support the preventStealingFocus parameter.
                if (webViewWithPointerDownAndUp == null || _options.clickWithoutStealingFocus) {
                    _webView.Click(eventArgs.Point, _options.clickWithoutStealingFocus);
                } else {
                    var pointerOptions = eventArgs.ToPointerOptions();
                    webViewWithPointerDownAndUp.PointerDown(eventArgs.Point, pointerOptions);
                    webViewWithPointerDownAndUp.PointerUp(eventArgs.Point, pointerOptions);
                }
            }

            var handler = Clicked;
            if (handler != null) {
                handler(this, new ClickedEventArgs(eventArgs.Point));
            }
        }

        void InputDetector_Scrolled(object sender, ScrolledEventArgs eventArgs) {

            var sensitivity = _getScrollingSensitivity();
            var scaledScrollDelta = new Vector2(
                eventArgs.ScrollDelta.x * sensitivity,
                eventArgs.ScrollDelta.y * sensitivity
            );
            _scrollIfNeeded(scaledScrollDelta, eventArgs.Point);
        }

        void _movePointerIfNeeded(Vector2 point) {

            var webViewWithMovablePointer = _webView as IWithMovablePointer;
            if (webViewWithMovablePointer == null) {
                return;
            }
            if (point != _previousMovePointerPoint) {
                _previousMovePointerPoint = point;
                webViewWithMovablePointer.MovePointer(point);
            }
        }

        void OnDestroy() {

            if (_webView != null && !_webView.IsDisposed) {
                _webView.Dispose();
            }
            Destroy();
            // Unity doesn't automatically destroy materials and textures
            // when the GameObject is destroyed.
            if (_viewMaterial != null) {
                Destroy(_viewMaterial.mainTexture);
                Destroy(_viewMaterial);
            }
            if (_videoMaterial != null) {
                Destroy(_videoMaterial.mainTexture);
                Destroy(_videoMaterial);
            }
        }

        void _scrollIfNeeded(Vector2 scrollDelta, Vector2 point) {

            // scrollDelta can be zero when the user drags the cursor off the screen.
            if (!ScrollingEnabled || _webView == null || scrollDelta == Vector2.zero) {
                return;
            }
            _webView.Scroll(scrollDelta, point);
            var handler = Scrolled;
            if (handler != null) {
                handler(this, new ScrolledEventArgs(scrollDelta, point));
            }
        }

        protected abstract void _setVideoLayerPosition(Rect videoRect);

        void _setVideoRect(Rect videoRect) {

            _view.SetCutoutRect(videoRect);
            _setVideoLayerPosition(videoRect);
            // This code applies a cropping rect to the video layer's shader based on what part of the video (if any)
            // falls outside of the viewport and therefore needs to be hidden. Note that the dimensions here are divided
            // by the videoRect's width or height, because in the videoLayer shader, the width of the videoRect is 1
            // and the height is 1 (i.e. the dimensions are normalized).
            float videoRectXMin = Math.Max(0, - 1 * videoRect.x / videoRect.width);
            float videoRectYMin = Math.Max(0, -1 * videoRect.y / videoRect.height);
            float videoRectXMax = Math.Min(1, (1 - videoRect.x) / videoRect.width);
            float videoRectYMax = Math.Min(1, (1 - videoRect.y) / videoRect.height);
            var videoCropRect = Rect.MinMaxRect(videoRectXMin, videoRectYMin, videoRectXMax, videoRectYMax);
            if (videoCropRect == new Rect(0, 0, 1, 1)) {
                // The entire video rect fits within the viewport, so set the cropt rect to zero to disable it.
                videoCropRect = new Rect(0, 0, 0, 0);
            }
            _videoLayer.SetCropRect(videoCropRect);
        }

        void _throwExceptionIfInitialized() {

            if (_webView != null) {
                throw new InvalidOperationException("Init() cannot be called on a WebViewPrefab that has already been initialized.");
            }
        }

        void Update() {

            // Check if LogConsoleMessages is changed from false to true at runtime.
            if (LogConsoleMessages && !_consoleMessageLoggedHandlerAttached && _webView != null) {
                _consoleMessageLoggedHandlerAttached = true;
                _webView.ConsoleMessageLogged += WebView_ConsoleMessageLogged;
            }
        }

        void WebView_ConsoleMessageLogged(object sender, ConsoleMessageEventArgs eventArgs) {

            if (!LogConsoleMessages) {
                return;
            }
            var message = "[Web Console] " + eventArgs.Message;
            if (eventArgs.Source != null) {
                message += String.Format(" ({0}:{1})", eventArgs.Source, eventArgs.Line);
            }
            switch (eventArgs.Level) {
                case ConsoleMessageLevel.Error:
                    WebViewLogger.LogError(message, false);
                    break;
                case ConsoleMessageLevel.Warning:
                    WebViewLogger.LogWarning(message, false);
                    break;
                default:
                    WebViewLogger.Log(message, false);
                    break;
            }
        }
    }
}
