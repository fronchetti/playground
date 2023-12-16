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
using UnityEditor;

namespace Vuplex.WebView {

    public abstract class BaseWebViewPrefabInspector : Editor {

        public override void OnInspectorGUI() {

            DocumentationLinkDrawer.DrawDocumentationLink(_getDocumentationLink());
            serializedObject.Update();
            EditorGUILayout.PropertyField(_initialUrl);
            EditorGUILayout.PropertyField(_initialResolution);
            EditorGUILayout.PropertyField(_scrollingSensitivity);
            serializedObject.ApplyModifiedProperties();
            DrawDefaultInspector();
            TroubleshootingLinkDrawer.DrawTroubleshootingLink();
        }

        protected abstract string _getDocumentationLink();
        SerializedProperty _initialResolution;
        SerializedProperty _initialUrl;
        SerializedProperty _scrollingSensitivity;

        void OnEnable() {

            _initialResolution = serializedObject.FindProperty("InitialResolution");
            _initialUrl = serializedObject.FindProperty("InitialUrl");
            _scrollingSensitivity = serializedObject.FindProperty("ScrollingSensitivity");
        }
    }
}
