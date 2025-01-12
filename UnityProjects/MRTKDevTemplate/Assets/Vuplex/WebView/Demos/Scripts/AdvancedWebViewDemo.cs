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
using System.Timers;
using UnityEngine;

namespace Vuplex.WebView.Demos {

    /// <summary>
    /// Sets up the AdvancedWebViewDemo scene, which displays web content in a main
    /// webview and then renders a UI in a second webview to display the current URL
    /// and provide back / forward navigation controls.
    ///
    /// The address bar currently only displays the current URL and is not an input.
    /// I plan to add a dedicated browser prefab in the future that will include
    /// a functional address bar. In the meantime, you can edit the CONTROLS_HTML field
    /// below to implement a URL input.
    /// </summary>
    /// <remarks>
    /// This scene demonstrates the following:
    /// - Programmatically instantiating `WebViewPrefab`s at runtime
    /// - Creating and hooking up an on-screen keyboard
    /// - Using `IWebView` methods like `LoadUrl`, `LoadHtml`, `GoBack`, and `GoForward`
    /// - Attaching handlers to the `IWebView.UrlChanged` and `MessageEmitted` events
    /// - Message passing from C#-to-JavaScript and vice versa
    /// - Creating a transparent webview using the transparent meta tag
    /// </remarks>
    class AdvancedWebViewDemo : MonoBehaviour {

        Timer _buttonRefreshTimer = new Timer();
        WebViewPrefab _controlsWebViewPrefab;
        HardwareKeyboardListener _hardwareKeyboardListener;
        WebViewPrefab _mainWebViewPrefab;

        void Start() {

            WebViewLogger.Log("Just a heads-up: AdvancedWebViewDemo's address bar currently only displays the current URL and is not an input. For more info, please see the comments in AdvancedWebViewDemo.cs.");

            // Create a 0.6 x 0.3 webview for the main web content.
            _mainWebViewPrefab = WebViewPrefab.Instantiate(0.6f, 0.3f);
            _mainWebViewPrefab.transform.parent = transform;
            _mainWebViewPrefab.transform.localPosition = new Vector3(0, -0.05f, 0.4f);
            _mainWebViewPrefab.transform.localEulerAngles = new Vector3(0, 180, 0);
            _mainWebViewPrefab.Initialized += (initializedSender, initializedEventArgs) => {
                _mainWebViewPrefab.WebView.UrlChanged += MainWebView_UrlChanged;
                _mainWebViewPrefab.WebView.LoadUrl("https://www.google.com");
            };

            _setUpKeyboards();

            // Create a second webview above the first to show a UI that
            // displays the current URL and provides back / forward navigation buttons.
            _controlsWebViewPrefab = WebViewPrefab.Instantiate(0.6f, 0.05f);
            _controlsWebViewPrefab.transform.parent = _mainWebViewPrefab.transform;
            _controlsWebViewPrefab.transform.localPosition = new Vector3(0, 0.06f, 0);
            _controlsWebViewPrefab.transform.localEulerAngles = Vector3.zero;
            _controlsWebViewPrefab.Initialized += (sender, e) => {
                // The controls UI won't be initialized in time to receive the first UrlChanged event,
                // so explicitly set the initial URL after the controls UI indicates it's ready.
                _controlsWebViewPrefab.WebView.MessageEmitted += (webView, eventArgs) => {
                    if (eventArgs.Value == "CONTROLS_INITIALIZED") {
                        _setDisplayedUrl(_mainWebViewPrefab.WebView.Url);
                    }
                };
                _controlsWebViewPrefab.WebView.MessageEmitted += Controls_MessageEmitted;
                _controlsWebViewPrefab.WebView.LoadHtml(CONTROLS_HTML);
                // Android Gecko and UWP w/ XR enabled don't support transparent webviews, so set the cutout
                // rect to the entire view so that the shader makes its black background pixels transparent.
                var pluginType = _controlsWebViewPrefab.WebView.PluginType;
                if (pluginType == WebPluginType.AndroidGecko || pluginType == WebPluginType.UniversalWindowsPlatform) {
                    _controlsWebViewPrefab.SetCutoutRect(new Rect(0, 0, 1, 1));
                }
            };

            // Set up a timer to allow the state of the back / forward buttons to be
            // refreshed one second after a URL change occurs.
            _buttonRefreshTimer.AutoReset = false;
            _buttonRefreshTimer.Interval = 1000;
            _buttonRefreshTimer.Elapsed += ButtonRefreshTimer_Elapsed;
        }

        void ButtonRefreshTimer_Elapsed(object sender, ElapsedEventArgs eventArgs) {

            // Get the main webview's back / forward state and then post a message
            // to the controls UI to update its buttons' state.
            Dispatcher.RunOnMainThread(() => {
                _mainWebViewPrefab.WebView.CanGoBack(canGoBack => {
                    _mainWebViewPrefab.WebView.CanGoForward(canGoForward => {
                        var serializedMessage = String.Format(
                            "{{ \"type\": \"SET_BUTTONS\", \"canGoBack\": {0}, \"canGoForward\": {1} }}",
                            canGoBack.ToString().ToLower(),
                            canGoForward.ToString().ToLower()
                        );
                        _controlsWebViewPrefab.WebView.PostMessage(serializedMessage);
                    });
                });
            });
        }

        void Controls_MessageEmitted(object sender, EventArgs<string> eventArgs) {

            var serializedMessage = eventArgs.Value;
            var type = BridgeMessage.ParseType(serializedMessage);
            if (type == "GO_BACK") {
                _mainWebViewPrefab.WebView.GoBack();
            } else if (type == "GO_FORWARD") {
                _mainWebViewPrefab.WebView.GoForward();
            }
        }

