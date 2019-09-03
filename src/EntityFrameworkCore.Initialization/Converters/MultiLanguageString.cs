using System.Collections.Generic;
using System.Globalization;

namespace EntityFrameworkCore.Initialization.Converters
{
    //https://github.com/aspnet/Extensions/blob/master/src/Localization/Abstractions/src/LocalizedString.cs
    public class MultiLanguageString : Dictionary<string, string>
    {
        private readonly string _defaultCulture;
        public MultiLanguageString(string defaultCulture = "")
        {
            _defaultCulture = defaultCulture;
            this.Add(_defaultCulture, null);
        }

        public string Value()
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            if (ContainsKey(culture))
            {
                return this[culture];
            }
            else if (ContainsKey(language))
            {
                return this[language];
            }
            else
            {
                return this[_defaultCulture];
            }
        }

        public void SetValue(string value)
        {
            var culture = CultureInfo.CurrentUICulture.Name;
            var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            if (ContainsKey(culture))
            {
                this[culture] = value;
            }
            else if (ContainsKey(language))
            {
               this[language] = value;
            }
            else
            {
                this[_defaultCulture] = value;
            }
        }

        public override string ToString() => Value();

        public static implicit operator string(MultiLanguageString multiLanguageString)
        {
            return multiLanguageString?.Value();
        }
    }
}
