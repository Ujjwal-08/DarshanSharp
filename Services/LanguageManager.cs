using System;
using System.Collections.Generic;
using System.Windows;

namespace DarshanPlayer.Services
{
    public class LanguageManager
    {
        public static readonly IReadOnlyList<LanguageOption> SupportedLanguages = new List<LanguageOption>
        {
            new("en", "English", "🇬🇧"),
            new("hi", "हिन्दी", "🇮🇳"),
            new("ta", "தமிழ்", "🇮🇳"),
            new("te", "తెలుగు", "🇮🇳"),
            new("kn", "ಕನ್ನಡ", "🇮🇳"),
            new("mr", "मराठी", "🇮🇳"),
            new("bn", "বাংলা", "🇮🇳"),
            new("gu", "ગુજરાતી", "🇮🇳"),
            new("pa", "ਪੰਜਾਬੀ", "🇮🇳"),
            new("ml", "മലയാളം", "🇮🇳"),
        };

        private string _currentCode = "en";
        public string CurrentCode => _currentCode;

        public event EventHandler<string>? LanguageChanged;

        public void Apply(string code)
        {
            _currentCode = code;
            var dictUri = new Uri($"pack://application:,,,/Localization/Lang.{code}.xaml");
            var newDict = new ResourceDictionary { Source = dictUri };

            // Replace existing language dictionary
            var merged = Application.Current.Resources.MergedDictionaries;
            for (int i = merged.Count - 1; i >= 0; i--)
            {
                var src = merged[i].Source?.OriginalString ?? "";
                if (src.Contains("/Localization/Lang."))
                {
                    merged.RemoveAt(i);
                    break;
                }
            }
            merged.Add(newDict);
            LanguageChanged?.Invoke(this, code);
        }
    }

    public record LanguageOption(string Code, string DisplayName, string Flag)
    {
        public override string ToString() => $"{Flag} {DisplayName}";
    }
}
