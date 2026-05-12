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

namespace QuantConnect.DataSource.QuiverQuant
{
    /// <summary>
    /// SEC Form 4 transaction codes (see https://www.sec.gov/files/forms-3-4-5.pdf)
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TransactionCode
    {
        /// <summary>
        /// S - Open market or private sale of non-derivative or derivative security
        /// </summary>
        [EnumMember(Value = "S")]
        Sale = -1,

        /// <summary>
        /// J - Other acquisition or disposition (describe transaction). Also used as the
        /// default value when no transaction code is provided.
        /// </summary>
        [EnumMember(Value = "J")]
        Other,

        /// <summary>
        /// P - Open market or private purchase of non-derivative or derivative security
        /// </summary>
        [EnumMember(Value = "P")]
        Purchase,

        /// <summary>
        /// V - Transaction voluntarily reported earlier than required
        /// </summary>
        [EnumMember(Value = "V")]
        VoluntaryReport,

        /// <summary>
        /// A - Grant, award, or other acquisition pursuant to Rule 16b-3(d)
        /// </summary>
        [EnumMember(Value = "A")]
        GrantOrAward,

        /// <summary>
        /// D - Disposition to the issuer of issuer equity securities pursuant to Rule 16b-3(e)
        /// </summary>
        [EnumMember(Value = "D")]
        DispositionToIssuer,

        /// <summary>
        /// F - Payment of exercise price or tax liability by delivering or withholding securities
        /// incident to the receipt, exercise, or vesting of a security issued in accordance with Rule 16b-3
        /// </summary>
        [EnumMember(Value = "F")]
        ExercisePaymentWithSecurities,

        /// <summary>
        /// I - Discretionary transaction in accordance with Rule 16b-3(f)
        /// </summary>
        [EnumMember(Value = "I")]
        DiscretionaryTransaction,

        /// <summary>
        /// M - Exercise or conversion of derivative security exempted pursuant to Rule 16b-3
        /// </summary>
        [EnumMember(Value = "M")]
        ExerciseOrConversionExempt,

        /// <summary>
        /// C - Conversion of derivative security
        /// </summary>
        [EnumMember(Value = "C")]
        ConversionOfDerivative,

        /// <summary>
        /// E - Expiration of short derivative position
        /// </summary>
        [EnumMember(Value = "E")]
        ShortDerivativeExpiration,

        /// <summary>
        /// H - Expiration (or cancellation) of long derivative position with value received
        /// </summary>
        [EnumMember(Value = "H")]
        LongDerivativeExpirationWithValue,

        /// <summary>
        /// O - Exercise of out-of-the-money derivative security
        /// </summary>
        [EnumMember(Value = "O")]
        OutOfMoneyExercise,

        /// <summary>
        /// X - Exercise of in-the-money or at-the-money derivative security
        /// </summary>
        [EnumMember(Value = "X")]
        InMoneyExercise,

        /// <summary>
        /// G - Bona fide gift
        /// </summary>
        [EnumMember(Value = "G")]
        Gift,

        /// <summary>
        /// L - Small acquisition under Rule 16a-6
        /// </summary>
        [EnumMember(Value = "L")]
        SmallAcquisition,

        /// <summary>
        /// W - Acquisition or disposition by will or the laws of descent and distribution
        /// </summary>
        [EnumMember(Value = "W")]
        AcquisitionByWill,

        /// <summary>
        /// Z - Deposit into or withdrawal from voting trust
        /// </summary>
        [EnumMember(Value = "Z")]
        VotingTrustDeposit,

        /// <summary>
        /// K - Transaction in equity swap or instrument with similar characteristics
        /// </summary>
        [EnumMember(Value = "K")]
        EquitySwap,

        /// <summary>
        /// U - Disposition pursuant to a tender of shares in a change of control transaction
        /// </summary>
        [EnumMember(Value = "U")]
        TenderDisposition,
    }
}
