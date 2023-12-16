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

namespace Vuplex.WebView {

    /// <summary>
    /// Event args for `DownloadProgressChanged`.
    /// </summary>
    public class DownloadChangedEventArgs : EventArgs {

        public DownloadChangedEventArgs(string contentType, string filePath, float progress, ProgressChangeType type, string url) {
            ContentType = contentType;
            FilePath = filePath;
            Progress = progress;
            Type = type;
            Url = url;
        }

        /// <summary>
        /// The mime type indicated by the `Content-Type` response header,
        /// or `null` if no content type was specified.
        /// </summary>
        readonly public string ContentType;

        /// <summary>
        /// The full file path of the downloaded file. Files are downloaded to
        /// Application.temporaryCachePath, but you can move them to a different
        /// location after they finish downloading.
        /// </summary>
        readonly public string FilePath;

        /// <summary>
        /// The estimated download progress, normalized to a float between 0 and 1.
        /// Note that not all platforms support intermediate progress updates.
        /// </summary>
        readonly public float Progress;

        /// <summary>
        /// The download progress event type. Note that not all platforms
        /// support the Updated event type.
        /// </summary>
        public readonly ProgressChangeType Type;

        /// <summary>
        /// The URL from which the file was downloaded.
        /// </summary>
        readonly public string Url;
    }
}

