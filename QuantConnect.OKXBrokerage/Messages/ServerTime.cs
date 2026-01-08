/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using Newtonsoft.Json;
using System.Collections.Generic;

namespace QuantConnect.Brokerages.OKX.Messages
{
    /// <summary>
    /// Represents OKX v5 API standard response wrapper
    /// All OKX v5 API endpoints return this format:
    /// {
    ///     "code": "0",
    ///     "msg": "",
    ///     "data": [...]
    /// }
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    public class OKXApiResponse<T>
    {
        /// <summary>
        /// Response code ("0" means success)
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// Error message (empty on success)
        /// </summary>
        [JsonProperty("msg")]
        public string Message { get; set; }

        /// <summary>
        /// Response data array
        /// </summary>
        [JsonProperty("data")]
        public List<T> Data { get; set; }

        /// <summary>
        /// Returns true if the API call was successful
        /// </summary>
        [JsonIgnore]
        public bool IsSuccess => Code == "0";
    }

    /// <summary>
    /// Represents OKX server time data object
    /// https://www.okx.com/docs-v5/en/#rest-api-public-data-get-system-time
    /// </summary>
    public class ServerTimeData
    {
        /// <summary>
        /// Server time in milliseconds (Unix timestamp)
        /// Example: "1597026383085"
        /// </summary>
        [JsonProperty("ts")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Gets timestamp as long
        /// </summary>
        [JsonIgnore]
        public long TimestampMs => long.TryParse(Timestamp, out var ts) ? ts : 0;
    }

    /// <summary>
    /// Represents OKX server time response (backward compatibility)
    /// </summary>
    public class ServerTime
    {
        /// <summary>
        /// Server time in milliseconds (Unix timestamp)
        /// Note: Property is named "ServerTimeSeconds" for historical reasons,
        /// but OKX API actually returns milliseconds
        /// </summary>
        [JsonProperty("ts")]
        public string TimestampString { get; set; }

        /// <summary>
        /// Gets timestamp as long
        /// </summary>
        [JsonIgnore]
        public long ServerTimeSeconds => long.TryParse(TimestampString, out var ts) ? ts : 0;
    }
}
