//-----------------------------------------------------------------------
// <copyright file="ClaimsResponse.cs" company="Andrew Arnott">
//     Copyright (c) Andrew Arnott. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DotNetOpenAuth.OpenId.Extensions.SimpleRegistration {
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.Net.Mail;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Xml.Serialization;
	using DotNetOpenAuth.Messaging;
	using DotNetOpenAuth.OpenId.Messages;

#pragma warning disable 0659, 0661
	/// <summary>
	/// A struct storing Simple Registration field values describing an
	/// authenticating user.
	/// </summary>
	public sealed class ClaimsResponse : ExtensionBase {
		/// <summary>
		/// The factory method that may be used in deserialization of this message.
		/// </summary>
		internal static readonly OpenIdExtensionFactory.CreateDelegate Factory = (typeUri, data, baseMessage) => {
			if (typeUri == Constants.sreg_ns && baseMessage is IndirectSignedResponse) {
				return new ClaimsResponse(typeUri);
			}

			return null;
		};

		/// <summary>
		/// The allowed format for birthdates.
		/// </summary>
		private static readonly Regex birthDateValidator = new Regex(@"^\d\d\d\d-\d\d-\d\d$");

		/// <summary>
		/// Storage for the raw string birthdate value.
		/// </summary>
		private string birthDateRaw;

		/// <summary>
		/// Backing field for the <see cref="BirthDate"/> property.
		/// </summary>
		private DateTime? birthDate;

		/// <summary>
		/// The TypeURI that must be used in the response, based on the one used in the request.
		/// </summary>
		private string typeUriToUse;

		/// <summary>
		/// Backing field for the <see cref="Culture"/> property.
		/// </summary>
		private CultureInfo culture;

		/// <summary>
		/// Initializes a new instance of the <see cref="ClaimsResponse"/> class.
		/// </summary>
		[Obsolete("Use ClaimsRequest.CreateResponse() instead.")]
		public ClaimsResponse()
			: this(Constants.sreg_ns) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ClaimsResponse"/> class.
		/// </summary>
		/// <param name="typeUriToUse">
		/// The type URI that must be used to identify this extension in the response message.
		/// This value should be the same one the relying party used to send the extension request.
		/// </param>
		internal ClaimsResponse(string typeUriToUse)
			: base(new Version(1, 0), typeUriToUse, EmptyList<string>.Instance) {
			ErrorUtilities.VerifyNonZeroLength(typeUriToUse, "typeUriToUse");
			this.typeUriToUse = typeUriToUse;
		}

		/// <summary>
		/// Gets or sets the nickname the user goes by.
		/// </summary>
		[MessagePart(Constants.nickname)]
		public string Nickname { get; set; }

		/// <summary>
		/// Gets or sets the user's email address.
		/// </summary>
		[MessagePart(Constants.email)]
		public string Email { get; set; }

		/// <summary>
		/// Gets or sets the full name of a user as a single string.
		/// </summary>
		[MessagePart(Constants.fullname)]
		public string FullName { get; set; }

		/// <summary>
		/// Gets or sets the user's birthdate.
		/// </summary>
		public DateTime? BirthDate {
			get {
				return this.birthDate;
			}

			set {
				this.birthDate = value;

				// Don't use property accessor for peer property to avoid infinite loop between the two proeprty accessors.
				if (value.HasValue) {
					this.birthDateRaw = value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
				} else {
					this.birthDateRaw = null;
				}
			}
		}

		/// <summary>
		/// Gets or sets the raw birth date string given by the extension.
		/// </summary>
		/// <value>A string in the format yyyy-MM-dd.</value>
		[MessagePart(Constants.dob)]
		public string BirthDateRaw {
			get {
				return this.birthDateRaw;
			}

			set {
				if (value != null) {
					if (!birthDateValidator.IsMatch(value)) {
						throw new ArgumentException(OpenIdStrings.SregInvalidBirthdate, "value");
					}

					// Update the BirthDate property, if possible. 
					// Don't use property accessor for peer property to avoid infinite loop between the two proeprty accessors.
					// Some valid sreg dob values like "2000-00-00" will not work as a DateTime struct, 
					// in which case we null it out, but don't show any error.
					DateTime newBirthDate;
					if (DateTime.TryParse(value, out newBirthDate)) {
						this.birthDate = newBirthDate;
					} else {
						Logger.WarnFormat("Simple Registration birthdate '{0}' could not be parsed into a DateTime and may not include month and/or day information.  Setting BirthDate property to null.", value);
						this.birthDate = null;
					}
				} else {
					this.birthDate = null;
				}

				this.birthDateRaw = value;
			}
		}

		/// <summary>
		/// Gets or sets the gender of the user.
		/// </summary>
		[MessagePart(Constants.gender, Encoder = typeof(GenderEncoder))]
		public Gender? Gender { get; set; }

		/// <summary>
		/// Gets or sets the zip code / postal code of the user.
		/// </summary>
		[MessagePart(Constants.postcode)]
		public string PostalCode { get; set; }

		/// <summary>
		/// Gets or sets the country of the user.
		/// </summary>
		[MessagePart(Constants.country)]
		public string Country { get; set; }

		/// <summary>
		/// Gets or sets the primary/preferred language of the user.
		/// </summary>
		[MessagePart(Constants.language)]
		public string Language { get; set; }

		/// <summary>
		/// Gets or sets the user's timezone.
		/// </summary>
		[MessagePart(Constants.timezone)]
		public string TimeZone { get; set; }

		/// <summary>
		/// Gets a combination of the user's full name and email address.
		/// </summary>
		public MailAddress MailAddress {
			get {
				if (string.IsNullOrEmpty(this.Email)) {
					return null;
				} else if (string.IsNullOrEmpty(this.FullName)) {
					return new MailAddress(this.Email);
				} else {
					return new MailAddress(this.Email, this.FullName);
				}
			}
		}

		/// <summary>
		/// Gets or sets a combination o the language and country of the user.
		/// </summary>
		[XmlIgnore]
		public CultureInfo Culture {
			get {
				if (this.culture == null && !string.IsNullOrEmpty(this.Language)) {
					string cultureString = string.Empty;
					cultureString = this.Language;
					if (!string.IsNullOrEmpty(this.Country)) {
						cultureString += "-" + this.Country;
					}
					this.culture = CultureInfo.GetCultureInfo(cultureString);
				}

				return this.culture;
			}

			set {
				this.culture = value;
				this.Language = (value != null) ? value.TwoLetterISOLanguageName : null;
				int indexOfHyphen = (value != null) ? value.Name.IndexOf('-') : -1;
				this.Country = indexOfHyphen > 0 ? value.Name.Substring(indexOfHyphen + 1) : null;
			}
		}

		#region IClientScriptExtension Members

		// TODO: re-enable this
		////string IClientScriptExtensionResponse.InitializeJavaScriptData(IDictionary<string, string> sreg, IAuthenticationResponse response, string typeUri) {
		////    StringBuilder builder = new StringBuilder();
		////    builder.Append("{ ");

		////    string nickname, email, fullName, dob, genderString, postalCode, country, language, timeZone;
		////    if (sreg.TryGetValue(Constants.nickname, out nickname)) {
		////        builder.Append(createAddFieldJS(Constants.nickname, nickname));
		////    }
		////    if (sreg.TryGetValue(Constants.email, out email)) {
		////        builder.Append(createAddFieldJS(Constants.email, email));
		////    }
		////    if (sreg.TryGetValue(Constants.fullname, out fullName)) {
		////        builder.Append(createAddFieldJS(Constants.fullname, fullName));
		////    }
		////    if (sreg.TryGetValue(Constants.dob, out dob)) {
		////        builder.Append(createAddFieldJS(Constants.dob, dob));
		////    }
		////    if (sreg.TryGetValue(Constants.gender, out genderString)) {
		////        builder.Append(createAddFieldJS(Constants.gender, genderString));
		////    }
		////    if (sreg.TryGetValue(Constants.postcode, out postalCode)) {
		////        builder.Append(createAddFieldJS(Constants.postcode, postalCode));
		////    }
		////    if (sreg.TryGetValue(Constants.country, out country)) {
		////        builder.Append(createAddFieldJS(Constants.country, country));
		////    }
		////    if (sreg.TryGetValue(Constants.language, out language)) {
		////        builder.Append(createAddFieldJS(Constants.language, language));
		////    }
		////    if (sreg.TryGetValue(Constants.timezone, out timeZone)) {
		////        builder.Append(createAddFieldJS(Constants.timezone, timeZone));
		////    }
		////    if (builder[builder.Length - 1] == ',') builder.Length -= 1;
		////    builder.Append("}");
		////    return builder.ToString();
		////}

		#endregion

		/// <summary>
		/// Tests equality of two <see cref="ClaimsResponse"/> objects.
		/// </summary>
		/// <param name="one">One instance to compare.</param>
		/// <param name="other">Another instance to compare.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator ==(ClaimsResponse one, ClaimsResponse other) {
			return one.EqualsNullSafe(other);
		}

		/// <summary>
		/// Tests inequality of two <see cref="ClaimsResponse"/> objects.
		/// </summary>
		/// <param name="one">One instance to compare.</param>
		/// <param name="other">Another instance to compare.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator !=(ClaimsResponse one, ClaimsResponse other) {
			return !(one == other);
		}

		/// <summary>
		/// Tests equality of two <see cref="ClaimsResponse"/> objects.
		/// </summary>
		/// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>.</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
		/// </returns>
		/// <exception cref="T:System.NullReferenceException">
		/// The <paramref name="obj"/> parameter is null.
		/// </exception>
		public override bool Equals(object obj) {
			ClaimsResponse other = obj as ClaimsResponse;
			if (other == null) {
				return false;
			}

			return
				this.BirthDateRaw.EqualsNullSafe(other.BirthDateRaw) &&
				this.Country.EqualsNullSafe(other.Country) &&
				this.Language.EqualsNullSafe(other.Language) &&
				this.Email.EqualsNullSafe(other.Email) &&
				this.FullName.EqualsNullSafe(other.FullName) &&
				this.Gender.Equals(other.Gender) &&
				this.Nickname.EqualsNullSafe(other.Nickname) &&
				this.PostalCode.EqualsNullSafe(other.PostalCode) &&
				this.TimeZone.EqualsNullSafe(other.TimeZone);
		}

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"/>.
		/// </returns>
		public override int GetHashCode() {
			return (this.Nickname != null) ? this.Nickname.GetHashCode() : base.GetHashCode();
		}
	}
}