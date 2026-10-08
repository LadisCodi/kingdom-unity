using System.Collections.Generic;
using Kingdom.Game.I18n;
using NUnit.Framework;

namespace Kingdom.Tests.Game.I18n
{
    public class LocalizationTests
    {
        private Localization _localization;

        [SetUp]
        public void SetUp()
        {
            _localization = new Localization();
            _localization.AddSpanish(new Dictionary<string, string>
            {
                ["Build"] = "Construir",
                ["noun::Build"] = "Construcción",
                ["A tap takes +{n}%"] = "Un toque recoge +{n}%",
                ["{n} coin|{n} coins"] = "{n} moneda|{n} monedas",
            });
        }

        [Test]
        public void Tr_ShouldReturnTheEnglishInEnglish()
        {
            Assert.That(_localization.Tr("Build"), Is.EqualTo("Build"));
            Assert.That(_localization.Tr("noun::Build"), Is.EqualTo("Build"));
        }

        [Test]
        public void Tr_ShouldTranslateByContextInSpanish()
        {
            _localization.SetLanguage(Language.Spanish);

            Assert.That(_localization.Tr("Build"), Is.EqualTo("Construir"));
            Assert.That(_localization.Tr("noun::Build"), Is.EqualTo("Construcción"));
        }

        [Test]
        public void Tr_ShouldFallBackToEnglishForAnUntranslatedLine()
        {
            _localization.SetLanguage(Language.Spanish);

            Assert.That(_localization.Tr("Collect"), Is.EqualTo("Collect"));
        }

        [Test]
        public void Tr_ShouldFillPlaceholders()
        {
            _localization.SetLanguage(Language.Spanish);

            Assert.That(_localization.Tr("A tap takes +{n}%", ("n", 15)), Is.EqualTo("Un toque recoge +15%"));
        }

        [Test]
        public void Trn_ShouldPickTheFormByCount()
        {
            Assert.That(_localization.Trn(1, "{n} coin", "{n} coins", ("n", 1)), Is.EqualTo("1 coin"));

            _localization.SetLanguage(Language.Spanish);

            Assert.That(_localization.Trn(3, "{n} coin", "{n} coins", ("n", 3)), Is.EqualTo("3 monedas"));
        }
    }
}
