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

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace QuantConnect.Brokerages.OKX
{
    /// <summary>
    /// OKX Unified Account Mode
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OKXUnifiedAccountMode
    {
        /// <summary>
        /// Classic account mode (default, no unified account features)
        /// </summary>
        [EnumMember(Value = "classic")]
        Classic,

        /// <summary>
        /// Single-currency margin mode (only USDT as margin)
        /// </summary>
        [EnumMember(Value = "single_currency")]
        SingleCurrency,

        /// <summary>
        /// Multi-currency margin mode (160+ currencies as margin)
        /// </summary>
        [EnumMember(Value = "multi_currency")]
        MultiCurrency,

        /// <summary>
        /// Portfolio margin mode (VaR-based risk assessment)
        /// </summary>
        [EnumMember(Value = "portfolio")]
        Portfolio
    }

    /// <summary>
    /// Settings for unified account mode
    /// </summary>
    public class OKXUnifiedAccountSettings
    {
        /// <summary>
        /// USDT futures switch. In multi-currency margin mode, can only be turned on, not off.
        /// </summary>
        [JsonProperty("usdt_futures")]
        public bool? UsdtFutures { get; set; }

        /// <summary>
        /// Spot hedge switch
        /// </summary>
        [JsonProperty("spot_hedge")]
        public bool? SpotHedge { get; set; }

        /// <summary>
        ///余币宝 (funding) switch. When mode is multi-currency margin, whether to use funding funds as margin.
        /// </summary>
        [JsonProperty("use_funding")]
        public bool? UseFunding { get; set; }

        /// <summary>
        /// Options switch. In multi-currency margin mode, can only be turned on, not off.
        /// </summary>
        [JsonProperty("options")]
        public bool? Options { get; set; }
    }

    /// <summary>
    /// Response from GET /unified/unified_mode
    /// </summary>
    public class OKXUnifiedAccountModeResponse
    {
        /// <summary>
        /// Unified account mode
        /// </summary>
        [JsonProperty("mode")]
        public OKXUnifiedAccountMode Mode { get; set; }

        /// <summary>
        /// Account settings
        /// </summary>
        [JsonProperty("settings")]
        public OKXUnifiedAccountSettings Settings { get; set; }
    }

    /// <summary>
    /// Request for PUT /unified/unified_mode
    /// </summary>
    public class OKXUnifiedAccountModeRequest
    {
        /// <summary>
        /// Unified account mode to set
        /// </summary>
        [JsonProperty("mode")]
        public OKXUnifiedAccountMode Mode { get; set; }

        /// <summary>
        /// Optional settings for the account mode
        /// </summary>
        [JsonProperty("settings", NullValueHandling = NullValueHandling.Ignore)]
        public OKXUnifiedAccountSettings Settings { get; set; }
    }
}