        void MainWebView_UrlChanged(object sender, UrlChangedEventArgs eventArgs) {

            _setDisplayedUrl(eventArgs.Url);
            _buttonRefreshTimer.Start();
        }

        void _setDisplayedUrl(string url) {

            if (_controlsWebViewPrefab.WebView != null) {
                var serializedMessage = String.Format("{{ \"type\": \"SET_URL\", \"url\": \"{0}\" }}", url);
                _controlsWebViewPrefab.WebView.PostMessage(serializedMessage);
            }
        }

        void _setUpKeyboards() {

            // Send keys from the hardware (USB or Bluetooth) keyboard to the webview.
            // Use separate `KeyDown()` and `KeyUp()` methods if the webview supports
            // it, otherwise just use `IWebView.HandleKeyboardInput()`.
            // https://developer.vuplex.com/webview/IWithKeyDownAndUp
            _hardwareKeyboardListener = HardwareKeyboardListener.Instantiate();
            _hardwareKeyboardListener.KeyDownReceived += (sender, eventArgs) => {
                var webViewWithKeyDown = _mainWebViewPrefab.WebView as IWithKeyDownAndUp;
                if (webViewWithKeyDown == null) {
                    _mainWebViewPrefab.WebView.HandleKeyboardInput(eventArgs.Value);
                } else {
                    webViewWithKeyDown.KeyDown(eventArgs.Value, eventArgs.Modifiers);
                }
            };
            _hardwareKeyboardListener.KeyUpReceived += (sender, eventArgs) => {
                var webViewWithKeyUp = _mainWebViewPrefab.WebView as IWithKeyDownAndUp;
                if (webViewWithKeyUp != null) {
                    webViewWithKeyUp.KeyUp(eventArgs.Value, eventArgs.Modifiers);
                }
            };

            // Also add an on-screen keyboard under the main webview.
            var keyboard = Keyboard.Instantiate();
            keyboard.transform.SetParent(_mainWebViewPrefab.transform, false);
            keyboard.transform.localPosition = new Vector3(0, -0.31f, 0);
            keyboard.transform.localEulerAngles = Vector3.zero;
            keyboard.InputReceived += (sender, eventArgs) => {
                _mainWebViewPrefab.WebView.HandleKeyboardInput(eventArgs.Value);
            };

        }

        const string CONTROLS_HTML = @"
            <!DOCTYPE html>
            <html>
                <head>
                    <!-- This transparent meta tag instructs 3D WebView to allow the page to be transparent. -->
                    <meta name='transparent' content='true'>
                    <meta charset='UTF-8'>
                    <style>
                        body {
                            font-family: Helvetica, Arial, Sans-Serif;
                            margin: 0;
                            height: 100vh;
                            color: white;
                        }
                        .controls {
                            display: flex;
                            justify-content: space-between;
                            align-items: center;
                            height: 100%;
                        }
                        .controls > div {
                            background-color: #283237;
                            border-radius: 8px;
                            height: 100%;
                        }
                        .url-display {
                            flex: 0 0 75%;
                            width: 75%;
                            display: flex;
                            align-items: center;
                            overflow: hidden;
                        }
                        #url {
                            width: 100%;
                            white-space: nowrap;
                            overflow: hidden;
                            text-overflow: ellipsis;
                            padding: 0 15px;
                            font-size: 18px;
                        }
                        .buttons {
                            flex: 0 0 20%;
                            width: 20%;
                            display: flex;
                            justify-content: space-around;
                            align-items: center;
                        }
                        .buttons > button {
                            font-size: 40px;
                            background: none;
                            border: none;
                            outline: none;
                            color: white;
                            margin: 0;
                            padding: 0;
                        }
                        .buttons > button:disabled {
                            color: rgba(255, 255, 255, 0.3);
                        }
                        .buttons > button:last-child {
                            transform: scaleX(-1);
                        }
                        /* For Gecko only, set the background color
                        to black so that the shader's cutout rect
                        can translate the black pixels to transparent.*/
                        @supports (-moz-appearance:none) {
                            body {
                                background-color: black;
                            }
                        }
                    </style>
                </head>
                <body>
                    <div class='controls'>
                        <div class='url-display'>
                            <div id='url'></div>
                        </div>
                        <div class='buttons'>
                            <button id='back-button' disabled='true' onclick='vuplex.postMessage({ type: ""GO_BACK"" })'>←</button>
                            <button id='forward-button' disabled='true' onclick='vuplex.postMessage({ type: ""GO_FORWARD"" })'>←</button>
                        </div>
                    </div>
                    <script>
                        // Handle messages sent from C#
                        function handleMessage(message) {
                            var data = JSON.parse(message.data);
                            if (data.type === 'SET_URL') {
                                document.getElementById('url').innerText = data.url;
                            } else if (data.type === 'SET_BUTTONS') {
                                document.getElementById('back-button').disabled = !data.canGoBack;
                                document.getElementById('forward-button').disabled = !data.canGoForward;
                            }
                        }

                        function attachMessageListener() {
                            window.vuplex.addEventListener('message', handleMessage);
                            window.vuplex.postMessage('CONTROLS_INITIALIZED');
                        }

                        if (window.vuplex) {
                            attachMessageListener();
                        } else {
                            window.addEventListener('vuplexready', attachMessageListener);
                        }
                    </script>
                </body>
            </html>
        ";
    }
}
